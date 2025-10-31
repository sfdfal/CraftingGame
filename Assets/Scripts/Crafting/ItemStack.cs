namespace CraftingGame.Crafting
{
    /// <summary>
    /// Represents a stack of identical items.
    /// </summary>
    [System.Serializable]
    public struct ItemStack
    {
        public ItemData Item;
        public int Count;

        public bool IsEmpty => Item == null || Count <= 0;

        public ItemStack(ItemData item, int count)
        {
            Item = item;
            Count = count;
        }

        public ItemStack Split(int amount)
        {
            if (IsEmpty || amount <= 0)
            {
                return default;
            }

            var removed = new ItemStack(Item, UnityEngine.Mathf.Min(amount, Count));
            Count -= removed.Count;
            if (Count <= 0)
            {
                Item = null;
                Count = 0;
            }

            return removed;
        }

        public void Merge(ItemStack other)
        {
            if (other.IsEmpty)
            {
                return;
            }

            if (IsEmpty)
            {
                Item = other.Item;
                Count = other.Count;
                return;
            }

            if (other.Item == Item)
            {
                Count += other.Count;
            }
        }
    }
}
