using UnityEngine;

/// <summary>One focused world interaction; SAL_InteractionDriver selects the closest visible one.</summary>
public sealed class SAL_Interactable : MonoBehaviour
{
    public enum Kind { Exit, Trash, DiamondSeal, FilterLabel, Valve, Filter, PanelTriangle, PanelDiamond, PanelCross, Block, Drawings }
    public Kind kind;
    public string prompt;
    public int blockIndex;
    public bool holdToClean;
    [Min(0.5f)] public float reach = 1.75f;
}
