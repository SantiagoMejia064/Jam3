using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Jam3/Objeto")]
public class CORE_ItemData : ScriptableObject
{
    public string id;
    public string displayName;

    [TextArea]
    public string description;

    public Sprite icon;
}