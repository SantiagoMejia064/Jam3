using System.Collections;
using UnityEngine;

/// <summary>Shared music, effects and subtitled logs. Clips can be assigned later without changing scene events.</summary>
public sealed class SAL_AudioDirector : MonoBehaviour
{
    [SerializeField] private SAL_AudioLogData[] logs;
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource stringsSource;
    [SerializeField] private AudioSource lightSource;
    [SerializeField] private AudioSource effectsSource;
    [SerializeField] private AudioClip industrialLoop;
    [SerializeField] private AudioClip musicBase;
    [SerializeField] private AudioClip musicStrings;
    [SerializeField] private AudioClip musicLight;
    [SerializeField] private AudioClip filterRemovedEffect;
    [SerializeField] private AudioClip stepPulse;

    public static SAL_AudioDirector Instance { get; private set; }
    public string Subtitle { get; private set; }
    public float SubtitleUntil { get; private set; }
    private Coroutine subtitleRoutine;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (!industrialLoop) industrialLoop = SAL_ProceduralSound.CreateIndustrialLoop();
        if (!musicBase) musicBase = SAL_ProceduralSound.CreateMusic(false);
        if (!musicStrings) musicStrings = SAL_ProceduralSound.CreateStrings();
        if (!musicLight) musicLight = SAL_ProceduralSound.CreateMusic(true);
        if (!filterRemovedEffect) filterRemovedEffect = SAL_ProceduralSound.CreateFilterEffect();
        if (!stepPulse) stepPulse = SAL_ProceduralSound.CreatePulse();
        if (ambienceSource && industrialLoop) { ambienceSource.clip = industrialLoop; ambienceSource.loop = true; ambienceSource.Play(); }
        if (SAL_Progress.HasFlag(SAL_Progress.FilterRemoved) && ambienceSource) ambienceSource.pitch = 0.78f;
        ApplyAct(SAL_Progress.Act);
    }

    private void OnEnable() { SAL_Progress.ActChanged += ApplyAct; }
    private void OnDisable() { SAL_Progress.ActChanged -= ApplyAct; }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void PlayLog(string id) => PlayLog(id, false);

    public void PlayLog(string id, bool complete)
    {
        if (logs == null) return;
        foreach (SAL_AudioLogData log in logs)
        {
            if (!log || log.id != id) continue;
            AudioClip clip = complete ? log.fullClip : log.shortClip;
            string text = complete ? log.fullSubtitle : log.shortSubtitle;
            if (voiceSource)
            {
                voiceSource.Stop();
                if (clip) { voiceSource.clip = clip; voiceSource.Play(); }
            }
            ShowSubtitle(text, clip ? Mathf.Max(3f, clip.length) : 5f);
            return;
        }
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

    private void ApplyAct(int act)
    {
        PlayLayer(musicSource, musicBase, act < 8, 0.26f);
        PlayLayer(stringsSource, musicStrings, act >= 3 && act < 8, 0.15f);
        PlayLayer(lightSource, musicLight, act == 7, 0.18f);
    }

    private static void PlayLayer(AudioSource source, AudioClip clip, bool enabled, float volume)
    {
        if (!source) return;
        if (!enabled || !clip) { source.Stop(); return; }
        source.volume = volume;
        if (source.clip != clip) source.clip = clip;
        source.loop = true;
        if (!source.isPlaying) source.Play();
    }
}
