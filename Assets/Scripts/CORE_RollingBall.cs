using UnityEngine;

/// <summary>Gives the revealed, scene-authored ball a short physical roll toward the player.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class CORE_RollingBall : MonoBehaviour
{
    [SerializeField] private Vector3 rollDirection = Vector3.left;
    [SerializeField, Min(0f)] private float rollImpulse = 1.8f;
    [SerializeField, Min(0f)] private float spinImpulse = 2.5f;

    private Rigidbody body;

    private void Awake() => body = GetComponent<Rigidbody>();

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        if (!body) body = GetComponent<Rigidbody>();
        if (!body) return;

        Vector3 direction = rollDirection.sqrMagnitude > 0f ? rollDirection.normalized : Vector3.left;
        body.AddForce(direction * rollImpulse, ForceMode.Impulse);
        body.AddTorque(Vector3.forward * -Mathf.Sign(direction.x) * spinImpulse, ForceMode.Impulse);
    }
}
