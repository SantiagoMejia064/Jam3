using UnityEngine;

/// <summary>Scene-authored proximity light driven by the existing interaction prompt.</summary>
[DisallowMultipleComponent]
public sealed class BOD_InteractionGlow : MonoBehaviour
{
    [SerializeField] private Behaviour interaction;
    [SerializeField] private Light proximityLight;

    private void LateUpdate()
    {
        if (proximityLight)
            proximityLight.enabled = interaction && interaction.isActiveAndEnabled &&
                CORE_InteractionHUD.HasPrompt(interaction);
    }

    private void OnDisable()
    {
        if (proximityLight) proximityLight.enabled = false;
    }
}
