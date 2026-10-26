using UnityEngine;

/// <summary>Generador y gestor de sonidos atmosféricos y mecánicos del Invernadero.</summary>
public static class INV_SoundHelper
{
    private const int SampleRate = 22050;

    /// <summary>Calor: zumbido eléctrico cálido continuo/corto.</summary>
    public static AudioClip CreateHeatHum()
    {
        return GenerateClip("INV_HumCalor", 1.8f, (t, i) =>
        {
            float env = Mathf.Min(t * 4f, 1f) * Mathf.Clamp01((1.8f - t) * 2f);
            float s1 = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.35f;
            float s2 = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.2f;
            float buzz = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.3f;
            return (s1 + s2 + buzz) * env * 0.45f;
        });
    }

    /// <summary>Oscuridad: clic oscuro profundo / encaje mecánico pesado.</summary>
    public static AudioClip CreateDarkClick()
    {
        return GenerateClip("INV_ClicOscuro", 0.9f, (t, i) =>
        {
            float env = Mathf.Exp(-t * 8f);
            float s1 = Mathf.Sin(2f * Mathf.PI * 75f * (1f - t * 0.5f) * t);
            float click = Mathf.Sin(2f * Mathf.PI * 340f * t) * Mathf.Exp(-t * 35f) * 0.6f;
            return (s1 * 0.6f + click) * env * 0.55f;
        });
    }

    /// <summary>Pulso: 'tum... tum...' rítmico como pasos pesados / latido de pulidora.</summary>
    public static AudioClip CreateStepPulse()
    {
        return GenerateClip("INV_PulsoPasos", 1.2f, (t, i) =>
        {
            float beat1 = (t < 0.55f) ? Mathf.Sin(2f * Mathf.PI * 82f * t) * Mathf.Exp(-t * 9f) : 0f;
            float t2 = t - 0.55f;
            float beat2 = (t2 >= 0f) ? Mathf.Sin(2f * Mathf.PI * 74f * t2) * Mathf.Exp(-t2 * 9f) : 0f;
            return (beat1 + beat2 * 0.85f) * 0.5f;
        });
    }

    /// <summary>Bruma: siseo suave y envolvente de vapor / difusor.</summary>
    public static AudioClip CreateMistHiss()
    {
        var rnd = new System.Random(42);
        return GenerateClip("INV_SiseoBruma", 2.0f, (t, i) =>
        {
            float env = Mathf.Min(t * 3f, 1f) * Mathf.Clamp01((2.0f - t) * 2f);
            float noise = ((float)rnd.NextDouble() * 2f - 1f) * 0.25f;
            float gentleWhistle = Mathf.Sin(2f * Mathf.PI * 1200f * t) * 0.04f;
            return (noise + gentleWhistle) * env * 0.5f;
        });
    }

    /// <summary>Floración: armonía etérea y luminosa cuando Ella florece.</summary>
    public static AudioClip CreateBloomHarmonic()
    {
        float[] notes = { 261.63f, 329.63f, 392.00f, 523.25f, 659.25f };
        return GenerateClip("INV_FloracionArmonica", 5.0f, (t, i) =>
        {
            float env = Mathf.Min(t * 1.5f, 1f) * Mathf.Clamp01((5.0f - t) * 0.5f);
            float sample = 0f;
            for (int n = 0; n < notes.Length; n++)
            {
                float delay = n * 0.45f;
                if (t >= delay)
                {
                    float noteT = t - delay;
                    sample += Mathf.Sin(2f * Mathf.PI * notes[n] * noteT) * Mathf.Exp(-noteT * 0.7f);
                }
            }
            return sample * env * 0.22f;
        });
    }

    /// <summary>Alarma de esporas: advertencia intermitente de emergencia biológica.</summary>
    public static AudioClip CreateAlarmSiren()
    {
        return GenerateClip("INV_AlarmaEsporas", 2.4f, (t, i) =>
        {
            float pulse = Mathf.Floor((t % 0.8f) / 0.4f) == 0 ? 1f : 0f;
            float pitch = 580f + Mathf.Sin(2f * Mathf.PI * 4f * t) * 90f;
            return Mathf.Sin(2f * Mathf.PI * pitch * t) * pulse * 0.25f;
        });
    }

    /// <summary>Clic mecánico al girar una perilla de afinación.</summary>
    public static AudioClip CreatePerillaClick()
    {
        return GenerateClip("INV_PerillaClick", 0.08f, (t, i) =>
        {
            float env = Mathf.Exp(-t * 70f);
            return Mathf.Sin(2f * Mathf.PI * 920f * t) * env * 0.35f;
        });
    }

    private static AudioClip GenerateClip(string name, float duration, System.Func<float, int, float> generator)
    {
        int sampleCount = Mathf.RoundToInt(duration * SampleRate);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            samples[i] = generator(t, i);
        }
        AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
