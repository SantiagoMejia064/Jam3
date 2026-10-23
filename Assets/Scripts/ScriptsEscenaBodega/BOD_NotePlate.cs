using UnityEngine;
using UnityEngine.Events;

/// <summary>Reusable N4 plate interaction with a distinct first read and reread.</summary>
[RequireComponent(typeof(Collider))]
public sealed class BOD_NotePlate : MonoBehaviour
{
    [SerializeField] private string prompt = "E · Leer placa N4";
    [TextArea, SerializeField] private string firstRead = "Él mató las otras semillas.";
    [TextArea, SerializeField] private string reread = "¿Por qué más grises junto al ducto?";
    [SerializeField, Range(0, NotebookController.NoteCount - 1)] private int notebookIndex = 3;

    [SerializeField] private UnityEvent onCollected = new UnityEvent();
    private CORE_PlayerController player;
    private Collider triggerCollider;

    private void Awake() => triggerCollider = GetComponent<Collider>();
    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out CORE_PlayerController found))
        {
            player = found;
            CORE_InteractionHUD.SetPrompt(this, $"E · Recoger nota N{notebookIndex + 1}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<CORE_PlayerController>())
        {
            player = null;
            CORE_InteractionHUD.ClearPrompt(this);
        }
    }

    private void Update()
    {
        if (!PlayerIsInRange())
        {
            if (player) CORE_InteractionHUD.ClearPrompt(this);
            player = null;
            return;
        }

        var notebook = NotebookController.Instance;
        if (!player.InteractPressed || (notebook && notebook.IsOpen)) return;
        if (!notebook)
        {
            CORE_InteractionHUD.ShowMessage("El cuaderno no está disponible. Inicia la partida desde Inicio.");
            return;
        }
        if (notebook.IsNoteUnlocked(notebookIndex))
        {
            CORE_InteractionHUD.ShowMessage("Ya recogiste esta nota. Consúltala en Cuaderno [Tab].");
            return;
        }
        notebook.CollectNote(notebookIndex, "Placa de la Bodega Fría", firstRead, reread);
        onCollected?.Invoke();
        CORE_InteractionHUD.ClearPrompt(this);
        CORE_InteractionHUD.ShowMessage($"Nota N{notebookIndex + 1} añadida al Cuaderno [Tab].");
    }

    private bool PlayerIsInRange()
    {
        Collider playerCollider = player ? player.GetComponent<Collider>() : null;
        return triggerCollider && playerCollider && triggerCollider.bounds.Intersects(playerCollider.bounds);
    }
}
