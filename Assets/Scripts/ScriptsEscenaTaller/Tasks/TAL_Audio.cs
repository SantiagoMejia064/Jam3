using UnityEngine;

public class TAL_Audio : MonoBehaviour
{
    [Header("Sonidos de las interacciones")]
    public AudioClip recogerCortador;
    public AudioClip recogerCable;
    public AudioClip leerTablilla;
    public AudioClip conectarCable;
    public AudioClip arrancarPulidora;
    public AudioClip limpiar;

    [Header("Fuentes de audio del Taller")]
    public AudioSource objetosSource;
    public AudioSource notasSource;
    public AudioSource pulidoraSource;
    public AudioSource limpiezaSource;

    [Header("Zonas de limpieza")]
    public CORE_Dirt motor;
    public CORE_Dirt registroCargador;
    public CORE_Dirt puntoRonda;

    void Awake()
    {
        if (limpiezaSource != null) limpiezaSource.loop = true;
    }

    void LateUpdate()
    {
        bool cleaning = (motor != null && motor.IsCleaning) ||
                        (registroCargador != null && registroCargador.IsCleaning) ||
                        (puntoRonda != null && puntoRonda.IsCleaning);

        if (limpiezaSource == null) return;

        if (limpiezaSource.clip != limpiar)
        {
            limpiezaSource.Stop();
            limpiezaSource.clip = limpiar;
        }

        if (cleaning && limpiar != null)
        {
            if (!limpiezaSource.isPlaying) limpiezaSource.Play();
        }
        else if (limpiezaSource.isPlaying)
        {
            limpiezaSource.Stop();
        }
    }

    public void PlayCutterPickup() => Play(objetosSource, recogerCortador);
    public void PlayCablePickup() => Play(objetosSource, recogerCable);
    public void PlayShelfNote() => Play(notasSource, leerTablilla);
    public void PlayCableConnection() => Play(pulidoraSource, conectarCable);
    public void PlayPolisherStart() => Play(pulidoraSource, arrancarPulidora);

    void Play(AudioSource source, AudioClip clip)
    {
        if (source != null && clip != null) source.PlayOneShot(clip);
    }

    void OnDisable()
    {
        if (objetosSource != null) objetosSource.Stop();
        if (notasSource != null) notasSource.Stop();
        if (pulidoraSource != null) pulidoraSource.Stop();
        if (limpiezaSource != null) limpiezaSource.Stop();
    }
}
