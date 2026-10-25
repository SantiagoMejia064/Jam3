using UnityEngine;

/// <summary>Uses the shared Siete input but gives SAL only one active target at a time.</summary>
public sealed class SAL_InteractionDriver : MonoBehaviour
{
    [SerializeField] private CORE_PlayerController player;
    [SerializeField] private SAL_RoomController room;
    private SAL_Interactable[] targets;
    private float holdProgress;
    private float readyAt;
    private SAL_Interactable previous;

    public SAL_Interactable Target { get; private set; }
    public float HoldProgress => holdProgress;

    private void Start()
    {
        // SAL ya no usa el Siete local duplicado de la escena; toma al jugador persistente que viene del Pasillo.
        if (!player || !player.isActiveAndEnabled)
        {
            PlayerPersistence persistentPlayer = PlayerPersistence.Instance;
            if (persistentPlayer) player = persistentPlayer.GetComponent<CORE_PlayerController>();
        }
        targets = FindObjectsByType<SAL_Interactable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        readyAt = Time.time + .35f;
    }

    private void Update()
    {
        if (!player || !room || targets == null || Time.time < readyAt) return;
        SAL_Interactable closest = null;
        float distance = float.MaxValue;
        foreach (SAL_Interactable candidate in targets)
        {
            if (!room.IsAvailable(candidate)) continue;
            float dx = Mathf.Abs(candidate.transform.position.x - player.transform.position.x);
            if (dx <= candidate.reach && dx < distance) { closest = candidate; distance = dx; }
        }
        Target = closest;
        if (previous != closest) { holdProgress = 0f; previous = closest; }
        if (!closest) return;
        if (closest.holdToClean)
        {
            if (!player.InteractHeld) { holdProgress = 0f; return; }
            holdProgress += Time.deltaTime;
            if (holdProgress >= 1f) { holdProgress = 0f; room.Use(closest); }
        }
        else if (player.InteractPressed) room.Use(closest);
    }
}
