using UnityEngine;

[System.Serializable]
public struct Ingredient
{
    public Item item;
    [Min(1)] public int amount;
}

[CreateAssetMenu(menuName = "Crafting/Shaped Recipe (4x4)")]
public class ShapedRecipe : ScriptableObject
{
    [Tooltip("Muss 16 Elemente (4x4) enthalten. Leere Zellen = kein Item verlangt.")]
    public Ingredient[] pattern = new Ingredient[16];

    [Header("Output")]
    public Item outputItem;
    [Min(1)] public int outputAmount = 1;
}
