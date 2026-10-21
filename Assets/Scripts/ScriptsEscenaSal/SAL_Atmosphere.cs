using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Maps story acts to URP saturation; a Volume is local to the current scene.</summary>
[RequireComponent(typeof(Volume))]
public sealed class SAL_Atmosphere : MonoBehaviour
{
    [SerializeField] private Light roomLight;
    private Volume volume;
    private ColorAdjustments color;

    private void Awake()
    {
        volume = GetComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10;
        volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        color = volume.profile.Add<ColorAdjustments>();
        color.saturation.overrideState = true;
        ApplyAct(SAL_Progress.Act);
    }

    private void OnEnable() { SAL_Progress.ActChanged += ApplyAct; }
    private void OnDisable() { SAL_Progress.ActChanged -= ApplyAct; }
    private void OnDestroy() { if (volume && volume.profile) Destroy(volume.profile); }

    private void ApplyAct(int act)
    {
        if (color == null) return;
        float saturation = act <= 2 ? -80 : act <= 4 ? -55 : act <= 6 ? -30 : act == 7 ? 0 : -100;
        color.saturation.value = saturation;
        if (roomLight) roomLight.color = act >= 8 ? new Color(0.62f, 0.68f, 0.77f) : new Color(1f, 0.53f, 0.25f);
    }
}
