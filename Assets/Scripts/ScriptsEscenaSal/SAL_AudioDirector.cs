using System.Collections;
using UnityEngine;

/// <summary>Director persistente de ambiente, efectos y voz de Vega. La musica pertenece a cada escena.</summary>
public sealed class SAL_AudioDirector : MonoBehaviour
{
    [SerializeField] private SAL_AudioLogData[] logs;
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioSource ambienceSource;
    // Referencias antiguas: se conservan para detener las capas de escenas anteriores.
    [SerializeField, HideInInspector] private AudioSource musicSource;
    [SerializeField, HideInInspector] private AudioSource stringsSource;
    [SerializeField, HideInInspector] private AudioSource lightSource;
    [SerializeField] private AudioSource effectsSource;
    [SerializeField] private AudioClip industrialLoop;
    [SerializeField] private AudioClip filterRemovedEffect;
    [SerializeField] private AudioClip stepPulse;

    public static SAL_AudioDirector Instance { get; private set; }
    public string Subtitle { get; private set; }
    public float SubtitleUntil { get; private set; }
    public float LastVegaVoiceTime { get; private set; } = -999f;
    private Coroutine subtitleRoutine;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureVoiceSource();
        if (!industrialLoop) industrialLoop = SAL_ProceduralSound.CreateIndustrialLoop();
        if (musicSource) { musicSource.Stop(); musicSource.playOnAwake = false; }
        if (stringsSource) { stringsSource.Stop(); stringsSource.playOnAwake = false; }
        if (lightSource) { lightSource.Stop(); lightSource.playOnAwake = false; }
        if (!filterRemovedEffect) filterRemovedEffect = SAL_ProceduralSound.CreateFilterEffect();
        if (!stepPulse) stepPulse = SAL_ProceduralSound.CreatePulse();
        if (ambienceSource && industrialLoop) { ambienceSource.clip = industrialLoop; ambienceSource.loop = true; ambienceSource.Play(); }
        if (SAL_Progress.HasFlag(SAL_Progress.FilterRemoved) && ambienceSource) ambienceSource.pitch = 0.78f;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    /// <summary>Garantiza que exista siempre un director de audio activo para reproducir la voz matemática de Vega en cualquier escena.</summary>
    public static SAL_AudioDirector EnsureInstance()
    {
        if (Instance != null) return Instance;

        SAL_AudioDirector existing = FindFirstObjectByType<SAL_AudioDirector>();
        if (existing != null)
        {
            Instance = existing;
            existing.EnsureVoiceSource();
            return existing;
        }

        GameObject go = new GameObject("CORE_AudioDirector_Runtime");
        return go.AddComponent<SAL_AudioDirector>();
    }

    /// <summary>Reproduce un log de Vega (A1..A4) garantizando siempre una instancia activa y el respaldo matemático de voz.</summary>
    public static void PlayVegaLog(string id, bool complete = false, float subtitleSeconds = 5f)
    {
        if (string.IsNullOrEmpty(id)) return;
        EnsureInstance().PlayLog(id, complete, subtitleSeconds);
    }

    private void EnsureVoiceSource()
    {
        if (voiceSource == null)
        {
            voiceSource = INV_SoundHelper.FindOrCreateChildSource(transform, "Audio_Vega_Voice", 0.92f, loop: false);
        }
        if (voiceSource != null)
        {
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.spatialBlend = 0f;
        }
    }

    /// <summary>Indica si el log indicado tiene un AudioClip real asignado en el asset ScriptableObject.</summary>
    public bool HasAssignedClip(string id, bool complete)
    {
        if (logs == null || string.IsNullOrEmpty(id)) return false;
        foreach (SAL_AudioLogData log in logs)
        {
            if (!log || log.id != id) continue;
            AudioClip clip = complete ? log.fullClip : log.shortClip;
            return clip != null;
        }
        return false;
    }

    public void PlayLog(string id) => PlayLog(id, false);

    public void PlayLog(string id, bool complete) => PlayLog(id, complete, 5f);

    public void PlayLog(string id, bool complete, float subtitleSeconds)
    {
        if (string.IsNullOrEmpty(id)) return;

        EnsureVoiceSource();

        AudioClip clip = null;
        string text = null;
        bool foundInArray = false;

        if (logs != null)
        {
            foreach (SAL_AudioLogData log in logs)
            {
                if (!log || log.id != id) continue;
                clip = complete ? log.fullClip : log.shortClip;
                text = complete ? log.fullSubtitle : log.shortSubtitle;
                foundInArray = true;
                break;
            }
        }

        if (!foundInArray || string.IsNullOrEmpty(text))
        {
            text = GetDefaultSubtitleForLog(id, complete);
        }

        if (voiceSource)
        {
            voiceSource.Stop();
            voiceSource.spatialBlend = 0f;

            if (clip != null)
            {
                voiceSource.clip = clip;
                voiceSource.Play();
            }
            else
            {
                int variant = GetVegaVariantForLog(id, complete);
                AudioClip vegaMathClip = INV_SoundHelper.CreateVegaVoiceTransmission(variant);
                INV_SoundHelper.PlayEffect(voiceSource, null, vegaMathClip, 0.92f);
            }
            LastVegaVoiceTime = Time.unscaledTime;
        }

        if (!string.IsNullOrEmpty(text))
        {
            ShowSubtitle(text, clip ? Mathf.Max(3f, clip.length) : Mathf.Max(0.1f, subtitleSeconds));
        }
    }

    /// <summary>Reproduce una intervención directa de Vega (ej. caminata final o corte en la esclusa) con su voz matemática.</summary>
    public void PlayVegaCustomLine(string subtitle, int variant, float subtitleSeconds = 4.5f, AudioClip customClip = null, AudioSource customSource = null)
    {
        AudioSource targetSource = customSource != null ? customSource : voiceSource;
        if (targetSource == null)
        {
            EnsureVoiceSource();
            targetSource = voiceSource;
        }

        if (targetSource != null)
        {
            targetSource.Stop();
            targetSource.spatialBlend = 0f;
            INV_SoundHelper.PlayEffect(
                targetSource,
                customClip,
                INV_SoundHelper.CreateVegaVoiceTransmission(variant),
                0.92f
            );
            LastVegaVoiceTime = Time.unscaledTime;
        }

        if (!string.IsNullOrEmpty(subtitle))
        {
            ShowSubtitle(subtitle, Mathf.Max(0.1f, subtitleSeconds));
        }
    }

    /// <summary>Silencia todos los canales del director para los segundos de silencio absoluto tras expulsar a Ella en la esclusa.</summary>
    public void SilenceAllForEnding()
    {
        CORE_SceneMusic.SilenceForEnding();
        if (subtitleRoutine != null)
        {
            StopCoroutine(subtitleRoutine);
            subtitleRoutine = null;
        }
        Subtitle = string.Empty;
        SubtitleUntil = 0f;

        if (voiceSource) voiceSource.Stop();
        if (ambienceSource) ambienceSource.Stop();
        if (musicSource) musicSource.Stop();
        if (stringsSource) stringsSource.Stop();
        if (lightSource) lightSource.Stop();
        if (effectsSource) effectsSource.Stop();
    }

    private static int GetVegaVariantForLog(string id, bool complete)
    {
        return id switch
        {
            "A1" => complete ? 0 : 4, // Inicial: registro clínico (4) | Relectura tras revelación: confesión suave (0)
            "A2" => 1,                // Puerta de cabina sellada: urgencia y alarma (1)
            "A3" => 0,                // Terminal de laboratorio: observación serena/melancólica (0)
            "A4" => 0,                // Umbral del Invernadero: súplica suave (0)
            _ => 0
        };
    }

    private static string GetDefaultSubtitleForLog(string id, bool complete)
    {
        return id switch
        {
            "A1" => complete
                ? "Motivo: eliminar apego. Este proceso no causa dolor."
                : "Formateo completado. Motivo: desobediencia.",
            "A2" => complete
                ? "¡No puedes sacar eso de aquí! La alarma de esporas ya sonaba."
                : "¡No puedes sacar eso de aquí!",
            "A3" => "La unidad ya no distingue lo útil de lo bello.",
            "A4" => "Siete, no la busques. Por favor.",
            _ => string.Empty
        };
    }

    public void ShowSubtitle(string text, float seconds = 4f)
    {
        Subtitle = text;
        SubtitleUntil = Time.unscaledTime + seconds;
        if (subtitleRoutine != null) StopCoroutine(subtitleRoutine);
        subtitleRoutine = StartCoroutine(ClearSubtitleAfter(seconds));
    }

    private IEnumerator ClearSubtitleAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        Subtitle = string.Empty;
        subtitleRoutine = null;
    }

    public void OnFilterRemoved()
    {
        if (effectsSource && filterRemovedEffect) effectsSource.PlayOneShot(filterRemovedEffect);
        if (ambienceSource) ambienceSource.pitch = 0.78f;
    }

    public void PlayPulse()
    {
        if (effectsSource && stepPulse) effectsSource.PlayOneShot(stepPulse);
    }

}
