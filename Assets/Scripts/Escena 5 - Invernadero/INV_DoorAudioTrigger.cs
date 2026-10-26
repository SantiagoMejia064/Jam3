using UnityEngine;

/// <summary>
/// Acompañante exclusivo del Invernadero para INV_Puerta_Pasillo.
/// Reproduce el AudioSource de apertura de puerta espacial sin modificar CORE_ZoneDoor.cs,
/// conservando el sonido durante el cambio de escena hacia el Pasillo (Javier).
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class INV_DoorAudioTrigger : MonoBehaviour
{
    [Header("Audio de Puerta al Pasillo (AudioSource en este mismo objeto)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clipAbrirPuerta;

    private CORE_PlayerController player;
    private Collider triggerCollider;
    private bool waitingForRelease;
    private bool triggeredOnce;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (!audioSource)
        {
            audioSource = GetComponent<AudioSource>();
            if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        var pc = other.GetComponentInParent<CORE_PlayerController>();
        if (pc != null)
        {
            player = pc;
            waitingForRelease = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<CORE_PlayerController>() != null)
        {
            player = null;
        }
    }

    private void Update()
    {
        if (triggeredOnce) return;
        if (NotebookController.Instance && NotebookController.Instance.IsOpen) return;
        if (player == null) return;

        Collider pCol = player.GetComponent<Collider>();
        if (triggerCollider == null || pCol == null || !triggerCollider.bounds.Intersects(pCol.bounds))
        {
            return;
        }

        if (waitingForRelease)
        {
            if (!player.InteractHeld) waitingForRelease = false;
            return;
        }

        if (player.InteractPressed)
        {
            triggeredOnce = true;
            PlayPersistentDoorSound();
        }
    }

    private void PlayPersistentDoorSound()
    {
        AudioClip clipToPlay = clipAbrirPuerta != null
            ? clipAbrirPuerta
            : (audioSource != null && audioSource.clip != null
                ? audioSource.clip
                : INV_SoundHelper.CreateDoorAirlockSound());

        float vol = audioSource != null ? audioSource.volume : 0.80f;
        float pitch = audioSource != null ? audioSource.pitch : 1.0f;

        if (clipToPlay == null) return;

        // Portador temporal DontDestroyOnLoad para que el sonido de la puerta no se corte al cargar Javier
        GameObject carrier = new GameObject("INV_TempDoorAudio");
        DontDestroyOnLoad(carrier);
        AudioSource tempSrc = carrier.AddComponent<AudioSource>();
        tempSrc.playOnAwake = false;
        tempSrc.spatialBlend = 0f;
        tempSrc.volume = vol;
        tempSrc.pitch = pitch;
        tempSrc.PlayOneShot(clipToPlay);
        Destroy(carrier, Mathf.Max(0.5f, clipToPlay.length / Mathf.Max(0.1f, Mathf.Abs(pitch))) + 0.25f);
    }
}
