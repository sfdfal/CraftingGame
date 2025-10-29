using UnityEngine;

namespace CraftingGame.Crafting
{
    /// <summary>
    /// Lightweight runtime representation of an item that can be crafted or stored in the inventory.
    /// </summary>
    [System.Serializable]
    public class ItemData
    {
        [SerializeField]
        private string id;

        [SerializeField]
        private string displayName;

        [SerializeField]
        private Sprite icon;

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Icon => icon;

        public ItemData(string id, string displayName, Sprite icon)
        {
            this.id = id;
            this.displayName = displayName;
            this.icon = icon;
        }
    }
}
