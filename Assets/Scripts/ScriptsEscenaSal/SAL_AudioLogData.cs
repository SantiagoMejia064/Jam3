using UnityEngine;

[CreateAssetMenu(fileName = "SAL_AudioLog", menuName = "Jam3/SAL/Audio log")]
public sealed class SAL_AudioLogData : ScriptableObject
{
    public string id;
    [TextArea(2, 5)] public string shortSubtitle;
    [TextArea(2, 8)] public string fullSubtitle;
    public AudioClip shortClip;
    public AudioClip fullClip;
}
