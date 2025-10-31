using System.Collections.Generic;
using UnityEngine;

namespace CraftingGame.Crafting
{
    /// <summary>
    /// Defines a shaped crafting recipe using a 3x3 pattern.
    /// </summary>
    [System.Serializable]
    public class CraftingRecipe
    {
        [SerializeField]
        private string name;

        [SerializeField]
        private string[] pattern = new string[3];

        [SerializeField]
        private List<LegendEntry> legend = new List<LegendEntry>();

        [SerializeField]
        private ItemStack output;

        public string Name => name;
        public string[] Pattern => pattern;
        public IReadOnlyList<LegendEntry> Legend => legend;
        public ItemStack Output => output;

        public CraftingRecipe(string name, string[] pattern, Dictionary<char, ItemData> legend, ItemStack output)
        {
            this.name = name;
            this.pattern = pattern;
            this.output = output;
            this.legend = new List<LegendEntry>();
            foreach (var kvp in legend)
            {
                this.legend.Add(new LegendEntry(kvp.Key, kvp.Value));
            }
        }

        [System.Serializable]
        public struct LegendEntry
        {
            public char Symbol;
            public ItemData Item;

            public LegendEntry(char symbol, ItemData item)
            {
                Symbol = symbol;
                Item = item;
            }
        }

        public ItemData GetItemForSymbol(char symbol)
        {
            for (int i = 0; i < legend.Count; i++)
            {
                if (legend[i].Symbol == symbol)
                {
                    return legend[i].Item;
                }
            }

            return null;
        }
    }
}
