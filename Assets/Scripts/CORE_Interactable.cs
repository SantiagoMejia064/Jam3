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
        if (!string.IsNullOrEmpty(message)) CORE_InteractionHUD.ShowMessage(message, messageSeconds);
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

        if (!string.IsNullOrEmpty(message)) CORE_InteractionHUD.ShowMessage(message, messageSeconds);
        if (responseAudio)
        {
            responseAudio.Stop();
            responseAudio.Play();
            if (messageSeconds > 0f) StartCoroutine(StopResponseAudioAfter(messageSeconds));
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
        return !string.IsNullOrEmpty(revealFlag) && SAL_Progress.HasFlag(revealFlag) && !string.IsNullOrEmpty(alternateResponseMessage)
            ? alternateResponseMessage
            : responseMessage;
    }

    private string CurrentPrompt()
    {
        if (!string.IsNullOrEmpty(requiredItem) && !CORE_InventoryBridge.HasItem(requiredItem))
            return requiredItem == "Pelota" ? "No agarra." : prompt;
        if (requireSelectedItem && !string.IsNullOrEmpty(requiredItem) && !CORE_InventoryBridge.IsSelected(requiredItem))
            return "Selecciona " + requiredItem + " · Inventario";
        return prompt;
    }

    private void PlayAudioLog()
    {
        if (!string.IsNullOrEmpty(audioLogId) && SAL_AudioDirector.Instance)
            SAL_AudioDirector.Instance.PlayLog(audioLogId, SAL_Progress.HasFlag(SAL_Progress.Revealed), messageSeconds);
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
