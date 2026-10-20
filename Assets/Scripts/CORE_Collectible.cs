using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class CORE_Collectible : MonoBehaviour
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    private CORE_PlayerController player;
    private void Reset() { GetComponent<Collider>().isTrigger=true; }
    private void OnTriggerEnter(Collider other) { if(other.TryGetComponent(out CORE_PlayerController p)) { player=p; CORE_InteractionHUD.SetPrompt(this,"E · Recoger " + displayName); } }
    private void OnTriggerExit(Collider other) { if(other.GetComponent<CORE_PlayerController>()) { player=null; CORE_InteractionHUD.ClearPrompt(this); } }
    private void Update() { if(player && player.InteractPressed) { CORE_InventoryBridge.AddItem(itemId); CORE_InteractionHUD.ClearPrompt(this); CORE_InteractionHUD.ShowMessage("Recogiste: " + displayName); gameObject.SetActive(false); } }
}
