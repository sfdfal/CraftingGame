using UnityEngine;

[CreateAssetMenu(menuName = "Items/Item")]
public class Item : ScriptableObject
{
    [Header("ID/Anzeige")]
    public string Id;
    public string DisplayName;

    [Header("Darstellung")]
    public Sprite Icon;

    [Header("Stacking")]
    [Min(1)] public int MaxStack = 99;
}