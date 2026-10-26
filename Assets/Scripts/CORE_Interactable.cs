using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Attach to a scene object with a trigger collider. E invokes its Inspector event.</summary>
[RequireComponent(typeof(Collider))]
public sealed class CORE_Interactable : MonoBehaviour
{
    [SerializeField] private string prompt = "Interactuar [E]";
    [SerializeField] private UnityEvent onInteract = new UnityEvent();
    [SerializeField] private string requiredItem;
    [SerializeField] private string resultItem;
    [SerializeField] private string lockedMessage = "No sirve aquí.";
    [SerializeField] private string responseMessage;
    [SerializeField] private string secondResponseMessage;
    [SerializeField] private string alternateResponseMessage;
    [SerializeField] private string revealFlag;
    [SerializeField, Min(0f)] private float messageSeconds = 3.5f;
    [SerializeField] private string audioLogId;
    [SerializeField] private bool playOnStart;
    [SerializeField] private bool playAudioOnApproach;
    [SerializeField] private string playOnStartOnceFlag;
    [SerializeField] private string requiredFlag;
    [SerializeField] private string resultFlag;
    [SerializeField] private bool triggerOnApproach;
    [SerializeField] private AudioSource responseAudio;
    [SerializeField] private bool requireSelectedItem;
    [Header("Nota del Cuaderno (opcional)")]
    [SerializeField, Range(-1, 9)] private int notebookIndex = -1;
    [SerializeField] private string notebookTitle;
    [SerializeField, TextArea(1, 3)] private string notebookContent;
    [SerializeField, TextArea(1, 3)] private string notebookRereading;
    [SerializeField] private GameObject[] successVisualsToHide;
    [SerializeField] private GameObject promptVisual;
    [SerializeField] private bool oneShot;

    private bool used;
    private int readCount;
    private string displayedPrompt;
    private CORE_PlayerController player;
    private Collider triggerCollider;

    private void Awake() { triggerCollider = GetComponent<Collider>(); }

    private void Start()
    {
        if (oneShot && ((!string.IsNullOrEmpty(resultItem) && CORE_InventoryBridge.WasCollected(resultItem)) ||
                        (notebookIndex >= 0 && NotebookController.Instance && NotebookController.Instance.IsNoteUnlocked(notebookIndex)) ||
                        (!string.IsNullOrEmpty(resultFlag) && SAL_Progress.HasFlag(resultFlag))))
        {
            used = true;
            HideSuccessVisuals();
        }
        if (used) return;
        if (!playOnStart) return;
        if (!string.IsNullOrEmpty(playOnStartOnceFlag) && SAL_Progress.HasFlag(playOnStartOnceFlag)) return;
        PlayAudioLog();
        string message = GetCurrentMessage();
        if (!string.IsNullOrEmpty(message)) CORE_InteractionHUD.ShowMessage(message, EffectiveMessageSeconds());
        if (!string.IsNullOrEmpty(playOnStartOnceFlag)) SAL_Progress.SetFlag(playOnStartOnceFlag);
    }

    private void Reset() { GetComponent<Collider>().isTrigger = true; }

    private void OnTriggerEnter(Collider other)
    {
        if (oneShot && used) return;
        if (!other.TryGetComponent(out CORE_PlayerController foundPlayer)) return;
        player = foundPlayer;
        displayedPrompt = CurrentPrompt();
        CORE_InteractionHUD.SetPrompt(this, displayedPrompt);
        if (playAudioOnApproach && !triggerOnApproach) PlayAudioLog();
        if (triggerOnApproach) Interact();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.GetComponent<CORE_PlayerController>()) return;
        player = null;
        displayedPrompt = null;
        CORE_InteractionHUD.ClearPrompt(this);
    }

    private void Update()
    {
        if (oneShot && used) { CORE_InteractionHUD.ClearPrompt(this); return; }
        if (NotebookController.Instance && NotebookController.Instance.IsOpen) return;
        if (!PlayerIsInRange())
        {
            if (player) CORE_InteractionHUD.ClearPrompt(this);
            player = null;
            return;
        }

        if ((!oneShot || !used) && player.InteractPressed && !triggerOnApproach)
            Interact();

        if (!isActiveAndEnabled || (oneShot && used)) return;

        string currentPrompt = CurrentPrompt();
        if (currentPrompt == displayedPrompt) return;
        displayedPrompt = currentPrompt;
        CORE_InteractionHUD.SetPrompt(this, displayedPrompt);
    }

    public void Interact()
    {
        if (NotebookController.Instance && NotebookController.Instance.IsOpen) return;
        if (oneShot && used) return;
        if (!string.IsNullOrEmpty(requiredItem) && !CORE_InventoryBridge.HasItem(requiredItem))
        {
            CORE_InteractionHUD.ShowMessage(lockedMessage);
            return;
        }
        if (requireSelectedItem && !string.IsNullOrEmpty(requiredItem) && !CORE_InventoryBridge.IsSelected(requiredItem))
        {
            CORE_InteractionHUD.ShowMessage("Selecciona " + requiredItem + " en el inventario.");
            return;
        }
        if (!string.IsNullOrEmpty(requiredFlag) && !SAL_Progress.HasFlag(requiredFlag))
        {
            CORE_InteractionHUD.ShowMessage(lockedMessage);
            return;
        }

        string message = readCount > 0 && !string.IsNullOrEmpty(secondResponseMessage)
            ? secondResponseMessage
            : GetCurrentMessage();
        onInteract?.Invoke();
        if (!string.IsNullOrEmpty(resultItem)) CORE_InventoryBridge.AddItem(resultItem);
        PlayAudioLog();

        float duration = EffectiveMessageSeconds();
        if (!string.IsNullOrEmpty(message)) CORE_InteractionHUD.ShowMessage(message, duration);
        if (responseAudio)
        {
            responseAudio.Stop();
            responseAudio.Play();
            if (duration > 0f) StartCoroutine(StopResponseAudioAfter(duration));
        }
        if (notebookIndex >= 0 && NotebookController.Instance)
            NotebookController.Instance.CollectNote(notebookIndex, notebookTitle, notebookContent, notebookRereading);
        if (!string.IsNullOrEmpty(resultFlag)) SAL_Progress.SetFlag(resultFlag);
        HideSuccessVisuals();

        readCount++;
        used = true;
        if (oneShot) CORE_InteractionHUD.ClearPrompt(this);
    }

    private void HideSuccessVisuals()
    {
        if (successVisualsToHide != null)
            foreach (GameObject visual in successVisualsToHide)
                if (visual) visual.SetActive(false);
    }

    private void OnDisable()
    {
        player = null;
        displayedPrompt = null;
        CORE_InteractionHUD.ClearPrompt(this);
    }

    private string GetCurrentMessage()
    {
        string effectiveLog = ResolveEffectiveAudioLogId();

        // GDD pág. 15 (El final · La caminata): Al pasar frente a la cabina sellada de Vega llevando a Ella en el carrito, Vega tose y repite su petición
        if (effectiveLog == "A2" && SAL_Progress.HasFlag(INV_IncubatorCart.FlagEllaEnCarrito))
        {
            return "Vega: \"*cof*... Suéltala, Siete. Por favor...\"";
        }

        if (!string.IsNullOrEmpty(revealFlag) && SAL_Progress.HasFlag(revealFlag) && !string.IsNullOrEmpty(alternateResponseMessage))
        {
            return alternateResponseMessage;
        }

        if (SAL_Progress.HasFlag(SAL_Progress.Revealed))
        {
            if (effectiveLog == "A2")
            {
                return "A2 · ¡No puedes sacar eso de aquí! La alarma de esporas ya sonaba.";
            }
            if (!string.IsNullOrEmpty(responseMessage) && responseMessage.StartsWith("A1", StringComparison.Ordinal))
            {
                return "A1 · Motivo: eliminar apego. Este proceso no causa dolor.";
            }
        }

        return responseMessage;
    }

    private float EffectiveMessageSeconds()
    {
        string effectiveLog = ResolveEffectiveAudioLogId();
        if (effectiveLog == "A2" && (SAL_Progress.HasFlag(SAL_Progress.Revealed) || SAL_Progress.HasFlag(INV_IncubatorCart.FlagEllaEnCarrito)))
        {
            return Mathf.Max(4.5f, messageSeconds);
        }
        return messageSeconds;
    }

    private string CurrentPrompt()
    {
        if (!string.IsNullOrEmpty(requiredItem) && !CORE_InventoryBridge.HasItem(requiredItem))
            return requiredItem == "Pelota" ? "No agarra." : prompt;
        if (requireSelectedItem && !string.IsNullOrEmpty(requiredItem) && !CORE_InventoryBridge.IsSelected(requiredItem))
            return "Selecciona " + requiredItem + " · Inventario";
        return prompt;
    }

    private string ResolveEffectiveAudioLogId()
    {
        if (!string.IsNullOrEmpty(audioLogId)) return audioLogId.Trim();

        // Si el mismo objeto ya tiene CORE_ScreenWake (ej. A1 en Javier), CORE_ScreenWake gestiona su propio PlayVegaLog en onInteract
        if (GetComponent<CORE_ScreenWake>() != null) return string.Empty;

        if (!string.IsNullOrEmpty(responseMessage))
        {
            if (responseMessage.StartsWith("A1 ", StringComparison.Ordinal) || responseMessage.StartsWith("A1·", StringComparison.Ordinal)) return "A1";
            if (responseMessage.StartsWith("A2 ", StringComparison.Ordinal) || responseMessage.StartsWith("A2·", StringComparison.Ordinal)) return "A2";
            if (responseMessage.StartsWith("A3 ", StringComparison.Ordinal) || responseMessage.StartsWith("A3·", StringComparison.Ordinal)) return "A3";
            if (responseMessage.StartsWith("A4 ", StringComparison.Ordinal) || responseMessage.StartsWith("A4·", StringComparison.Ordinal)) return "A4";
        }
        return string.Empty;
    }

    private void PlayAudioLog()
    {
        string effectiveLog = ResolveEffectiveAudioLogId();
        if (string.IsNullOrEmpty(effectiveLog)) return;

        // En la caminata final con el carrito hacia la esclusa, Vega repite su súplica debilitado detrás de la puerta de su cabina
        if (effectiveLog == "A2" && SAL_Progress.HasFlag(INV_IncubatorCart.FlagEllaEnCarrito))
        {
            SAL_AudioDirector.EnsureInstance().PlayVegaCustomLine(
                "Vega: \"*cof*... Suéltala, Siete. Por favor...\"",
                variant: 3,
                subtitleSeconds: 4.5f
            );
            return;
        }

        SAL_AudioDirector.PlayVegaLog(effectiveLog, SAL_Progress.HasFlag(SAL_Progress.Revealed), Mathf.Max(3.5f, EffectiveMessageSeconds()));
    }

    private IEnumerator StopResponseAudioAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (responseAudio) responseAudio.Stop();
    }

    public string Prompt => prompt;

    private bool PlayerIsInRange()
    {
        Collider playerCollider = player ? player.GetComponent<Collider>() : null;
        return triggerCollider && playerCollider && triggerCollider.bounds.Intersects(playerCollider.bounds);
    }
}
