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
    [SerializeField, Tooltip("Optional stamp flag; leave empty for ordinary dirt.")]
    private string stampId;
    private CORE_PlayerController player;
    private Collider triggerCollider;
    private float progress;
    private bool cleaned;
    private void Awake() { triggerCollider = GetComponent<Collider>(); }
    private void Start()
    {
        if (!CORE_GameState.IsStamp(stampId)) return;
        cleaned = CORE_GameState.HasStamp(stampId);
        ApplyStampVisuals();
    }
    private void ApplyStampVisuals()
    {
        if (dustVisual) dustVisual.SetActive(!cleaned);
        if (revealOnClean) revealOnClean.SetActive(cleaned);
        if (revealOnCleanExtra) revealOnCleanExtra.SetActive(cleaned);
        if (cleaned && triggerCollider) triggerCollider.enabled = false;
    }
    private void Reset() { GetComponent<Collider>().isTrigger=true; }
    private void OnTriggerEnter(Collider other) { if(!cleaned && other.TryGetComponent(out CORE_PlayerController p)) { player=p; CORE_InteractionHUD.SetPrompt(this,prompt); } }
    private void OnDisable() { player = null; progress = 0; CORE_InteractionHUD.ClearPrompt(this); }
    private void OnTriggerExit(Collider other) { if(other.GetComponent<CORE_PlayerController>()) { player=null; progress=0; CORE_InteractionHUD.ClearPrompt(this); } }
    private void Update()
    {
        if (!PlayerIsInRange())
        {
            if (player) CORE_InteractionHUD.ClearPrompt(this);
            player = null;
            progress = 0;
            return;
        }
        if(cleaned || !player || !player.InteractHeld) { if(!cleaned) progress=0; return; }
        progress += Time.deltaTime;
        if(progress >= cleanSeconds)
        {
            cleaned=true;
            onCleaned?.Invoke();
            if(revealOnClean) revealOnClean.SetActive(true);
            if(revealOnCleanExtra) revealOnCleanExtra.SetActive(true);
            if(dustVisual) dustVisual.SetActive(false); else gameObject.SetActive(false);
            CORE_InteractionHUD.ClearPrompt(this);
            if (CORE_GameState.IsStamp(stampId))
            {
                CORE_GameState.RegisterStamp(stampId);
                ApplyStampVisuals();
            }
            else CORE_InteractionHUD.ShowMessage("Acción completada.");
        }
    }

    private bool PlayerIsInRange()
    {
        Collider playerCollider = player ? player.GetComponent<Collider>() : null;
        return triggerCollider && playerCollider && triggerCollider.bounds.Intersects(playerCollider.bounds);
    }
}
