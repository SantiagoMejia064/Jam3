using UnityEngine;

public sealed class CORE_CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 xLimits = new Vector2(-7, 24);
    [SerializeField] private Vector3 offset = new Vector3(0, 2, -12);
    [SerializeField, Range(0.1f, 20f)] private float smooth = 6f;
    private void LateUpdate()
    {
        if (!target) return;
        Vector3 p = target.position + offset; p.x = Mathf.Clamp(p.x, xLimits.x, xLimits.y);
        transform.position = Vector3.Lerp(transform.position, p, Time.deltaTime * smooth);
    }
    public void SetTarget(Transform value)
    {
        target = value;
        if (!target) return;
        Vector3 position = target.position + offset;
        position.x = Mathf.Clamp(position.x, xLimits.x, xLimits.y);
        transform.position = position;
    }
}
