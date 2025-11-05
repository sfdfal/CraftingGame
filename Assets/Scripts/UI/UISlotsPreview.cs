using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class UISlotsPreview : MonoBehaviour
{
    public enum CountMode { Fixed, FromInventoryCapacity }

    [Header("Setup")]
    public Transform slotParent;
    public GameObject slotPrefab;
    public CountMode countMode = CountMode.Fixed;
    public int fixedCount = 16;
    public Inventory inventorySource;

    [Header("Preview")]
    public bool showPreviewInEditMode = true;
    public Sprite placeholderIcon;
    public bool disableInteractiveComponents = true;

    bool wasPlaying;

    void Reset() { slotParent = transform; }

    void OnEnable()  { UpdatePreview(); }
    void OnDisable() { ClearPreview(); }

    void Update()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            if (!wasPlaying) { wasPlaying = true; ClearPreview(); }
            return;
        }
        wasPlaying = false;

        if (showPreviewInEditMode) EnsurePreviewCount();
#endif
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && isActiveAndEnabled) UpdatePreview();
#endif
    }

    public void UpdatePreview()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) { ClearPreview(); return; }
        if (!showPreviewInEditMode || !slotParent || !slotPrefab) { ClearPreview(); return; }
        EnsurePreviewCount();
#endif
    }

#if UNITY_EDITOR
    void EnsurePreviewCount()
    {
        int want = (countMode == CountMode.FromInventoryCapacity && inventorySource)
            ? Mathf.Max(0, inventorySource.capacity)
            : Mathf.Max(0, fixedCount);

        int have = CountPreviewChildren();
        if (have > want) RemovePreviewChildren(have - want);
        else if (have < want) AddPreviewChildren(want - have);
    }

    int CountPreviewChildren()
    {
        int c = 0;
        foreach (Transform t in slotParent)
            if (t.GetComponent<PreviewMarker>()) c++;
        return c;
    }

    void RemovePreviewChildren(int count)
    {
        for (int i = slotParent.childCount - 1; i >= 0 && count > 0; i--)
        {
            var child = slotParent.GetChild(i);
            if (!child.GetComponent<PreviewMarker>()) continue;
            Undo.DestroyObjectImmediate(child.gameObject);
            count--;
        }
    }

    void AddPreviewChildren(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, slotParent);
            if (!go) go = (GameObject)Object.Instantiate(slotPrefab, slotParent);

            go.name = "Slot (Preview)";
            if (!go.GetComponent<PreviewMarker>()) go.AddComponent<PreviewMarker>();

            if (disableInteractiveComponents)
            {
                var slotUI = go.GetComponent<ItemSlotUI>();
                if (slotUI) slotUI.enabled = false;
                foreach (var sel in go.GetComponentsInChildren<UnityEngine.UI.Selectable>(true))
                    sel.enabled = false;
            }

            if (placeholderIcon)
            {
                var icon = go.transform.Find("Icon")?.GetComponent<UnityEngine.UI.Image>();
                if (icon) { icon.sprite = placeholderIcon; icon.enabled = true; }
                var countObj = go.transform.Find("Count");
                if (countObj) countObj.gameObject.SetActive(false);
            }

            go.hideFlags |= HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            Undo.RegisterCreatedObjectUndo(go, "Add Slot Preview");
        }
    }

    [ContextMenu("Preview ▶ Rebuild Now")]
    void CM_Rebuild() { ClearPreview(); UpdatePreview(); }

    [ContextMenu("Preview ✖ Clear")]
    void CM_Clear() { ClearPreview(); }
#endif

    public void ClearPreview()
    {
#if UNITY_EDITOR
        if (!slotParent) return;
        for (int i = slotParent.childCount - 1; i >= 0; i--)
        {
            var child = slotParent.GetChild(i);
            if (child.GetComponent<PreviewMarker>())
                Undo.DestroyObjectImmediate(child.gameObject);
        }
#endif
    }
}
