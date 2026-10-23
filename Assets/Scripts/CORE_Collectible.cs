using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public sealed class CORE_Collectible : MonoBehaviour
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [SerializeField] private UnityEvent onCollected = new UnityEvent();

    private CORE_PlayerController player;
    private Collider triggerCollider;

    void Awake()
    {
        triggerCollider = GetComponent<Collider>();
    }

    void Start()
    {
        if (CORE_Inventory.Instance != null && CORE_InventoryBridge.WasCollected(itemId))
        {
            gameObject.SetActive(false);
        }
    }

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out CORE_PlayerController found))
        {
            player = found;
            CORE_InteractionHUD.SetPrompt(this, "E · Recoger " + displayName);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (player != null && other.GetComponent<CORE_PlayerController>() == player)
        {
            ClearInteraction();
        }
    }

    void OnDisable()
    {
        ClearInteraction();
    }

    void Update()
    {
        if (!PlayerIsInRange())
        {
            ClearInteraction();
            return;
        }

        if (NotebookController.Instance != null && NotebookController.Instance.IsOpen)
        {
            return;
        }

        if (!player.InteractPressed)
            return;

        if (CORE_Inventory.Instance == null)
        {
            CORE_InteractionHUD.ShowMessage("El inventario no está disponible.");
            return;
        }

        if (CORE_InventoryBridge.WasCollected(itemId))
        {
            gameObject.SetActive(false);
            return;
        }

        bool collected = CORE_InventoryBridge.TryAddItem(itemId);

        if (!collected)
        {
            CORE_InteractionHUD.ShowMessage("No se pudo recoger este objeto.");
            return;
        }

        // El sonido y las demás acciones ocurren solo si se recogió.
        onCollected?.Invoke();

        CORE_InteractionHUD.ShowMessage("Recogiste: " + displayName);

        gameObject.SetActive(false);
    }

    void ClearInteraction()
    {
        player = null;
        CORE_InteractionHUD.ClearPrompt(this);
    }

    bool PlayerIsInRange()
    {
        Collider playerCollider = player ? player.GetComponent<Collider>() : null;

        return triggerCollider && playerCollider && triggerCollider.bounds.Intersects(playerCollider.bounds);
    }
}
