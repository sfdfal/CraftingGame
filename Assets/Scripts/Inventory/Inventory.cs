using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Min(1)] public int capacity = 20;
    [SerializeField] private List<ItemStack> slots = new List<ItemStack>();

    [System.Serializable]
    public class Seed
    {
        public Item item;
        public int amount = 1;
    }
    [Header("Start-Befüllung (optional)")]
    public List<Seed> startItems = new List<Seed>();

    public IReadOnlyList<ItemStack> Slots => slots;
    public event System.Action OnChanged;
private void Awake()
{
    // Slots initialisieren
    for (int i = slots.Count; i < capacity; i++) slots.Add(new ItemStack());

    // START-ITEMS => HIER (nicht mehr in Start)
    foreach (var s in startItems)
    {
        if (s?.item != null && s.amount > 0)
            Add(s.item, s.amount);   // packt ins Inventar
    }
}

// ENTFERNE (oder leere) das alte Start():
// private void Start() { ... }  // <- nicht mehr nötig


public int GetTotalAmount(Item item)
{
    if (item == null) return 0;
    int total = 0;
    foreach (var s in slots) if (s.Item == item) total += s.Amount;
    return total;
}

/// <summary>Entfernt bis zu 'amount' eines Items und gibt die tatsächlich entfernte Menge zurück.</summary>
public int RemoveUpTo(Item item, int amount)
{
    if (item == null || amount <= 0) return 0;
    int remaining = amount;
    for (int i = 0; i < slots.Count && remaining > 0; i++)
    {
        var st = slots[i];
        if (st.Item != item) continue;
        int take = Mathf.Min(st.Amount, remaining);
        st.Amount -= take;
        remaining -= take;
        if (st.Amount <= 0) st.Clear();
    }
    int removed = amount - remaining;
    if (removed > 0) OnChanged?.Invoke();
    return removed;
}

    public ItemStack GetSlot(int index) => slots[index];

    public bool CanAdd(Item item, int amount)
    {
        if (item == null || amount <= 0) return false;
        int remaining = amount;

        // 1) Fülle existierende Stacks gleichen Typs
        foreach (var st in slots)
        {
            if (st.Item == item)
            {
                int addable = Mathf.Min(st.RemainingSpace, remaining);
                remaining -= addable;
                if (remaining <= 0) return true;
            }
        }
        // 2) Zähle freie Slots
        foreach (var st in slots)
        {
            if (st.IsEmpty)
            {
                int addable = Mathf.Min(item.MaxStack, remaining);
                remaining -= addable;
                if (remaining <= 0) return true;
            }
        }
        return remaining <= 0;
    }

    /// <summary>Gibt zurück, wie viel NICHT einsortiert werden konnte.</summary>
    public int Add(Item item, int amount)
    {
        if (item == null || amount <= 0) return amount;
            int remaining = amount;

        // 1) Bestehende Stacks auffüllen
        foreach (var st in slots)
        {
            if (st.Item == item && st.Amount < item.MaxStack)
            {
                int addable = Mathf.Min(item.MaxStack - st.Amount, remaining);
                st.Amount += addable;
                remaining -= addable;
                if (remaining <= 0) break;
            }
        }
        // 2) Leere Slots befüllen
        foreach (var st in slots)
        {
            if (st.IsEmpty)
            {
                int put = Mathf.Min(item.MaxStack, remaining);
                st.Item = item;
                st.Amount = put;
                remaining -= put;
                if (remaining <= 0) break;
            }
        }
        int added = amount - remaining;
        if (added > 0) OnChanged?.Invoke();
        return remaining;
    }

    public bool Remove(Item item, int amount)
    {
        if (item == null || amount <= 0) return false;
        int remaining = amount;

        // Prüfe Verfügbarkeit
        int total = 0;
        foreach (var st in slots) if (st.Item == item) total += st.Amount;
        if (total < amount) return false;

        // Entferne
        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            var st = slots[i];
            if (st.Item != item) continue;
            int take = Mathf.Min(st.Amount, remaining);
            st.Amount -= take;
            remaining -= take;
            if (st.Amount <= 0) st.Clear();
        }
        OnChanged?.Invoke();
        return true;
    }

    // Hilfsfunktionen für Slot-zu-Slot Operationen
    public static void Swap(ItemStack a, ItemStack b)
    {
        var ai = a.Item; var aa = a.Amount;
        a.Item = b.Item; a.Amount = b.Amount;
        b.Item = ai; b.Amount = aa;
    }

    /// <summary>Versucht b in a zu mergen (gleicher Item-Typ). Gibt die Menge zurück, die in b übrig bleibt.</summary>
    public static int Merge(ItemStack target, ItemStack source)
    {
        if (source.IsEmpty) return 0;
        if (target.IsEmpty)
        {
            int move = Mathf.Min(source.Amount, source.Item.MaxStack);
            target.Item = source.Item;
            target.Amount = move;
            source.Amount -= move;
            if (source.Amount <= 0) source.Clear();
            return source.Amount;
        }
        if (target.Item != source.Item) return source.Amount;

        int addable = Mathf.Min(target.Item.MaxStack - target.Amount, source.Amount);
        target.Amount += addable;
        source.Amount -= addable;
        if (source.Amount <= 0) source.Clear();
        return source.Amount;
    }

    public static int MoveQuantity(ItemStack from, ItemStack to, int quantity)
    {
        if (from == null || to == null) return 0;
        if (from.IsEmpty || quantity <= 0) return 0;

        // Wenn Ziel leer: neuen Stack beginnen
        if (to.IsEmpty)
        {
            int move = Mathf.Min(quantity, from.Amount, from.Item.MaxStack);
            to.Item = from.Item;
            to.Amount = move;
            from.Amount -= move;
            if (from.Amount <= 0) from.Clear();
            return move;
        }

        // Wenn gleicher Typ: in bestehenden Stack hinein
        if (to.Item == from.Item)
        {
            int space = Mathf.Max(0, to.Item.MaxStack - to.Amount);
            int move = Mathf.Min(space, quantity, from.Amount);
            to.Amount += move;
            from.Amount -= move;
            if (from.Amount <= 0) from.Clear();
            return move;
        }

        // Unterschiedlicher Typ und Ziel belegt -> kein Teil-Transfer möglich
        // (Swap nur sinnvoll, wenn man alles tauscht – das behandeln wir außerhalb gezielt)
        return 0;
    }
    public int GetEmptySlotCount()
{
    int c = 0;
    foreach (var s in slots) if (s.IsEmpty) c++;
    return c;
}

public int GetExistingStackSpace(Item item)
{
    if (item == null) return 0;
    int space = 0;
    foreach (var s in slots)
        if (!s.IsEmpty && s.Item == item)
            space += Mathf.Max(0, item.MaxStack - s.Amount);
    return space;
}

/// <summary>
/// Schätzt, wie viele Inventarslots nach dem geplanten Verbrauch leer werden.
/// Wir zählen, wie viele Stacks durch die Entnahme auf 0 fallen.
/// </summary>
public int PredictEmptySlotsAfterConsumption(Dictionary<Item, int> needsPerItem)
{
    if (needsPerItem == null || needsPerItem.Count == 0) return 0;

    // Simpler, aber verlässlicher Ansatz:
    // Wir laufen über die Slots und ziehen von den Needs ab; jedes Mal,
    // wenn ein Stack auf 0 fällt, entsteht potentiell 1 freier Slot.
    var remaining = new Dictionary<Item, int>();
    foreach (var kv in needsPerItem) if (kv.Key != null && kv.Value > 0) remaining[kv.Key] = kv.Value;

    int newEmptySlots = 0;

    for (int i = 0; i < slots.Count; i++)
    {
        var s = slots[i];
        if (s.IsEmpty) continue;
        if (!remaining.TryGetValue(s.Item, out int need) || need <= 0) continue;

        int take = Mathf.Min(need, s.Amount);
        int after = s.Amount - take;
        remaining[s.Item] = need - take;

        if (after <= 0) newEmptySlots++;
    }

    return newEmptySlots;
}


}
