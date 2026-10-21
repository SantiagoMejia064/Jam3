using UnityEngine;

/// <summary>Original temporary sound cues; replace with final licensed/recorded clips in the Inspector.</summary>
public static class SAL_ProceduralSound
{
    private const int Rate = 22050;

    public static AudioClip CreateIndustrialLoop() => Make("SAL_IndustrialPlaceholder", 4f, (t, i) =>
        0.032f * Mathf.Sin(2f * Mathf.PI * 58f * t) +
        0.014f * Mathf.Sin(2f * Mathf.PI * 117f * t) +
        0.016f * Mathf.Sin(2f * Mathf.PI * (320f + 25f * Mathf.Sin(t * 1.8f)) * t));

    public static AudioClip CreateMusic(bool light) => Make(light ? "SAL_LightPlaceholder" : "SAL_BasePlaceholder", 8f, (t, i) =>
    {
        float[] notes = { 164.81f, 196f, 246.94f, 220f };
        float note = notes[Mathf.FloorToInt(t / 2f) % notes.Length];
        float pulse = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 0.5f * t);
        float root = 0.035f * Mathf.Sin(2f * Mathf.PI * note * t);
        float upper = light ? 0.035f * Mathf.Sin(2f * Mathf.PI * (note * 2f) * t) : 0f;
        return (root + upper) * pulse;
    });

    public static AudioClip CreateStrings() => Make("SAL_StringsPlaceholder", 8f, (t, i) =>
        0.023f * Mathf.Sin(2f * Mathf.PI * 329.63f * t) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * .25f * t)));

    public static AudioClip CreateFilterEffect() => Make("SAL_FilterRemovedPlaceholder", 1f, (t, i) =>
        (1f - t) * 0.13f * Mathf.Sin(2f * Mathf.PI * (200f - t * 150f) * t));

    public static AudioClip CreatePulse() => Make("SAL_PulsePlaceholder", 0.6f, (t, i) =>
        Mathf.Exp(-t * 12f) * 0.14f * Mathf.Sin(2f * Mathf.PI * 83f * t));

    private static AudioClip Make(string name, float seconds, System.Func<float, int, float> sample)
    {
        int count = Mathf.RoundToInt(seconds * Rate);
        float[] data = new float[count];
        for (int i = 0; i < count; i++) data[i] = sample(i / (float)Rate, i);
        AudioClip clip = AudioClip.Create(name, count, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
