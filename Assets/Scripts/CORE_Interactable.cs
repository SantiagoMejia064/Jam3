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
    [SerializeField] private GameObject promptVisual;
    [SerializeField] private bool oneShot;
    private bool used;
    private CORE_PlayerController player;
    private void Reset() { GetComponent<Collider>().isTrigger = true; }
    private void OnTriggerEnter(Collider other) { var p=other.GetComponent<CORE_PlayerController>(); if (p) { player=p; CORE_InteractionHUD.SetPrompt(this, prompt); } }
    private void OnTriggerExit(Collider other) { if (other.GetComponent<CORE_PlayerController>()) { player=null; CORE_InteractionHUD.ClearPrompt(this); } }
    private void Update() { if (player && (!oneShot || !used) && player.InteractPressed) Interact(); }
    public void Interact()
    {
        if(oneShot && used) return;
        if(!string.IsNullOrEmpty(requiredItem) && !CORE_InventoryBridge.HasItem(requiredItem)) { CORE_InteractionHUD.ShowMessage(lockedMessage); return; }
        onInteract?.Invoke();
        if(!string.IsNullOrEmpty(resultItem)) CORE_InventoryBridge.AddItem(resultItem);
        if(!string.IsNullOrEmpty(responseMessage)) CORE_InteractionHUD.ShowMessage(responseMessage);
        used=true;
    }
    public string Prompt => prompt;
}
