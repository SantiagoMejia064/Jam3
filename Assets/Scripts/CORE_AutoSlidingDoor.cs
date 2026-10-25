using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Slides an already placed scene panel upward while Siete is near its trigger.
/// The panel and trigger are authored in the scene hierarchy; this component only animates them.
/// </summary>
[DisallowMultipleComponent]
public sealed class CORE_AutoSlidingDoor : MonoBehaviour
{
    [SerializeField] private Transform slidingPanel;
    [SerializeField, Min(0.1f)] private float openDistance = 3.2f;
    [SerializeField, Min(0.1f)] private float slideSpeed = 3f;

    private readonly HashSet<CORE_PlayerController> nearbyPlayers = new HashSet<CORE_PlayerController>();
    private Vector3 closedLocalPosition;

    private void Awake()
    {
        if (slidingPanel)
            closedLocalPosition = slidingPanel.localPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        CORE_PlayerController player = other.GetComponentInParent<CORE_PlayerController>();
        if (player)
            nearbyPlayers.Add(player);
    }

    private void OnTriggerExit(Collider other)
    {
        CORE_PlayerController player = other.GetComponentInParent<CORE_PlayerController>();
        if (player)
            nearbyPlayers.Remove(player);
    }

    private void Update()
    {
        if (!slidingPanel)
            return;

        Vector3 target = closedLocalPosition + Vector3.up * (nearbyPlayers.Count > 0 ? openDistance : 0f);
        slidingPanel.localPosition = Vector3.MoveTowards(
            slidingPanel.localPosition,
            target,
            slideSpeed * Time.deltaTime);
    }

    private void OnDisable()
    {
        nearbyPlayers.Clear();
    }
}
