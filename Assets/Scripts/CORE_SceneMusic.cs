using UnityEngine;

// La pista se asigna en el AudioSource de este objeto y pertenece solo a su escena.
[RequireComponent(typeof(AudioSource))]
public class CORE_SceneMusic : MonoBehaviour
{
    private AudioSource source;
    private bool endingSilence;
    private static bool pausedForIntro;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() => pausedForIntro = false;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        SAL_Progress.ActChanged += UpdateMusic;
        UpdateMusic(SAL_Progress.Act);
    }

    private void OnDisable()
    {
        SAL_Progress.ActChanged -= UpdateMusic;
        source.Stop();
    }

    private void UpdateMusic(int act)
    {
        // Desde el acto 8 se conserva el silencio musical del desenlace.
        if (endingSilence || act >= 8 || !source.clip)
        {
            source.Stop();
            return;
        }
        if (pausedForIntro) return;
        if (!source.isPlaying) source.Play();
    }

    public static void PauseForIntro(bool paused)
    {
        pausedForIntro = paused;
        foreach (CORE_SceneMusic music in FindObjectsByType<CORE_SceneMusic>(FindObjectsSortMode.None))
        {
            if (!music.isActiveAndEnabled) continue;
            if (paused) music.source.Pause();
            else
            {
                music.source.UnPause();
                music.UpdateMusic(SAL_Progress.Act);
            }
        }
    }

    public static void SilenceForEnding()
    {
        foreach (CORE_SceneMusic music in FindObjectsByType<CORE_SceneMusic>(FindObjectsSortMode.None))
        {
            music.endingSilence = true;
            music.source.Stop();
        }
    }
}
