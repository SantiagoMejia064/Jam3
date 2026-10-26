using UnityEngine;

/// <summary>Ventanal del Invernadero hacia el jardín exterior. Sus plantas cambian tras la revelación y reproduce su AudioSource al inspeccionar.</summary>
[RequireComponent(typeof(Collider))]
public sealed class INV_Ventanal : MonoBehaviour
{
    [SerializeField] private string normalMessage = "Plantas verdes y frondosas en el jardín de la nave.";
    [SerializeField] private string revealedMessage = "Las plantas del jardín exterior se han vuelto negras... marchitas por las esporas.";

    [Header("Audio (AudioSource en este mismo objeto)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clipMirarVentanal;

    private CORE_PlayerController player;
    private bool inRange;

    private void Awake()
    {
        if (!audioSource)
        {
            audioSource = GetComponent<AudioSource>();
            if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void Start()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var pc = other.GetComponentInParent<CORE_PlayerController>();
        if (pc != null)
        {
            player = pc;
            inRange = true;
            CORE_InteractionHUD.SetPrompt(this, "E · Mirar por el ventanal");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<CORE_PlayerController>() != null)
        {
            player = null;
            inRange = false;
            CORE_InteractionHUD.ClearPrompt(this);
        }
    }

    private void OnDisable()
    {
        inRange = false;
        player = null;
        CORE_InteractionHUD.ClearPrompt(this);
    }

    private void Update()
    {
        if (!inRange || player == null) return;
        if (NotebookController.Instance && NotebookController.Instance.IsOpen) return;

        if (player.InteractPressed)
        {
            INV_SoundHelper.PlayEffect(
                audioSource,
                clipMirarVentanal,
                INV_SoundHelper.CreateWindowWindSound(),
                0.70f
            );

            bool revealed = SAL_Progress.HasFlag(SAL_Progress.Revealed);
            CORE_InteractionHUD.ShowMessage(revealed ? revealedMessage : normalMessage, 4f);
        }
    }
}
