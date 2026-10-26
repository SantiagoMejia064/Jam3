using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public sealed class CORE_Collectible : MonoBehaviour
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [SerializeField] private UnityEvent onCollected = new UnityEvent();

    [Header("Glow opcional")]
    [SerializeField] private bool enableGlow;
    [SerializeField] private Color glowColor = new Color(0.15f, 0.9f, 1f, 1f);
    [SerializeField, Range(0f, 1f)] private float glowStrength = 0.7f;
    [SerializeField, Min(0f)] private float glowPulsesPerSecond = 1.8f;
    [SerializeField, Tooltip("Déjalo vacío para usar los modelos del objeto y sus hijos.")]
    private Renderer[] glowRenderers;

    private CORE_PlayerController player;
    private Collider triggerCollider;
    private MaterialPropertyBlock glowBlock;
    private MaterialPropertyBlock[] originalGlowBlocks;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int ImportedColorId = Shader.PropertyToID("_BASE_COLOR");

    void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (glowRenderers == null || glowRenderers.Length == 0)
            glowRenderers = GetComponentsInChildren<Renderer>(true);

        glowBlock = new MaterialPropertyBlock();
        originalGlowBlocks = new MaterialPropertyBlock[glowRenderers.Length];
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
        ClearGlow();
    }

    void Update()
    {
        UpdateGlow();

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

    void UpdateGlow()
    {
        if (!enableGlow)
        {
            ClearGlow();
            return;
        }

        float pulse = (Mathf.Sin(Time.time * glowPulsesPerSecond * Mathf.PI * 2f) + 1f) * 0.5f;

        for (int i = 0; i < glowRenderers.Length; i++)
        {
            Renderer model = glowRenderers[i];
            if (!model || !model.enabled || !model.gameObject.activeInHierarchy) continue;
            if (model.GetComponent<TextMesh>()) continue;

            Material material = model.sharedMaterial;
            if (!material) continue;
            bool hasBaseColor = material.HasProperty(BaseColorId);
            bool hasColor = material.HasProperty(ColorId);
            bool hasImportedColor = material.HasProperty(ImportedColorId);
            if (!hasBaseColor && !hasColor && !hasImportedColor) continue;

            // Conservamos los valores originales para restaurarlos al quitar el glow.
            if (originalGlowBlocks[i] == null)
            {
                originalGlowBlocks[i] = new MaterialPropertyBlock();
                model.GetPropertyBlock(originalGlowBlocks[i]);
            }

            // Algunos modelos FBX usan Arnold Standard Surface y su color está en mayúsculas.
            int colorProperty = hasImportedColor ? ImportedColorId :
                hasBaseColor ? BaseColorId : ColorId;
            Color baseColor = originalGlowBlocks[i].HasColor(colorProperty)
                ? originalGlowBlocks[i].GetColor(colorProperty)
                : material.GetColor(colorProperty);

            Color pulsingColor = Color.Lerp(baseColor, glowColor, pulse * glowStrength);
            pulsingColor.a = baseColor.a;

            model.GetPropertyBlock(glowBlock);
            if (hasBaseColor) glowBlock.SetColor(BaseColorId, pulsingColor);
            if (hasColor) glowBlock.SetColor(ColorId, pulsingColor);
            if (hasImportedColor) glowBlock.SetColor(ImportedColorId, pulsingColor);
            model.SetPropertyBlock(glowBlock);
        }
    }

    void ClearGlow()
    {
        if (originalGlowBlocks == null) return;

        for (int i = 0; i < originalGlowBlocks.Length; i++)
        {
            if (originalGlowBlocks[i] == null) continue;
            if (glowRenderers[i]) glowRenderers[i].SetPropertyBlock(originalGlowBlocks[i]);
            originalGlowBlocks[i] = null;
        }
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
