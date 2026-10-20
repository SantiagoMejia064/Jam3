using UnityEngine;

/// <summary>Reusable scene event target for graybox story interactions.</summary>
public sealed class CORE_StoryAction : MonoBehaviour
{
    [SerializeField] private string actionLabel;
    public void SetActionLabel(string value) { actionLabel = value; }
    public void Report() { CORE_InteractionHUD.ShowMessage(actionLabel); }
    public void Show(GameObject target) { if(target) target.SetActive(true); }
    public void Hide(GameObject target) { if(target) target.SetActive(false); }
}
