using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ItemSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public enum OwnerType { Inventory, Crafting }

    [Header("Slot-Zuordnung")]
    public OwnerType owner;
    public int index;
    public Inventory inventory;
    public CraftingGrid craftingGrid;

    [Header("UI")]
    public Image iconImage;
    // Unterstütze sowohl Legacy-Text als auch TMP:
    public Text countText;                       // optional (Legacy)
    public TextMeshProUGUI countTMP;             // optional (TMP)

    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform dragIcon;
    private Image dragIconImage;
    private Text dragCountTextLegacy;            // zur Not Legacy
    private TextMeshProUGUI dragCountTMP;        // wenn TMP verfügbar

    private bool isInitialized;

    private ItemStack Stack
    {
        get
        {
            if (!isInitialized) return new ItemStack();
            return owner == OwnerType.Inventory
                ? inventory.GetSlot(index)
                : craftingGrid.GetCell(index);
        }
    }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();
        // kein Refresh hier
    }

public void Init()
{
    if (!iconImage) iconImage = transform.Find("Icon")?.GetComponent<Image>();

    // TMP bevorzugen
    if (!countTMP) countTMP = transform.Find("Count")?.GetComponent<TextMeshProUGUI>();
    if (!countTMP) countTMP = GetComponentInChildren<TextMeshProUGUI>(true);

    // Legacy nur als Fallback, sonst deaktivieren
    if (!countTMP)
    {
        if (!countText) countText = transform.Find("Count")?.GetComponent<Text>();
        if (!countText) countText = GetComponentInChildren<Text>(true);
    }
    else
    {
        var legacy = GetComponentInChildren<Text>(true);
        if (legacy) legacy.enabled = false; // verhindert „Geisterzahlen“
    }

    isInitialized = (owner == OwnerType.Inventory && inventory != null)
                 || (owner == OwnerType.Crafting && craftingGrid != null);

    // beide Gegenanzeigen initial leeren
    if (countTMP) countTMP.text = "";
    if (countText) countText.text = "";

    Refresh();
}

    public void Refresh()
    {
        if (!isInitialized || iconImage == null) return;

        var st = Stack;
        bool has = !st.IsEmpty;

        iconImage.sprite  = has ? st.Item.Icon : null;
        iconImage.enabled = has && iconImage.sprite != null;

        string countStr = (has && st.Amount > 1) ? st.Amount.ToString() : "";

        if (countTMP)  countTMP.text  = countStr;
        if (countText) countText.text = countStr;

        // after setting text: trigger count pop animation
        var pop = transform.Find("Count")?.GetComponent<CountPopAnimator>();
        if (pop)
        {
            int amt = st.IsEmpty ? 0 : st.Amount;
            pop.SetCount(amt, showWhenOne: false);
        }
    }


    // --- Drag & Drop ---
public void OnBeginDrag(PointerEventData eventData)
{
    if (!isInitialized || Stack.IsEmpty) return;

    // Drag-Ghost root
    dragIcon = new GameObject("DragIcon", typeof(RectTransform)).GetComponent<RectTransform>();
    dragIcon.SetParent(rootCanvas.transform, false);
    dragIcon.SetAsLastSibling();

    // Bild/Icon
    dragIconImage = dragIcon.gameObject.AddComponent<Image>();
    dragIconImage.raycastTarget = false;
    dragIconImage.sprite = Stack.Item.Icon;
    dragIconImage.preserveAspect = true;

    // feste, handliche Größe (optional: SetNativeSize())
    dragIcon.sizeDelta = new Vector2(64f, 64f);

    // Ghost soll nicht blocken
    var cg = dragIcon.gameObject.AddComponent<CanvasGroup>();
    cg.blocksRaycasts = false;

    // Vorschau-Menge (z.B. aus Inventar default 1; Shift=voll, Ctrl=halb)
    int previewQty = ComputeDragQuantityPreview(Stack.Amount);

    // Count-Overlay nur wenn > 1
    if (previewQty > 1)
    {
        var countGO = new GameObject("Count", typeof(RectTransform));
        countGO.transform.SetParent(dragIcon, false);

        var rt = countGO.GetComponent<RectTransform>();
        // unten rechts andocken mit kleinem Padding
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-4f, 4f);
        rt.sizeDelta = Vector2.zero;

        // TMP bevorzugen, Legacy als Fallback
        dragCountTMP = countGO.AddComponent<TextMeshProUGUI>();
        if (dragCountTMP != null)
        {
            dragCountTMP.text = previewQty.ToString();
            dragCountTMP.alignment = TextAlignmentOptions.BottomRight;
            dragCountTMP.fontSize = 20f;
            dragCountTMP.raycastTarget = false;

            // leichte Outline für Lesbarkeit (optional)
            var outline = countGO.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, -1f);
        }
        else
        {
            dragCountTextLegacy = countGO.AddComponent<Text>();
            dragCountTextLegacy.text = previewQty.ToString();
            dragCountTextLegacy.alignment = TextAnchor.LowerRight;
            dragCountTextLegacy.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            dragCountTextLegacy.raycastTarget = false;
        }
    }

    // Drops zulassen
    if (canvasGroup) canvasGroup.blocksRaycasts = false;

    // sofort an Maus positionieren
    MoveDragIcon(eventData);
}


    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null) MoveDragIcon(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup) canvasGroup.blocksRaycasts = true;
        if (dragIcon != null) Destroy(dragIcon.gameObject);
        dragIcon = null;
        dragCountTMP = null;
        dragCountTextLegacy = null;
    }

public void OnDrop(PointerEventData eventData)
    {
        if (!isInitialized) return;

        var source = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<ItemSlotUI>() : null;
        if (source == null || source == this) return;

        var from = source.Stack;
        var to = this.Stack;
        if (from.IsEmpty) return;

        // Menge nach Modifikatoren bestimmen
        int qty = ComputeDropQuantity(from.Amount, source.owner, this.owner);

        // Wenn Ziel leer oder gleicher Typ -> Teilmenge bewegen
        if (to.IsEmpty || to.Item == from.Item)
        {
            Inventory.MoveQuantity(from, to, qty);
            source.Refresh();
            Refresh();
            return;
        }

        // Unterschiedlicher Typ und Ziel belegt:
        // Wenn ganze Menge bewegt werden soll und beide Seiten gleiche "Art" (z. B. Inv↔Inv),
        // kannst du Swap erlauben. Für Crafting-Slots ist Swap meist unerwünscht.
        bool movingFullStack = qty >= from.Amount;
        bool bothInventory = source.owner == OwnerType.Inventory && this.owner == OwnerType.Inventory;

        if (movingFullStack && bothInventory)
        {
            Inventory.Swap(to, from);
            source.Refresh();
            Refresh();
            return;
        }

        // Sonst: kein Move möglich (belegt & anderer Typ)
        // (Optional: Feedback/Shake/Sound)
    }
    private int ComputeDropQuantity(int available, OwnerType fromOwner, OwnerType toOwner)
        {
            // Modifiers
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

            if (shift) return available;                  // voller Stack
            if (ctrl)  return Mathf.Max(1, Mathf.CeilToInt(available / 2f)); // halber Stack
            if (alt)   return 1;                          // exakt 1

            // Default-Regel:
            // - Inventar -> Crafting: 1 Stück
            // - sonst: ganze Menge
            if (fromOwner == OwnerType.Inventory && toOwner == OwnerType.Crafting)
                return 1;

            return available;
        }

        private int ComputeDragQuantityPreview(int available)
{
    bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    bool ctrl  = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
              || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
    bool alt   = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

    if (shift) return available;                                   // voller Stack
    if (ctrl)  return Mathf.Max(1, Mathf.CeilToInt(available/2f)); // halber Stack
    if (alt)   return 1;                                           // exakt 1

    // Standard: aus Inventar = 1, aus Crafting = voller Stack
    return owner == OwnerType.Inventory ? 1 : available;
}



    private void MoveDragIcon(PointerEventData eventData)
    {
        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            dragIcon.position = eventData.position;
        else if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                     (RectTransform)rootCanvas.transform, eventData.position,
                     eventData.pressEventCamera, out var local))
            dragIcon.localPosition = local;
    }

}
