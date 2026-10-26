using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Opens Javier's authored airlock hatch and finishes the story when Ella reaches it.</summary>
public sealed class CORE_AirlockEnding : MonoBehaviour
{
    public const string FlagVegaFinalWalkPlea = "FIN_VegaPeticionPasillo";

    [SerializeField] private Transform hatch;
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private TMP_Text endingText;
    [SerializeField, Min(0.1f)] private float slideDistance = 2.4f;
    [SerializeField, Min(0.1f)] private float openDuration = 1.6f;
    [SerializeField, Min(0.1f)] private float fadeDuration = 1.6f;
    [SerializeField, Min(0f)] private float returnToMenuDelay = 5f;
    [SerializeField, TextArea] private string endingMessage = "Ella acabó con toda la vida en la nave, solo te queda esperar a oxidarte o apagarte...";

    [Header("Audio de Vega en el Final (Generado con Matemáticas si está vacío)")]
    [SerializeField] private AudioSource vegaFinalSource;
    [SerializeField] private AudioClip clipVegaCaminataFinal;
    [SerializeField] private AudioClip clipVegaCorteEsclusa;

    private Vector3 closedLocalPosition;
    private bool opening;
    private Coroutine finalWalkRoutine;

    private void Awake()
    {
        if (hatch) closedLocalPosition = hatch.localPosition;
        if (fadeOverlay)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.interactable = false;
            fadeOverlay.blocksRaycasts = false;
        }
        if (endingText) endingText.gameObject.SetActive(false);

        EnsureFinalAudioSource();
    }

    private void Start()
    {
        // GDD pág. 15 (El final · 1. La caminata): Al regresar al Pasillo empujando a Ella en el carrito,
        // sin música (Acto 9) y Vega tose y repite su petición debilitado por el intercomunicador.
        if (SAL_Progress.HasFlag(INV_IncubatorCart.FlagEllaEnCarrito) &&
            !SAL_Progress.HasFlag(SAL_Progress.FinalComplete))
        {
            SAL_Progress.SetAct(9);

            if (!SAL_Progress.HasFlag(FlagVegaFinalWalkPlea))
            {
                SAL_Progress.SetFlag(FlagVegaFinalWalkPlea);
                finalWalkRoutine = StartCoroutine(PlayFinalWalkVegaPleaRoutine());
            }
        }
    }

    private void EnsureFinalAudioSource()
    {
        if (vegaFinalSource == null)
        {
            vegaFinalSource = INV_SoundHelper.FindOrCreateChildSource(transform, "Audio_Vega_Final_Esclusa", 0.92f, loop: false);
        }
        if (vegaFinalSource != null)
        {
            vegaFinalSource.playOnAwake = false;
            vegaFinalSource.loop = false;
            vegaFinalSource.spatialBlend = 0f;
        }
    }

    private IEnumerator PlayFinalWalkVegaPleaRoutine()
    {
        yield return new WaitForSeconds(1.2f);

        if (opening || SAL_Progress.HasFlag(SAL_Progress.FinalComplete)) yield break;

        SAL_AudioDirector director = SAL_AudioDirector.EnsureInstance();
        if (Time.unscaledTime - director.LastVegaVoiceTime < 2.0f) yield break;

        const string walkLine = "Vega: \"*cof*... Siete... llévala por el pasillo hasta la esclusa... por favor...\"";
        director.PlayVegaCustomLine(
            walkLine,
            variant: 2,
            subtitleSeconds: 4.8f,
            customClip: clipVegaCaminataFinal,
            customSource: vegaFinalSource
        );
        CORE_InteractionHUD.ShowMessage(walkLine, 4.8f);
        finalWalkRoutine = null;
    }

    private void Update()
    {
        if (opening || !SAL_Progress.HasFlag(SAL_Progress.FinalComplete)) return;
        if (!SAL_Progress.HasFlag(INV_IncubatorCart.FlagEllaEnCarrito)) return;
        if (!hatch)
        {
            Debug.LogError("CORE_AirlockEnding: falta asignar la escotilla en el Inspector.", this);
            enabled = false;
            return;
        }

        opening = true;
        if (finalWalkRoutine != null)
        {
            StopCoroutine(finalWalkRoutine);
            finalWalkRoutine = null;
        }
        StartCoroutine(OpenSequence());
    }

    private IEnumerator OpenSequence()
    {
        EnsureFinalAudioSource();

        INV_IncubatorCart cart = INV_IncubatorCart.ActiveAttachedInstance;
        if (!cart) cart = FindFirstObjectByType<INV_IncubatorCart>(FindObjectsInactive.Include);

        // GDD pág. 16 (El final · 3. Es tarde): Vega comienza su última frase y su voz se corta en seco (~0.86s)
        SAL_AudioDirector director = SAL_AudioDirector.EnsureInstance();
        director.PlayVegaCustomLine(
            "Vega: \"Siete... gracias, yo—\"",
            variant: 5,
            subtitleSeconds: 0.95f,
            customClip: clipVegaCorteEsclusa,
            customSource: vegaFinalSource
        );
        CORE_InteractionHUD.ShowMessage("La escotilla se abre.\nVega: \"Siete... gracias, yo—\"", 1.1f);

        Vector3 start = hatch.localPosition;
        Vector3 end = closedLocalPosition + Vector3.up * slideDistance;
        float elapsed = 0f;
        bool cutoffTriggered = false;

        if (fadeOverlay)
        {
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = true;
        }

        while (elapsed < Mathf.Max(openDuration, fadeDuration))
        {
            elapsed += Time.deltaTime;
            float doorT = Mathf.Clamp01(elapsed / openDuration);
            doorT = doorT * doorT * (3f - 2f * doorT);
            hatch.localPosition = Vector3.LerpUnclamped(start, end, doorT);
            if (fadeOverlay) fadeOverlay.alpha = Mathf.Clamp01(elapsed / fadeDuration);

            // Justo cuando la transmisión matemática de Vega se corta en seco: Ella sale al espacio, su brillo se apaga y cae el silencio absoluto
            if (!cutoffTriggered && elapsed >= 0.92f)
            {
                cutoffTriggered = true;
                if (cart) cart.SetEllaVisible(false);
                if (SAL_AudioDirector.Instance) SAL_AudioDirector.Instance.SilenceAllForEnding();
            }

            if (endingText && fadeOverlay && fadeOverlay.alpha >= 1f && !endingText.gameObject.activeSelf)
            {
                endingText.text = endingMessage;
                endingText.gameObject.SetActive(true);
            }
            yield return null;
        }

        hatch.localPosition = end;

        if (!cutoffTriggered)
        {
            if (cart) cart.SetEllaVisible(false);
            if (SAL_AudioDirector.Instance) SAL_AudioDirector.Instance.SilenceAllForEnding();
        }
        if (vegaFinalSource) vegaFinalSource.Stop();

        SAL_Progress.SetFlag(SAL_Progress.FinalComplete);
        CORE_PlayerController player = PlayerPersistence.Instance
            ? PlayerPersistence.Instance.GetComponent<CORE_PlayerController>()
            : FindFirstObjectByType<CORE_PlayerController>();
        if (player)
        {
            if (player.TryGetComponent(out Rigidbody body)) body.linearVelocity = Vector3.zero;
            player.enabled = false;
        }

        if (endingText)
        {
            endingText.text = endingMessage;
            endingText.gameObject.SetActive(true);
        }
        else
        {
            CORE_InteractionHUD.ShowMessage(endingMessage, returnToMenuDelay);
        }

        yield return new WaitForSecondsRealtime(returnToMenuDelay);

        if (cart) Destroy(cart.gameObject);
        if (PlayerPersistence.Instance) Destroy(PlayerPersistence.Instance.gameObject);
        if (CORE_Inventory.Instance) Destroy(CORE_Inventory.Instance.gameObject);
        if (NotebookController.Instance) Destroy(NotebookController.Instance.gameObject);
        if (SAL_AudioDirector.Instance) Destroy(SAL_AudioDirector.Instance.gameObject);

        Time.timeScale = 1f;
        SceneManager.LoadScene("Assets/inicio/MenuInicio.unity", LoadSceneMode.Single);
    }
}
