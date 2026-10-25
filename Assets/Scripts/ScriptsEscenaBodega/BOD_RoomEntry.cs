using UnityEngine;

/// <summary>Aligns the actual persistent player's feet with this room's authored floor.</summary>
public sealed class BOD_RoomEntry : MonoBehaviour
{
    [SerializeField] private Transform arrival;
    [SerializeField] private Transform cameraTarget;
    private Collider playerBody;

    private void Start()
    {
        if (SAL_Progress.Act < 4) SAL_Progress.SetAct(4);
        var player = PlayerPersistence.Instance
            ? PlayerPersistence.Instance.GetComponent<CORE_PlayerController>()
            : FindFirstObjectByType<CORE_PlayerController>();
        if (!player || !arrival) return;
        playerBody = player.GetComponent<Collider>();
        if (!playerBody) return;
        Physics.SyncTransforms();
        Bounds bounds = playerBody.bounds;
        Vector3 shift = new Vector3(arrival.position.x - bounds.center.x,
            arrival.position.y - bounds.min.y + .02f, 0f);
        Vector3 position = player.transform.position + shift;
        player.transform.position = position;
        if (player.TryGetComponent(out Rigidbody body))
        {
            body.position = position;
            body.linearVelocity = Vector3.zero;
        }
        Physics.SyncTransforms();
        FollowBody();
        foreach (var follow in FindObjectsByType<CORE_CameraFollow>(FindObjectsSortMode.None))
            if (cameraTarget) follow.SetTarget(cameraTarget);
        foreach (var framing in FindObjectsByType<BOD_CameraFraming>(FindObjectsSortMode.None))
            if (cameraTarget) framing.SetTarget(cameraTarget);
    }

    private void Update() => FollowBody();
    private void FollowBody()
    {
        if (playerBody && cameraTarget) cameraTarget.position = playerBody.bounds.center;
    }
}
