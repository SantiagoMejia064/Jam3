using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Scene-authored doorway. Assign destination scene and optional required item key in Inspector.</summary>
[RequireComponent(typeof(Collider))]
public sealed class CORE_ZoneDoor : MonoBehaviour
{
    [SerializeField] private string destinationScene;
    [SerializeField] private string requiredItem;
    [SerializeField, Tooltip("Temporary: lets this door open without its required item.")]
    private bool ignoreRequirementForDebug;
    [SerializeField] private string prompt = "E · Entrar";
    [SerializeField] private string lockedMessage = "Está sellada.";
    [SerializeField] private GameObject promptVisual;
    private CORE_PlayerController player;
    private Collider triggerCollider;
    private void Awake() { triggerCollider = GetComponent<Collider>(); }
    private void Reset() { GetComponent<Collider>().isTrigger=true; }
    private void OnTriggerEnter(Collider other) { if(isActiveAndEnabled && other.TryGetComponent(out CORE_PlayerController p)) { player=p; CORE_InteractionHUD.SetPrompt(this,prompt); } }
    private void OnTriggerExit(Collider other) { if(other.GetComponent<CORE_PlayerController>()) { player=null; CORE_InteractionHUD.ClearPrompt(this); } }
    private void OnDisable() { player = null; CORE_InteractionHUD.ClearPrompt(this); }
    private void Update()
    {
        if(!PlayerIsInRange())
        {
            if (player) CORE_InteractionHUD.ClearPrompt(this);
            player = null;
            return;
        }
        if(!player.InteractPressed) return;
        if(!ignoreRequirementForDebug && !string.IsNullOrEmpty(requiredItem) && !CORE_InventoryBridge.HasItem(requiredItem)) { CORE_InteractionHUD.ShowMessage(lockedMessage); return; }
        if(!string.IsNullOrEmpty(destinationScene) && Application.CanStreamedLevelBeLoaded(destinationScene))
        {
            if (GameSceneManager.Instance != null) GameSceneManager.Instance.LoadSceneByName(destinationScene);
            else SceneManager.LoadScene(destinationScene);
        }
        else CORE_InteractionHUD.ShowMessage("Esta zona todavía no está disponible.");
    }

    private bool PlayerIsInRange()
    {
        Collider playerCollider = player ? player.GetComponent<Collider>() : null;
        return triggerCollider && playerCollider && triggerCollider.bounds.Intersects(playerCollider.bounds);
    }
}

public static class CORE_InventoryBridge
{
    public static bool HasItem(string id)
    {
        return CORE_Inventory.Instance != null && CORE_Inventory.Instance.HasItem(id);
    }

    public static bool WasCollected(string id)
    {
        return CORE_Inventory.Instance != null && CORE_Inventory.Instance.WasCollected(id);
    }

    public static bool IsSelected(string id)
    {
        return CORE_Inventory.Instance != null && CORE_Inventory.Instance.IsSelected(id);
    }

    public static bool TryAddItem(string id)
    {
        return CORE_Inventory.Instance != null && CORE_Inventory.Instance.TryAddItem(id);
    }

    // Conserva la firma utilizada por las zonas actuales.
    public static void AddItem(string id)
    {
        TryAddItem(id);
    }

    public static void RemoveItem(string id)
    {
        if (CORE_Inventory.Instance != null) CORE_Inventory.Instance.RemoveItem(id);
    }

    public static void ClearLocalItems()
    {
        if (CORE_Inventory.Instance != null) CORE_Inventory.Instance.ResetForNewGame();
    }
}
