using UnityEngine;

/// <summary>SAL utiliza los mismos avisos que el resto de salas.</summary>
public sealed class SAL_UI : MonoBehaviour
{
    [SerializeField] private SAL_RoomController room;
    [SerializeField] private SAL_InteractionDriver interaction;
    private void LateUpdate()
    {
        bool notebookOpen = NotebookController.Instance && NotebookController.Instance.IsOpen;
        if (!room || !interaction || !interaction.Target || notebookOpen)
        {
            CORE_InteractionHUD.ClearPrompt(this);
            return;
        }
        var target = interaction.Target;
        string prompt = (target.holdToClean ? "Mantén E · " : "E · ") + target.prompt;
        CORE_InteractionHUD.SetPrompt(this, prompt);
    }

    private void OnDisable() => CORE_InteractionHUD.ClearPrompt(this);
}
