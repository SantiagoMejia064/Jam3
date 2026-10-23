using UnityEngine;

/// <summary>Optional Bodega clips. Assign them on BOD_Audio in the scene; empty slots stay silent.</summary>
public sealed class BOD_Audio : MonoBehaviour
{
    [Header("Clips de Bodega - asignar los sonidos solo aqui")]
    [SerializeField, Tooltip("Bucle de limpieza: solo con E pulsada sobre una bandeja sucia.")]
    private AudioClip limpiezaBandeja;
    [SerializeField] private AudioClip abrirCamara;
    [SerializeField] private AudioClip recogerSello;
    [SerializeField] private AudioClip codigoErroneo;
    [SerializeField] private AudioClip codigoCorrecto;

    [Header("Fuentes necesarias - ajustar volumen en los hijos")]
    [SerializeField] private AudioSource limpiezaSource;
    [SerializeField] private AudioSource puertaSource;
    [SerializeField] private AudioSource feedbackSource;
    [SerializeField] private BOD_Tray[] bandejas;

    [Header("Ambiente - inicia al entrar a Bodega y se detiene al salir")]
    [SerializeField] private AudioSource[] ambienteSources;

    private void Start()
    {
        if (ambienteSources == null) return;
        foreach (var source in ambienteSources)
        {
            if (!source || !source.isActiveAndEnabled || !source.clip) continue;
            source.loop = true;
            if (!source.isPlaying) source.Play();
        }
    }

    private void LateUpdate()
    {
        bool cleaning = false;
        if (bandejas != null)
            foreach (var tray in bandejas)
                if (tray && tray.isActiveAndEnabled && tray.IsCleaning) { cleaning = true; break; }

        UpdateLoop(limpiezaSource, limpiezaBandeja, cleaning);
    }

    private static void UpdateLoop(AudioSource source, AudioClip clip, bool active)
    {
        if (!source) return;
        if (source.clip != clip) { source.Stop(); source.clip = clip; }
        if (active && clip)
        {
            if (!source.isPlaying) source.Play();
        }
        else if (source.isPlaying) source.Stop();
    }

    public void PlayChamberOpening() => Play(puertaSource, abrirCamara);
    public void PlayPickup() => Play(feedbackSource, recogerSello);
    public void PlayStampPickup() => PlayPickup();
    public void PlayWrongCode() => Play(feedbackSource, codigoErroneo);
    public void PlayCorrectCode() => Play(feedbackSource, codigoCorrecto);

    private static void Play(AudioSource source, AudioClip clip)
    {
        if (source && clip) source.PlayOneShot(clip);
    }

    private void OnDisable()
    {

        if (limpiezaSource) limpiezaSource.Stop();
        if (puertaSource) puertaSource.Stop();
        if (feedbackSource) feedbackSource.Stop();
        if (ambienteSources != null)
            foreach (var source in ambienteSources)
                if (source) source.Stop();
    }
}
