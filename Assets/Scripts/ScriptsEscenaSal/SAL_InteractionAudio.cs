using System;
using System.Collections;
using UnityEngine;

/// <summary>Sonidos locales de SAL. Solo se llaman cuando la accion correspondiente ocurre.</summary>
[DisallowMultipleComponent]
public sealed class SAL_InteractionAudio : MonoBehaviour
{
    [Serializable]
    public sealed class Cue
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = .4f;
        [Range(.5f, 2f)] public float pitch = 1f;
        [Min(0f), Tooltip("Inicio del fragmento, en segundos dentro del archivo.")]
        public float startSeconds;
        [Min(.1f), Tooltip("Duracion maxima del fragmento. El final se desvanece suavemente.")]
        public float maxSeconds = 1f;
    }

    [Header("Fuentes locales - sin musica ni voces")]
    [SerializeField] private AudioSource paperSource;
    [SerializeField] private AudioSource mechanismSource;
    [SerializeField] private AudioSource pickupSource;
    [Header("Papeles: basurero, dibujos y etiqueta N7")]
    [SerializeField] private Cue paper = new Cue();
    [Header("Valvula abierta")]
    [SerializeField] private Cue valve = new Cue();
    [Header("Pieza Bruma recogida")]
    [SerializeField] private Cue filterPickup = new Cue();
    [Header("Panel: pulsacion aceptada, acierto y error")]
    [SerializeField] private Cue panelPress = new Cue();
    [SerializeField] private Cue panelSolved = new Cue();
    [SerializeField] private Cue panelRejected = new Cue();
    [Header("Bloques: examinar y recoger Semilla")]
    [SerializeField] private Cue blockInspect = new Cue();
    [SerializeField] private Cue seedPickup = new Cue();
    [Header("Sello de la version de prueba, si se habilita")]
    [SerializeField] private Cue sealPickup = new Cue();

    private Coroutine paperRoutine, mechanismRoutine, pickupRoutine;

    public void PlayPaper() => Play(paperSource, paper, ref paperRoutine);
    public void PlayValveOpened() => Play(mechanismSource, valve, ref mechanismRoutine);
    public void PlayFilterCollected() => Play(pickupSource, filterPickup, ref pickupRoutine);
    public void PlayPanelPress() => Play(mechanismSource, panelPress, ref mechanismRoutine);
    public void PlayPanelSolved() => Play(mechanismSource, panelSolved, ref mechanismRoutine);
    public void PlayPanelRejected() => Play(mechanismSource, panelRejected, ref mechanismRoutine);
    public void PlayBlockInspect() => Play(mechanismSource, blockInspect, ref mechanismRoutine);
    public void PlaySeedCollected() => Play(pickupSource, seedPickup, ref pickupRoutine);
    public void PlaySealCollected() => Play(pickupSource, sealPickup, ref pickupRoutine);

    private void Play(AudioSource source, Cue cue, ref Coroutine routine)
    {
        if (!isActiveAndEnabled || !source || cue == null || !cue.clip) return;
        if (routine != null) StopCoroutine(routine);
        source.Stop();
        source.clip = cue.clip;
        source.loop = false;
        source.pitch = Mathf.Clamp(cue.pitch, .5f, 2f);
        source.volume = Mathf.Clamp01(cue.volume);
        source.time = Mathf.Clamp(cue.startSeconds, 0f, Mathf.Max(0f, cue.clip.length - .01f));
        float remaining = (cue.clip.length - source.time) / source.pitch;
        float duration = Mathf.Min(Mathf.Max(.1f, cue.maxSeconds), remaining);
        source.Play();
        routine = StartCoroutine(Finish(source, duration, source.volume));
    }

    private static IEnumerator Finish(AudioSource source, float duration, float volume)
    {
        // No se superponen copias al pulsar E varias veces; cada fuente tiene un unico fragmento.
        float fade = Mathf.Min(.06f, duration * .2f);
        float elapsed = 0f;
        while (source && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (fade > 0f && elapsed > duration - fade)
                source.volume = volume * Mathf.Clamp01((duration - elapsed) / fade);
            yield return null;
        }
        if (source) { source.Stop(); source.volume = volume; }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (paperSource) paperSource.Stop();
        if (mechanismSource) mechanismSource.Stop();
        if (pickupSource) pickupSource.Stop();
        paperRoutine = mechanismRoutine = pickupRoutine = null;
    }
}
