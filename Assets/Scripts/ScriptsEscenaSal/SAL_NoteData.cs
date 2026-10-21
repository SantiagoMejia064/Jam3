using UnityEngine;

[CreateAssetMenu(fileName = "SAL_Nota", menuName = "Jam3/SAL/Nota")]
public sealed class SAL_NoteData : ScriptableObject
{
    public string id;
    [TextArea(1, 3)] public string visibleText;
    [TextArea(1, 3)] public string revealedText;
}
