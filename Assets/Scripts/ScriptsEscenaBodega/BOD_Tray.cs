using UnityEngine;

/// <summary>Hold to clean, release, then press E to inspect the scene-authored number.</summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class BOD_Tray : MonoBehaviour
{
    [SerializeField] private GameObject dirtVisual;
    [SerializeField] private GameObject numberVisual;
    [SerializeField, Min(.1f)] private float cleanSeconds = 1f;
    [SerializeField] private string cleanFlag;
    private CORE_PlayerController player;
    private Collider area;
    private float elapsed;
    private bool cleaned;
    private bool waitingForRelease;
    public bool IsCleaning { get; private set; }
    public float Progress01 => Mathf.Clamp01(elapsed / cleanSeconds);

    private void Awake()
    {
        area = GetComponent<Collider>();
        if (numberVisual) numberVisual.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out CORE_PlayerController found)) player = found;
    }

    private void Start()
    {
        cleaned = SAL_Progress.HasFlag(cleanFlag);
        if (dirtVisual) dirtVisual.SetActive(!cleaned);
    }

    private void OnTriggerExit(Collider other)
    {
        if (player && other.GetComponent<CORE_PlayerController>() == player) Leave();
    }

    private void Update()
    {
        IsCleaning = false;
        if (NotebookController.Instance && NotebookController.Instance.IsOpen)
        {
            elapsed = 0;
            return;
        }
        if (!player) return;
        var body = player.GetComponent<Collider>();
        if (!body || !area.bounds.Intersects(body.bounds)) { Leave(); return; }
        if (cleaned)
        {
            CORE_InteractionHUD.SetPrompt(this, "E · Ver / ocultar número de la bandeja");
            if (waitingForRelease) { if (!player.InteractHeld) waitingForRelease = false; return; }
            if (player.InteractPressed && numberVisual) numberVisual.SetActive(!numberVisual.activeSelf);
            return;
        }
        elapsed = player.InteractHeld ? Mathf.Min(cleanSeconds, elapsed + Time.deltaTime) : 0f;
        IsCleaning = player.InteractHeld && elapsed < cleanSeconds;
        CORE_InteractionHUD.SetPrompt(this, "Mantén E · Limpiar bandeja");
        if (elapsed < cleanSeconds) return;
        cleaned = true;
        SAL_Progress.SetFlag(cleanFlag);
        waitingForRelease = true;
        if (dirtVisual) dirtVisual.SetActive(false);
        CORE_InteractionHUD.ShowMessage("Bandeja limpia. Suelta E y pulsa E para ver su número.");
    }

    private void Leave()
    {
        IsCleaning = false;
        elapsed = 0f;
        player = null;
        if (numberVisual) numberVisual.SetActive(false);
        CORE_InteractionHUD.ClearPrompt(this);
    }
    private void OnDisable() => Leave();
}
