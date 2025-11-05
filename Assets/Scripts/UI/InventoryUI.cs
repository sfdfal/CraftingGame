using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public Inventory inventory;     // <- dein Daten-Inventory (separates GameObject mit Inventory.cs)
    public GameObject slotPrefab;   // <- Prefabs/UI/Slot (mit ItemSlotUI)
    public Transform slotParent;    // <- meist das gleiche Objekt, auf dem dieses Script hängt

    private ItemSlotUI[] slots;
    public ItemSlotUI[] Slots => slots;

    private void Start()
    {
        Build();

        if (inventory != null)
            inventory.OnChanged += RefreshAll;
        else
            Debug.LogError("[InventoryUI] 'inventory' ist nicht zugewiesen.", this);
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnChanged -= RefreshAll;
    }

    public void Build()
    {
        if (inventory == null)   { Debug.LogError("[InventoryUI] 'inventory' ist nicht zugewiesen.", this); return; }
        if (slotPrefab == null)  { Debug.LogError("[InventoryUI] 'slotPrefab' ist nicht zugewiesen.", this); return; }
        if (slotParent == null)  { Debug.LogError("[InventoryUI] 'slotParent' ist nicht zugewiesen.", this); return; }

        // alte Kinder entfernen
        for (int i = slotParent.childCount - 1; i >= 0; i--)
            Destroy(slotParent.GetChild(i).gameObject);

        int count = inventory.Slots.Count;
        slots = new ItemSlotUI[count];

        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(slotPrefab, slotParent);
            var ui = go.GetComponent<ItemSlotUI>();
            if (ui == null)
            {
                Debug.LogError("[InventoryUI] slotPrefab hat kein ItemSlotUI-Component!", slotPrefab);
                Destroy(go);
                continue;
            }

            ui.owner = ItemSlotUI.OwnerType.Inventory;
            ui.index = i;
            ui.inventory = inventory;

            // falls Prefab-Referenzen fehlen, versucht Init() sie zu finden
            ui.Init();

            slots[i] = ui;
        }
    }

    public void RefreshAll()
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] != null) slots[i].Refresh();
    }
}
