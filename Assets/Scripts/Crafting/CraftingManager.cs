using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private CraftingGrid grid;
    [SerializeField] private List<ShapedRecipe> recipes = new();
    
    // Optional: Direkte UI-Referenzen, um bei jeder Änderung hart zu refreshen
    // (zusätzlich zu den Events der Datenobjekte)
    [Header("UI (optional)")]
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private CraftingGridUI craftingGridUI;

    private void OnEnable()
    {
        if (inventory != null) inventory.OnChanged += ForceUIRefresh;
        if (grid != null) grid.OnChanged += ForceUIRefresh;
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.OnChanged -= ForceUIRefresh;
        if (grid != null) grid.OnChanged -= ForceUIRefresh;
    }

    private void ForceUIRefresh()
    {
        // Falls UI-Referenzen gesetzt sind, erzwinge ein komplettes Refresh
        if (inventoryUI != null) inventoryUI.RefreshAll();
        if (craftingGridUI != null) craftingGridUI.RefreshAll();
    }

    // Button-Handler (OnClick -> CraftingManager.Craft)
    public void Craft()
    {
        if (!TryCraftOnce())
        {
            Debug.Log("[CraftingManager] Kein passendes Rezept / nicht genug Ressourcen / kein Platz.");
        }
        else
        {
            Debug.Log("Crafting erfolgreich!");
            // pulse button
            var btn = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            if (btn)
            {
                var juice = btn.GetComponent<ButtonJuice>();
                if (juice) juice.PulseSuccess();
            }
        }

        // Sicherheitshalber UI immer refreshen (auch wenn TryCraftOnce intern Events raised)
        ForceUIRefresh();
    }

    private bool TryCraftOnce()
    {
        if (!inventory || !grid || recipes == null || recipes.Count == 0) return false;

        foreach (var r in recipes)
        {
            if (r == null || r.pattern == null || r.pattern.Length != 16) continue;

            // 1) Form + Bedarf ermitteln (shaped; Typen müssen passen, Menge egal; Bedarf je Item summieren)
            if (!ComputeNeedsIfShapeMatches(r, out var needsPerItem)) continue;

            // 2) SIMULATION auf Kopien von Inventar + Grid
            // --- Inventar kopieren
            var invSlots = inventory.Slots; // IReadOnly
            int invCount = invSlots.Count;
            var invItems = new Item[invCount];
            var invAmts  = new int[invCount];
            int emptyNow = 0;
            for (int i = 0; i < invCount; i++)
            {
                invItems[i] = invSlots[i].Item;
                invAmts[i]  = invSlots[i].Amount;
                if (invItems[i] == null || invAmts[i] <= 0) emptyNow++;
            }

            // --- Grid kopieren (nur Mengen relevant)
            var gridItems = new Item[16];
            var gridAmts  = new int[16];
            for (int i = 0; i < 16; i++)
            {
                var c = grid.GetCell(i);
                gridItems[i] = c.Item;
                gridAmts[i]  = c.Amount;
            }

            // --- Verbrauch simulieren (Inventar bevorzugt, Rest aus den vom Rezept verwendeten Zellen)
            int freedSlotsByConsumption = 0;

            foreach (var kv in needsPerItem)
            {
                Item item = kv.Key;
                int need  = kv.Value;
                if (item == null || need <= 0) { freedSlotsByConsumption = 0; break; }

                // aus Inventar
                for (int i = 0; i < invCount && need > 0; i++)
                {
                    if (invItems[i] != item || invAmts[i] <= 0) continue;
                    int take = Mathf.Min(invAmts[i], need);
                    invAmts[i] -= take;
                    need -= take;
                    if (invAmts[i] <= 0)
                    {
                        invItems[i] = null;
                        invAmts[i]  = 0;
                        freedSlotsByConsumption++; // Slot wird frei
                    }
                }
                // aus passenden Grid-Zellen
                for (int i = 0; i < 16 && need > 0; i++)
                {
                    var req = r.pattern[i];
                    if (req.item != item || req.amount <= 0) continue;
                    if (gridItems[i] != item || gridAmts[i] <= 0) continue;

                    int take = Mathf.Min(gridAmts[i], need);
                    gridAmts[i] -= take;
                    need -= take;
                    if (gridAmts[i] <= 0)
                    {
                        gridItems[i] = null;
                        gridAmts[i]  = 0;
                    }
                }
                if (need > 0)
                {
                    // unerwartet nicht gedeckt -> Rezept überspringen
                    freedSlotsByConsumption = 0;
                    goto NextRecipe;
                }
            }

            // --- Output-Einräumbarkeit simulieren
            int existingSpace = 0;
            if (r.outputItem)
            {
                for (int i = 0; i < invCount; i++)
                    if (invItems[i] == r.outputItem && invAmts[i] > 0)
                        existingSpace += Mathf.Max(0, r.outputItem.MaxStack - invAmts[i]);
            }

            int remainAfterExisting = Mathf.Max(0, r.outputAmount - existingSpace);
            int newStacksNeeded = (r.outputItem && r.outputItem.MaxStack > 0)
                ? Mathf.CeilToInt(remainAfterExisting / (float)r.outputItem.MaxStack)
                : 0;

            if (newStacksNeeded > emptyNow + freedSlotsByConsumption)
            {
                // nicht genug Slots, selbst nach Verbrauch
                goto NextRecipe;
            }

            // 3) WIRKLICH anwenden (jetzt ohne Überraschungen)

            // 3a) Verbrauch real: erst Inventar, dann Grid
            foreach (var kv in needsPerItem)
            {
                Item item = kv.Key;
                int need  = kv.Value;

                int tookInv = inventory.RemoveUpTo(item, need);
                int remaining = need - tookInv;

                for (int i = 0; i < 16 && remaining > 0; i++)
                {
                    var req = r.pattern[i];
                    if (req.item != item || req.amount <= 0) continue;

                    var cell = grid.GetCell(i);
                    if (cell.IsEmpty || cell.Item != item) continue;

                    int take = Mathf.Min(cell.Amount, remaining);
                    cell.Amount -= take;
                    remaining   -= take;
                    if (cell.Amount <= 0) cell.Clear();
                }

                if (remaining > 0)
                {
                    Debug.LogWarning($"[CraftingManager] Unerwartet: Bedarf {item?.name} nicht gedeckt.");
                    grid.RaiseChanged();
                    return false;
                }
            }

            // 3b) Output einsortieren
            int remainder = inventory.Add(r.outputItem, r.outputAmount);
            if (remainder > 0)
            {
                Debug.LogWarning("[CraftingManager] Unerwartet kein Platz für Output nach erfolgreicher Simulation.");
                grid.RaiseChanged();
                return false;
            }

            // 3c) Output-Highlight im UI (erstes passendes Inventar-Slot blinken)
            var invUI = FindObjectOfType<InventoryUI>();
            if (invUI != null && invUI.Slots != null)
            {
                for (int i = 0; i < inventory.Slots.Count; i++)
                {
                    var st = inventory.GetSlot(i);
                    if (!st.IsEmpty && st.Item == r.outputItem)
                    {
                        var slotUI = (i >= 0 && i < invUI.Slots.Length) ? invUI.Slots[i] : null;
                        if (slotUI != null)
                        {
                            var flash = slotUI.gameObject.GetComponent<SlotHighlightFlash>();
                            if (!flash) flash = slotUI.gameObject.AddComponent<SlotHighlightFlash>();
                            if (flash) flash.Flash();
                        }
                        break;
                    }
                }
            }

            // 3d) UI refresh (Inventory.Add feuert OnChanged; Grid manuell)
            grid.RaiseChanged();
            return true;

            // label für "continue outer foreach"
            NextRecipe: ;
        }

        return false;
    }

    /// Ermittelt Bedarf je Item, wenn die Form passt (shaped).
    /// Belegte Rezeptfelder: Grid-Zelle muss selben Item-Typ haben (Menge egal).
    /// Leere Rezeptfelder: Grid-Zelle muss leer sein.
    private bool ComputeNeedsIfShapeMatches(ShapedRecipe r, out Dictionary<Item, int> needsPerItem)
    {
        needsPerItem = new Dictionary<Item, int>();

        for (int i = 0; i < 16; i++)
        {
            var req  = r.pattern[i];
            var cell = grid.GetCell(i);

            bool reqEmpty = req.item == null || req.amount <= 0;
            if (reqEmpty)
            {
                if (!cell.IsEmpty) return false;
            }
            else
            {
                if (cell.IsEmpty || cell.Item != req.item) return false;
                if (!needsPerItem.ContainsKey(req.item)) needsPerItem[req.item] = 0;
                needsPerItem[req.item] += Mathf.Max(1, req.amount);
            }
        }
        return true;
    }
}
