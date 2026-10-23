using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class CORE_Collectible : MonoBehaviour
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    private CORE_PlayerController player;
    private Collider triggerCollider;
    private void Awake() { triggerCollider = GetComponent<Collider>(); }
    private void Start()
    {
        if (!string.IsNullOrEmpty(itemId) && CORE_InventoryBridge.HasItem(itemId)) gameObject.SetActive(false);
    }
    private void Reset() { GetComponent<Collider>().isTrigger=true; }
    private void OnTriggerEnter(Collider other) { if(other.TryGetComponent(out CORE_PlayerController p)) { player=p; CORE_InteractionHUD.SetPrompt(this,"E · Recoger " + displayName); } }
    private void OnTriggerExit(Collider other) { if(other.GetComponent<CORE_PlayerController>()) { player=null; CORE_InteractionHUD.ClearPrompt(this); } }
    private void Update()
    {
        if (!PlayerIsInRange())
        {
            if (player) CORE_InteractionHUD.ClearPrompt(this);
            player = null;
            return;
        }

        if(player.InteractPressed) { CORE_InventoryBridge.AddItem(itemId); CORE_InteractionHUD.ClearPrompt(this); CORE_InteractionHUD.ShowMessage("Recogiste: " + displayName); gameObject.SetActive(false); }
    }

    private bool PlayerIsInRange()
    {
        Collider playerCollider = player ? player.GetComponent<Collider>() : null;
        return triggerCollider && playerCollider && triggerCollider.bounds.Intersects(playerCollider.bounds);
    }
}
