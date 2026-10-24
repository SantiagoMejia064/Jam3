using UnityEngine;
using UnityEngine.Events;

/// <summary>Hold E while near this marked dust patch to reveal its scene-authored reward.</summary>
[RequireComponent(typeof(Collider))]
public sealed class CORE_Dirt : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float cleanSeconds = 1f;
    [SerializeField] private string completedMessage = "Action completed.";
    [SerializeField] private string prompt = "Mantén E · Limpiar";
    [SerializeField] private UnityEvent onCleaned = new UnityEvent();
    [SerializeField] private GameObject promptVisual;
    [SerializeField] private GameObject dustVisual;
    [SerializeField] private GameObject revealOnClean;
    [SerializeField] private GameObject revealOnCleanExtra;
    [SerializeField] private AudioClip cleanSound;
    [SerializeField] private AudioSource cleanAudioSource;
    [SerializeField] private Color glowColor = new Color(0.15f, 0.9f, 1f, 1f);
    [SerializeField, Range(0f, 1f)] private float glowStrength = 0.7f;
    [SerializeField, Min(0f)] private float glowPulsesPerSecond = 1.8f;
    [SerializeField, Tooltip("Optional stamp flag; leave empty for ordinary dirt.")]
    private string stampId;
    [SerializeField] private string cleanFlag;
    [SerializeField, Tooltip("Optional story flag set when this patch is cleaned.")]
    private string resultFlag;
    [SerializeField, Range(-1, 9)] private int notebookIndex = -1;
    [SerializeField] private string noteTitle;
    [SerializeField, TextArea(1, 3)] private string noteContent;
    [SerializeField, TextArea(1, 3)] private string noteRereading;
    [SerializeField] private bool rollRevealedBall;
    [SerializeField, Min(0f)] private float revealedBallImpulse = 1.8f;
    [SerializeField, Min(0f)] private float revealedBallSpinImpulse = 2.5f;

    private CORE_PlayerController player;
    private Collider triggerCollider;
    private Renderer pulseRenderer;
    private MaterialPropertyBlock pulseBlock;
    private float progress;
    private bool cleaned;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        pulseRenderer = dustVisual ? dustVisual.GetComponent<Renderer>() : GetComponent<Renderer>();
        if (!pulseRenderer && dustVisual) pulseRenderer = dustVisual.GetComponentInChildren<Renderer>();
        pulseBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        cleaned = (!string.IsNullOrEmpty(cleanFlag) && SAL_Progress.HasFlag(cleanFlag)) || CORE_GameState.HasStamp(stampId);
        if (cleaned) ApplyStampVisuals();
    }

    private void ApplyStampVisuals()
    {
        if (dustVisual) dustVisual.SetActive(!cleaned);
        if (revealOnClean) revealOnClean.SetActive(cleaned);
        if (revealOnCleanExtra) revealOnCleanExtra.SetActive(cleaned);
        if (cleaned && triggerCollider) triggerCollider.enabled = false;
    }

    private void Reset() { GetComponent<Collider>().isTrigger = true; }

    private void OnTriggerEnter(Collider other)
    {
        if (!cleaned && other.TryGetComponent(out CORE_PlayerController p))
        {
            player = p;
            CORE_InteractionHUD.SetPrompt(this, prompt);
        }
    }

    private void OnDisable()
    {
        player = null;
        progress = 0;
        CORE_InteractionHUD.ClearPrompt(this);
        ClearPulse();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.GetComponent<CORE_PlayerController>()) return;
        player = null;
        progress = 0;
        CORE_InteractionHUD.ClearPrompt(this);
    }

    private void Update()
    {
        if (!cleaned) PulseDust();
        if (!PlayerIsInRange())
        {
            if (player) CORE_InteractionHUD.ClearPrompt(this);
            player = null;
            progress = 0;
            return;
        }

        if (cleaned || !player || !player.InteractHeld)
        {
            if (!cleaned) progress = 0;
            return;
        }

        progress += Time.deltaTime;
        if (progress < cleanSeconds) return;

        cleaned = true;
        if (cleanAudioSource)
        {
            if (cleanSound) cleanAudioSource.PlayOneShot(cleanSound);
            else cleanAudioSource.Play();
        }
        ClearPulse();
        onCleaned?.Invoke();
        if (revealOnClean) revealOnClean.SetActive(true);
        if (revealOnCleanExtra)
        {
            revealOnCleanExtra.SetActive(true);
                if (rollRevealedBall && revealOnCleanExtra.TryGetComponent(out Rigidbody ballBody))
                {
                    ballBody.AddForce(Vector3.left * revealedBallImpulse, ForceMode.Impulse);
                    ballBody.AddTorque(Vector3.back * revealedBallSpinImpulse, ForceMode.Impulse);
                }
        }
        if (dustVisual) dustVisual.SetActive(false);
        else gameObject.SetActive(false);
        CORE_InteractionHUD.ClearPrompt(this);

        if (notebookIndex >= 0 && NotebookController.Instance)
            NotebookController.Instance.CollectNote(notebookIndex, noteTitle, noteContent, noteRereading);
        if (!string.IsNullOrEmpty(cleanFlag)) SAL_Progress.SetFlag(cleanFlag);
        if (!string.IsNullOrEmpty(resultFlag)) SAL_Progress.SetFlag(resultFlag);
        if (CORE_GameState.IsStamp(stampId))
        {
            CORE_GameState.RegisterStamp(stampId);
            ApplyStampVisuals();
        }
        CORE_InteractionHUD.ShowMessage(completedMessage);
    }

    private void PulseDust()
    {
        if (!pulseRenderer || !pulseRenderer.enabled || !pulseRenderer.gameObject.activeInHierarchy) return;
        float pulse = (Mathf.Sin(Time.time * glowPulsesPerSecond * Mathf.PI * 2f) + 1f) * 0.5f;
        pulseRenderer.GetPropertyBlock(pulseBlock);
        Material material = pulseRenderer.sharedMaterial;
        Color baseColor = material && material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) :
                          material && material.HasProperty(ColorId) ? material.GetColor(ColorId) : Color.white;
        Color pulsingColor = Color.Lerp(baseColor, glowColor, pulse * glowStrength);
        if (material && material.HasProperty(BaseColorId)) pulseBlock.SetColor(BaseColorId, pulsingColor);
        if (material && material.HasProperty(ColorId)) pulseBlock.SetColor(ColorId, pulsingColor);
        pulseRenderer.SetPropertyBlock(pulseBlock);
    }

    private void ClearPulse()
    {
        if (pulseRenderer) pulseRenderer.SetPropertyBlock(null);
    }

    private bool PlayerIsInRange()
    {
        Collider playerCollider = player ? player.GetComponent<Collider>() : null;
        return triggerCollider && playerCollider && triggerCollider.bounds.Intersects(playerCollider.bounds);
    }
}
