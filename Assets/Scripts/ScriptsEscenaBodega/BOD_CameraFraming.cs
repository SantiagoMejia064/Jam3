using UnityEngine;

/// <summary>Side-view follow constrained by the visible back wall, rather than by the player range.</summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Camera))]
public sealed class BOD_CameraFraming : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Renderer roomShell;
    [SerializeField] private Vector3 offset = new Vector3(0f, 4.7f, -16f);
    [SerializeField, Min(.1f)] private float smooth = 3.09f;
    [SerializeField, Min(0f)] private float edgeInset = .35f;
    private Camera view;
    private bool snapped;

    private void Awake() => view = GetComponent<Camera>();
    public void SetTarget(Transform value) { target = value; snapped = false; }

    private void LateUpdate()
    {
        if (!target || !roomShell) return;
        if (!view) view = GetComponent<Camera>();
        var desired = target.position + offset;
        // The first frame starts at the arrival; no flight in from an old editor position.
        transform.position = snapped
            ? Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-smooth * Time.deltaTime))
            : desired;
        ClampToWall();
        snapped = true;
    }

    public void SnapToTarget()
    {
        if (!target || !roomShell) return;
        if (!view) view = GetComponent<Camera>();
        transform.position = target.position + offset;
        ClampToWall();
        snapped = true;
    }

    private void ClampToWall()
    {
        var wall = roomShell.bounds;
        var position = transform.position;
        float minOffset = float.PositiveInfinity, maxOffset = float.NegativeInfinity;
        // Use all four corners: the scene's camera has a slight yaw and downward pitch.
        for (int y = 0; y < 2; y++)
        for (int x = 0; x < 2; x++)
        {
            var ray = view.ViewportPointToRay(new Vector3(x, y, 0));
            if (Mathf.Abs(ray.direction.z) < .0001f) continue;
            float distance = (wall.max.z - ray.origin.z) / ray.direction.z;
            if (distance <= 0f) continue;
            float hitX = ray.origin.x + ray.direction.x * distance - position.x;
            minOffset = Mathf.Min(minOffset, hitX);
            maxOffset = Mathf.Max(maxOffset, hitX);
        }
        if (float.IsInfinity(minOffset)) return;
        float left = wall.min.x + edgeInset - minOffset;
        float right = wall.max.x - edgeInset - maxOffset;
        position.x = left <= right ? Mathf.Clamp(position.x, left, right) : (left + right) * .5f;
        transform.position = position;
    }
}
