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
    [SerializeField] private string alternateResponseMessage;
    [SerializeField] private string revealFlag;
    [SerializeField, Min(0f)] private float messageSeconds = 3.5f;
    [SerializeField] private string audioLogId;
    [SerializeField] private bool playOnStart;
    [SerializeField] private string playOnStartOnceFlag;
    [SerializeField] private string requiredFlag;
    [SerializeField] private string resultFlag;
    [SerializeField] private GameObject promptVisual;
    [SerializeField] private bool oneShot;
    private bool used;
    private CORE_PlayerController player;
    private Collider triggerCollider;
    private void Awake() { triggerCollider = GetComponent<Collider>(); }
    private void Start()
    {
        if (!playOnStart) return;
        if (!string.IsNullOrEmpty(playOnStartOnceFlag) && SAL_Progress.HasFlag(playOnStartOnceFlag)) return;
        if (!string.IsNullOrEmpty(audioLogId) && SAL_AudioDirector.Instance)
            SAL_AudioDirector.Instance.PlayLog(audioLogId, false, messageSeconds);
        string message = GetCurrentMessage();
        if (!string.IsNullOrEmpty(message)) CORE_InteractionHUD.ShowMessage(message, messageSeconds);
        if (!string.IsNullOrEmpty(playOnStartOnceFlag)) SAL_Progress.SetFlag(playOnStartOnceFlag);
    }
    private void Reset() { GetComponent<Collider>().isTrigger = true; }
    private void OnTriggerEnter(Collider other) { var p=other.GetComponent<CORE_PlayerController>(); if (p) { player=p; CORE_InteractionHUD.SetPrompt(this, prompt); } }
    private void OnTriggerExit(Collider other) { if (other.GetComponent<CORE_PlayerController>()) { player=null; CORE_InteractionHUD.ClearPrompt(this); } }
    private void Update()
    {
        if (!PlayerIsInRange())
        {
            if (player) CORE_InteractionHUD.ClearPrompt(this);
            player = null;
            return;
        }

        if (!oneShot || !used)
        {
            if (player.InteractPressed) Interact();
        }
    }
    public void Interact()
    {
        if(oneShot && used) return;
        if(!string.IsNullOrEmpty(requiredItem) && !CORE_InventoryBridge.HasItem(requiredItem)) { CORE_InteractionHUD.ShowMessage(lockedMessage); return; }
        if(!string.IsNullOrEmpty(requiredFlag) && !SAL_Progress.HasFlag(requiredFlag)) { CORE_InteractionHUD.ShowMessage(lockedMessage); return; }
        onInteract?.Invoke();
        if(!string.IsNullOrEmpty(resultItem)) CORE_InventoryBridge.AddItem(resultItem);
        if(!string.IsNullOrEmpty(resultFlag)) SAL_Progress.SetFlag(resultFlag);
        if(!string.IsNullOrEmpty(audioLogId) && SAL_AudioDirector.Instance)
            SAL_AudioDirector.Instance.PlayLog(audioLogId, false, messageSeconds);
        string message = GetCurrentMessage();
        if(!string.IsNullOrEmpty(message)) CORE_InteractionHUD.ShowMessage(message, messageSeconds);
        used=true;
    }
    private string GetCurrentMessage()
    {
        return !string.IsNullOrEmpty(revealFlag) && SAL_Progress.HasFlag(revealFlag) && !string.IsNullOrEmpty(alternateResponseMessage)
            ? alternateResponseMessage : responseMessage;
    }
    public string Prompt => prompt;

    private bool PlayerIsInRange()
    {
        Collider playerCollider = player ? player.GetComponent<Collider>() : null;
        return triggerCollider && playerCollider && triggerCollider.bounds.Intersects(playerCollider.bounds);
    }
}
