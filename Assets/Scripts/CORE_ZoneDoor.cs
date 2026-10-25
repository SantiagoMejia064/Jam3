using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Scene-authored doorway with optional inventory and story-state requirements.</summary>
[RequireComponent(typeof(Collider))]
public sealed class CORE_ZoneDoor : MonoBehaviour
{
    [SerializeField] private string destinationScene;
    [SerializeField] private string requiredItem;
    [SerializeField, Tooltip("Acceso de prueba: omite los requisitos de esta puerta.")]
    private bool ignoreRequirementForDebug;
    [SerializeField] private string additionalRequiredItem;
    [SerializeField] private bool requireSelectedItem;
    [SerializeField] private string requiredFlag;
    [SerializeField] private string missingFlagMessage;
    [SerializeField] private string prompt = "E · Entrar";
    [SerializeField] private string lockedMessage = "Está sellada.";
    [SerializeField] private string openMessage;
    [SerializeField] private string alternateOpenMessage;
    [SerializeField] private string revealFlag;
    [SerializeField, Min(0f)] private float openMessageSeconds = 2f;
    [SerializeField] private GameObject promptVisual;

    private CORE_PlayerController player;
    private Collider triggerCollider;
    private bool loadingScene;

    private void Awake() { triggerCollider = GetComponent<Collider>(); }
    private void Reset() { GetComponent<Collider>().isTrigger = true; }

    private void OnTriggerEnter(Collider other)
    {
        if (isActiveAndEnabled && other.TryGetComponent(out CORE_PlayerController foundPlayer))
        {
            player = foundPlayer;
            CORE_InteractionHUD.SetPrompt(this, prompt);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.GetComponent<CORE_PlayerController>()) return;
        player = null;
        CORE_InteractionHUD.ClearPrompt(this);
    }

    private void OnDisable()
    {
        player = null;
        CORE_InteractionHUD.ClearPrompt(this);
    }

    private void Update()
    {
        if (!PlayerIsInRange())
        {
            if (player) CORE_InteractionHUD.ClearPrompt(this);
            player = null;
            return;
        }
        if (!player.InteractPressed || loadingScene) return;
        if (!ignoreRequirementForDebug && !string.IsNullOrEmpty(requiredItem) && !CORE_InventoryBridge.HasItem(requiredItem))
        {
            CORE_InteractionHUD.ShowMessage(lockedMessage);
            return;
        }
        if (!ignoreRequirementForDebug && requireSelectedItem && !string.IsNullOrEmpty(requiredItem) && !CORE_InventoryBridge.IsSelected(requiredItem))
        {
            CORE_InteractionHUD.ShowMessage("Selecciona " + requiredItem + " en el inventario.");
            return;
        }
        if (!ignoreRequirementForDebug && !string.IsNullOrEmpty(additionalRequiredItem) && !CORE_InventoryBridge.HasItem(additionalRequiredItem))
        {
            CORE_InteractionHUD.ShowMessage(lockedMessage);
            return;
        }
        if (!ignoreRequirementForDebug && !string.IsNullOrEmpty(requiredFlag) && !SAL_Progress.HasFlag(requiredFlag))
        {
            CORE_InteractionHUD.ShowMessage(string.IsNullOrEmpty(missingFlagMessage) ? lockedMessage : missingFlagMessage);
            return;
        }
        if (string.IsNullOrEmpty(destinationScene) || !Application.CanStreamedLevelBeLoaded(destinationScene))
        {
            CORE_InteractionHUD.ShowMessage("Esta zona todavía no está disponible.");
            return;
        }

        string message = !string.IsNullOrEmpty(revealFlag) && SAL_Progress.HasFlag(revealFlag) && !string.IsNullOrEmpty(alternateOpenMessage)
            ? alternateOpenMessage
            : openMessage;
        if (!string.IsNullOrEmpty(message)) CORE_InteractionHUD.ShowMessage(message, openMessageSeconds);
        if (openMessageSeconds > 0f && !string.IsNullOrEmpty(message)) StartCoroutine(LoadAfterMessage());
        else LoadDestination();
    }

    private IEnumerator LoadAfterMessage()
    {
        loadingScene = true;
        yield return new WaitForSecondsRealtime(openMessageSeconds);
        LoadDestination();
    }

    private void LoadDestination()
    {
        if (GameSceneManager.Instance != null) GameSceneManager.Instance.LoadSceneByName(destinationScene);
        else SceneManager.LoadScene(destinationScene);
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
