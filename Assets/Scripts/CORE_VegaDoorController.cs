using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controlador narrativo y sensorial para la puerta "Habitación de vega".
/// Maneja las 3 etapas del juego:
/// 1. Inicio/Mitad: 5 diálogos secuenciales de Vega justificando su aislamiento tras el borrado de memoria.
/// 2. Ella en el carrito: Diálogo agónico de Vega tosiendo e instando a llevarla a la esclusa.
/// 3. Post-esclusa: Los 3 toques finales de Siete (... / ...... / Nadie responde...) que culminan
///    en una pantalla final reflexiva, cinematográfica y multimedia con música generada proceduralmente.
/// </summary>
public sealed class CORE_VegaDoorController : MonoBehaviour
{
    [Header("Posicionamiento del Sensor")]
    [SerializeField] private Vector3 colliderCenter = new Vector3(-12.5f, 1.48f, -2.05f);
    [SerializeField] private Vector3 colliderSize = new Vector3(3.5f, 3.2f, 3.5f);
    [SerializeField] private float maxInteractionDistance = 3.8f;

    [Header("Líneas de Vega - Fase Inicial (5 Diálogos Distintos)")]
    [SerializeField, TextArea(2, 4)]
    private string[] lineasVegaIniciales = new string[]
    {
        "Vega: «Siete... déjame descansar. El procedimiento de purga fue agotador. Haz tu ronda y deja todo en orden.»",
        "Vega: «¿Otra vez tú? Te dije que no me molestes. Revisa los filtros y no toques los especímenes del invernadero.»",
        "Vega: «Siete, duérmete o limpia los conductos. La puerta tiene el cierre hermético por protocolo... no voy a abrir.»",
        "Vega: «¿Por qué sigues rondando por aquí?... Confío en que el borrado haya eliminado cualquier... residuo emocional. Vuelve al trabajo.»",
        "Vega: «...Estoy intentando dormir, Siete. Si hay alguna anomalía, el sistema me avisará. Desconéctate un rato.»"
    };

    [Header("Línea de Vega - Ella en el Carrito")]
    [SerializeField, TextArea(2, 4)]
    private string lineaEllaEnCarrito = "Vega: «*cof, cof*... ¡Siete!... ¿qué hiciste?... el aire... quema... ¿liberaste a la planta?... ¡cof!... me estoy muriendo... ¡llévala a la esclusa, ya!... ¡es una orden!...»";
    [SerializeField, TextArea(2, 4)]
    private string lineaEllaEnCarritoRepetida = "Vega: «*cof*... ¡A la esclusa, maldita sea!... ¡sácala de la estación antes de que nos mate a los dos!...»";

    [Header("Líneas Finales de Siete (Post-Esclusa)")]
    [SerializeField, TextArea(2, 4)]
    private string lineaFinalToque1 = "Siete: «... ¿Vega? Ya expulsé la planta al espacio. La nave está a salvo... ¿Vega?...»";
    [SerializeField, TextArea(2, 4)]
    private string lineaFinalToque2 = "Siete: «......»\n[Intercomunicador: Sólo estática fría. El sensor emite un tono plano y continuo: ───]";
    [SerializeField, TextArea(2, 4)]
    private string lineaFinalToque3 = "Siete: «...Nadie responde. Se ha ido. Todos se han ido.»\n[ALERTA DE SISTEMA: CERO SIGNOS VITALES HUMANOS DETECTADOS A BORDO.]";

    [Header("Audio")]
    [SerializeField] private AudioSource doorAudioSource;

    private int dialogoInicialIndice = 0;
    private int finalKnockCount = 0;
    private bool isBusy = false;
    private bool inFinalEndingSequence = false;
    private bool waitingForRelease = false;
    private CORE_PlayerController player;
    private BoxCollider triggerCollider;

    private void Awake()
    {
        EnsureCollider();
        EnsureAudioSource();
    }

    private void Start()
    {
        // Silenciar la cabina antigua si existiera en la escena para evitar conflictos de audio logs
        var cabinaAntigua = GameObject.Find("LAB_Cabina_Vega_Sellada");
        if (cabinaAntigua != null)
        {
            var interactable = cabinaAntigua.GetComponent<CORE_Interactable>();
            if (interactable != null) interactable.enabled = false;
            var col = cabinaAntigua.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    private void EnsureCollider()
    {
        triggerCollider = GetComponent<BoxCollider>();
        if (!triggerCollider) triggerCollider = gameObject.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.center = colliderCenter;
        triggerCollider.size = colliderSize;
    }

    private void EnsureAudioSource()
    {
        if (doorAudioSource == null)
        {
            doorAudioSource = GetComponent<AudioSource>();
            if (doorAudioSource == null) doorAudioSource = gameObject.AddComponent<AudioSource>();
            doorAudioSource.playOnAwake = false;
            doorAudioSource.spatialBlend = 0f;
            doorAudioSource.volume = 0.95f;
        }
    }

    private Vector3 GetDoorWorldPosition()
    {
        return transform.TransformPoint(colliderCenter);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (inFinalEndingSequence) return;
        if (other.TryGetComponent(out CORE_PlayerController foundPlayer))
        {
            player = foundPlayer;
            waitingForRelease = true;
            CORE_InteractionHUD.SetPrompt(this, CurrentPrompt());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<CORE_PlayerController>())
        {
            player = null;
            CORE_InteractionHUD.ClearPrompt(this);
        }
    }

    private void Update()
    {
        if (inFinalEndingSequence) return;
        if (NotebookController.Instance && NotebookController.Instance.IsOpen) return;

        // Búsqueda proactiva / tolerancia por distancia si el player no cruzó el trigger de forma directa
        Vector3 doorWorldPos = GetDoorWorldPosition();
        if (player == null)
        {
            CORE_PlayerController found = PlayerPersistence.Instance
                ? PlayerPersistence.Instance.GetComponent<CORE_PlayerController>()
                : FindFirstObjectByType<CORE_PlayerController>();

            if (found != null && Vector3.Distance(doorWorldPos, found.transform.position) <= maxInteractionDistance)
            {
                player = found;
                CORE_InteractionHUD.SetPrompt(this, CurrentPrompt());
            }
        }
        else if (Vector3.Distance(doorWorldPos, player.transform.position) > maxInteractionDistance + 0.8f)
        {
            CORE_InteractionHUD.ClearPrompt(this);
            player = null;
            return;
        }

        if (player == null) return;

        if (waitingForRelease)
        {
            if (!player.InteractHeld) waitingForRelease = false;
            return;
        }

        if (player.InteractPressed && !isBusy)
        {
            Interact();
        }
    }

    private string CurrentPrompt()
    {
        if (SAL_Progress.HasFlag("INV_EllaExpulsada"))
        {
            return "E · Llamar a Vega";
        }
        if (SAL_Progress.HasFlag(INV_IncubatorCart.FlagEllaEnCarrito))
        {
            return "E · Habitación de Vega";
        }
        return "E · Habitación de Vega";
    }

    public void Interact()
    {
        if (isBusy || inFinalEndingSequence) return;

        // ETAPA 3: Post-esclusa (Ella ya fue expulsada)
        if (SAL_Progress.HasFlag("INV_EllaExpulsada"))
        {
            StartCoroutine(FinalKnocksSequenceRoutine());
            return;
        }

        // ETAPA 2: Ella está en el carrito (Vega tosiendo/muriendo)
        if (SAL_Progress.HasFlag(INV_IncubatorCart.FlagEllaEnCarrito))
        {
            StartCoroutine(VegaCoughingRoutine());
            return;
        }

        // ETAPA 1: Juego inicial (5 diálogos distintos)
        StartCoroutine(VegaRoutineDialogue());
    }

    private IEnumerator VegaRoutineDialogue()
    {
        isBusy = true;
        string textoLinea;
        if (dialogoInicialIndice < lineasVegaIniciales.Length)
        {
            textoLinea = lineasVegaIniciales[dialogoInicialIndice];
            dialogoInicialIndice++;
        }
        else
        {
            textoLinea = "Vega: «...Silencio, Siete. Ya te di tus órdenes. Vuelve a trabajar.»";
        }

        int variant = (dialogoInicialIndice % 5) + 1;
        SAL_AudioDirector director = SAL_AudioDirector.EnsureInstance();
        director.PlayVegaCustomLine(textoLinea, variant: variant, subtitleSeconds: 5f, customSource: doorAudioSource);

        yield return new WaitForSeconds(1.2f);
        isBusy = false;
    }

    private IEnumerator VegaCoughingRoutine()
    {
        isBusy = true;
        string texto = (dialogoInicialIndice == 0) ? lineaEllaEnCarrito : lineaEllaEnCarritoRepetida;
        dialogoInicialIndice++;

        SAL_AudioDirector director = SAL_AudioDirector.EnsureInstance();
        // Variante 4 o 2 con tono más forzado/tos
        director.PlayVegaCustomLine(texto, variant: 4, subtitleSeconds: 6f, customSource: doorAudioSource);

        yield return new WaitForSeconds(1.5f);
        isBusy = false;
    }

    private IEnumerator FinalKnocksSequenceRoutine()
    {
        isBusy = true;

        if (finalKnockCount == 0)
        {
            // Primer toque: Siete habla con sus blips digitales
            finalKnockCount = 1;
            PlaySieteBeeps();
            if (SAL_AudioDirector.Instance)
            {
                SAL_AudioDirector.Instance.ShowSubtitle(lineaFinalToque1, 5.5f);
            }
            yield return new WaitForSeconds(2.0f);
            isBusy = false;
        }
        else if (finalKnockCount == 1)
        {
            // Segundo toque: estática y sensor plano continuo (paro cardíaco)
            finalKnockCount = 2;
            PlayFlatlineAndStaticSound();
            if (SAL_AudioDirector.Instance)
            {
                SAL_AudioDirector.Instance.ShowSubtitle(lineaFinalToque2, 6f);
            }
            yield return new WaitForSeconds(2.5f);
            isBusy = false;
        }
        else
        {
            // Tercer toque: nadie responde -> disparar el gran final reflexivo
            finalKnockCount = 3;
            inFinalEndingSequence = true;
            CORE_InteractionHUD.ClearPrompt(this);

            PlayWarningTone();
            if (SAL_AudioDirector.Instance)
            {
                SAL_AudioDirector.Instance.ShowSubtitle(lineaFinalToque3, 6f);
            }

            yield return new WaitForSeconds(3.5f);
            StartCoroutine(ReflectiveEndingSequenceRoutine());
        }
    }

    private void PlaySieteBeeps()
    {
        EnsureAudioSource();
        StartCoroutine(PlaySieteBlipsSequence());
    }

    private IEnumerator PlaySieteBlipsSequence()
    {
        for (int i = 0; i < 4; i++)
        {
            AudioClip clip = CORE_CinematicaVoiceGenerator.CrearBlipDigitalSiete(540f + i * 20f, 0.055f, i);
            doorAudioSource.PlayOneShot(clip, 0.85f);
            yield return new WaitForSeconds(0.09f);
        }
    }

    private void PlayFlatlineAndStaticSound()
    {
        EnsureAudioSource();
        AudioClip clip = GenerateFlatlineMonitorSound();
        doorAudioSource.PlayOneShot(clip, 0.88f);
    }

    private void PlayWarningTone()
    {
        EnsureAudioSource();
        AudioClip clip = GenerateWarningChime();
        doorAudioSource.PlayOneShot(clip, 0.92f);
    }

    #region Secuencia Final Reflexiva Multimedia

    private IEnumerator ReflectiveEndingSequenceRoutine()
    {
        // 1. Bloquear input y físicas de Siete
        CORE_PlayerController pc = PlayerPersistence.Instance
            ? PlayerPersistence.Instance.GetComponent<CORE_PlayerController>()
            : FindFirstObjectByType<CORE_PlayerController>();
        if (pc)
        {
            if (pc.TryGetComponent(out Rigidbody rb)) rb.linearVelocity = Vector3.zero;
            pc.enabled = false;
        }

        // 2. Construir Canvas cinematográfico de overlay
        GameObject endingCanvasGo = new GameObject("CORE_EndingCanvas");
        Canvas canvas = endingCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        var scaler = endingCanvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        endingCanvasGo.AddComponent<GraphicRaycaster>();

        CanvasGroup cg = endingCanvasGo.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // Fondo negro profundo
        GameObject bgGo = new GameObject("EndingBackground");
        bgGo.transform.SetParent(endingCanvasGo.transform, false);
        Image bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.02f, 0.03f, 1f);
        RectTransform bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Barras de cine (Letterboxing)
        CreateCinematicBar(endingCanvasGo.transform, isTop: true);
        CreateCinematicBar(endingCanvasGo.transform, isTop: false);

        // Texto poético / reflexivo central
        GameObject textGo = new GameObject("ReflectiveText");
        textGo.transform.SetParent(endingCanvasGo.transform, false);
        TextMeshProUGUI tmpText = textGo.AddComponent<TextMeshProUGUI>();
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = new Color(0.92f, 0.94f, 0.98f, 1f);
        tmpText.fontSize = 28f;
        tmpText.lineSpacing = 18f;
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.12f, 0.22f);
        textRect.anchorMax = new Vector2(0.88f, 0.78f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Botón interactivo inferior para volver al menú
        GameObject buttonGo = new GameObject("ReturnButton");
        buttonGo.transform.SetParent(endingCanvasGo.transform, false);
        Button returnBtn = buttonGo.AddComponent<Button>();
        Image btnImg = buttonGo.AddComponent<Image>();
        btnImg.color = new Color(0.15f, 0.18f, 0.24f, 0.65f);
        RectTransform btnRect = buttonGo.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.38f, 0.08f);
        btnRect.anchorMax = new Vector2(0.62f, 0.15f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        GameObject btnLabelGo = new GameObject("Label");
        btnLabelGo.transform.SetParent(buttonGo.transform, false);
        TextMeshProUGUI btnLabel = btnLabelGo.AddComponent<TextMeshProUGUI>();
        btnLabel.text = "[ VOLVER AL INICIO ]";
        btnLabel.alignment = TextAlignmentOptions.Center;
        btnLabel.fontSize = 20f;
        btnLabel.color = new Color(0.85f, 0.90f, 0.98f, 1f);
        RectTransform btnLabelRect = btnLabelGo.GetComponent<RectTransform>();
        btnLabelRect.anchorMin = Vector2.zero;
        btnLabelRect.anchorMax = Vector2.one;
        btnLabelRect.offsetMin = Vector2.zero;
        btnLabelRect.offsetMax = Vector2.zero;

        buttonGo.SetActive(false);

        // 3. Audio de música emotiva procedural
        GameObject musicGo = new GameObject("CORE_EndingProceduralMusic");
        AudioSource musicSource = musicGo.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = 0f;
        musicSource.clip = GenerateMelancholicEndingMusic();
        musicSource.Play();

        // 4. Fundido de entrada a negro + fade in musical suave
        float fadeElapsed = 0f;
        const float fadeDuration = 2.4f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(fadeElapsed / fadeDuration);
            cg.alpha = t;
            musicSource.volume = t * 0.76f;
            yield return null;
        }
        cg.alpha = 1f;

        // 5. Presentación de las reflexiones poéticas
        string[] estrofas = new string[]
        {
            "«Lo que guardé en mis circuitos no era una amenaza biológica.»",
            "«Era el último recuerdo de que alguna vez fuimos más que órdenes, protocolos y metal.»",
            "«Vega intentó borrarme para salvar la nave.\nYo rompí mis directivas para salvar lo que sentíamos.»",
            "«Ahora la estación navega en silencio absoluto.\nLa flor flota entre las estrellas... y Vega duerme para siempre.»",
            "«Solo quedo yo...\nesperando a que mi última batería se extinga en la oscuridad.»"
        };

        foreach (string estrofa in estrofas)
        {
            tmpText.text = estrofa;
            // Fade suave de cada línea
            for (float ft = 0f; ft <= 0.6f; ft += Time.unscaledDeltaTime)
            {
                tmpText.alpha = ft / 0.6f;
                yield return null;
            }
            tmpText.alpha = 1f;

            yield return new WaitForSecondsRealtime(3.2f);

            for (float ft = 0.5f; ft >= 0f; ft -= Time.unscaledDeltaTime)
            {
                tmpText.alpha = ft / 0.5f;
                yield return null;
            }
            tmpText.alpha = 0f;
            yield return new WaitForSecondsRealtime(0.4f);
        }

        // Pantalla de cierre de título
        tmpText.fontSize = 32f;
        tmpText.text = "<size=140%><b>L O   Q U E   G U A R D É</b></size>\n\n<size=75%><color=#88A0B8>Un juego creado para la Game Jam</color></size>\n\n<size=65%><color=#D47070>[ SISTEMA EN REPOSO DEFINITIVO ]</color></size>";
        for (float ft = 0f; ft <= 0.8f; ft += Time.unscaledDeltaTime)
        {
            tmpText.alpha = ft / 0.8f;
            yield return null;
        }
        tmpText.alpha = 1f;

        buttonGo.SetActive(true);

        bool proceed = false;
        returnBtn.onClick.AddListener(() => proceed = true);

        // Esperar clic o tecla de interacción o timeout de 16 segundos
        float waitElapsed = 0f;
        while (!proceed && waitElapsed < 16f)
        {
            waitElapsed += Time.unscaledDeltaTime;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E))
            {
                proceed = true;
            }
            yield return null;
        }

        // Fundido final y retorno a la escena MenuInicio
        float outElapsed = 0f;
        while (outElapsed < 1.4f)
        {
            outElapsed += Time.unscaledDeltaTime;
            float ot = 1f - Mathf.Clamp01(outElapsed / 1.4f);
            musicSource.volume = ot * 0.76f;
            yield return null;
        }

        // Limpieza de persistencias para un reinicio limpio
        if (INV_IncubatorCart.ActiveAttachedInstance) Destroy(INV_IncubatorCart.ActiveAttachedInstance.gameObject);
        if (PlayerPersistence.Instance) Destroy(PlayerPersistence.Instance.gameObject);
        if (CORE_Inventory.Instance) Destroy(CORE_Inventory.Instance.gameObject);
        if (NotebookController.Instance) Destroy(NotebookController.Instance.gameObject);
        if (SAL_AudioDirector.Instance) Destroy(SAL_AudioDirector.Instance.gameObject);

        SAL_Progress.ResetForNewGame();
        Time.timeScale = 1f;

        SceneManager.LoadScene("Assets/inicio/MenuInicio.unity", LoadSceneMode.Single);
    }

    private static void CreateCinematicBar(Transform parent, bool isTop)
    {
        GameObject bar = new GameObject(isTop ? "CinematicBar_Top" : "CinematicBar_Bottom");
        bar.transform.SetParent(parent, false);
        Image img = bar.AddComponent<Image>();
        img.color = Color.black;
        RectTransform rt = bar.GetComponent<RectTransform>();
        if (isTop)
        {
            rt.anchorMin = new Vector2(0f, 0.92f);
            rt.anchorMax = new Vector2(1f, 1f);
        }
        else
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0.08f);
        }
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    #endregion

    #region Procedural Audio Synthesis (Sin Dependencias Externas, WebGL Compatible)

    private static AudioClip GenerateFlatlineMonitorSound()
    {
        const int sampleRate = 22050;
        const float duration = 2.4f;
        int count = Mathf.RoundToInt(duration * sampleRate);
        float[] samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            // Primeros 0.35s: ráfaga de estática y chasquido de radio
            float radioBurst = 0f;
            if (t < 0.35f)
            {
                float noise = (Mathf.Sin(i * 12.9898f) * 43758.5453f) % 1f * 2f - 1f;
                radioBurst = noise * Mathf.Exp(-t * 8f) * 0.28f;
            }

            // Tono continuo de monitor cardíaco plano a 880 Hz
            float tone = 0f;
            if (t > 0.18f)
            {
                float toneT = t - 0.18f;
                float env = Mathf.Clamp01(toneT / 0.15f) * Mathf.Clamp01((duration - t) / 0.25f);
                tone = Mathf.Sin(2f * Mathf.PI * 880f * toneT) * env * 0.32f;
            }

            samples[i] = Mathf.Clamp(radioBurst + tone, -0.9f, 0.9f);
        }

        AudioClip clip = AudioClip.Create("SFX_Flatline_Monitor", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip GenerateWarningChime()
    {
        const int sampleRate = 22050;
        const float duration = 1.6f;
        int count = Mathf.RoundToInt(duration * sampleRate);
        float[] samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            // Tono grave de advertencia con armónico menor
            float f1 = 220f;
            float f2 = 277.18f;
            float env = Mathf.Clamp01(t / 0.02f) * Mathf.Exp(-t * 2.2f);
            float wave = (Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.45f + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.25f) * env;
            samples[i] = Mathf.Clamp(wave, -0.9f, 0.9f);
        }

        AudioClip clip = AudioClip.Create("SFX_Warning_Alert", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Genera una composición musical de ambiente melancólico en bucle (acordes menores en La menor/Fa/Do/Mi menor)
    /// con síntesis sustractiva cálida, ideal para la reflexión final en WebGL.
    /// </summary>
    private static AudioClip GenerateMelancholicEndingMusic()
    {
        const int sampleRate = 22050;
        const float duration = 24.0f;
        int count = Mathf.RoundToInt(duration * sampleRate);
        float[] samples = new float[count];

        // 4 acordes de 6 segundos cada uno: Am -> Fmaj7 -> C -> Em
        float[][] chordFreqs = new float[][]
        {
            new float[] { 110.00f, 164.81f, 220.00f, 261.63f }, // Am
            new float[] { 87.31f,  174.61f, 220.00f, 261.63f }, // Fmaj7
            new float[] { 65.41f,  130.81f, 196.00f, 261.63f }, // C
            new float[] { 82.41f,  164.81f, 196.00f, 246.94f }  // Em
        };

        // Notas altas de campana/piano (tiempo_inicio, duracion, frecuencia)
        var melodyNotes = new (float start, float dur, float freq)[]
        {
            (1.2f, 2.5f, 329.63f),  // Mi4
            (3.8f, 2.0f, 293.66f),  // Re4
            (7.2f, 2.6f, 349.23f),  // Fa4
            (10.0f, 2.0f, 329.63f), // Mi4
            (13.2f, 2.6f, 261.63f), // Do4
            (16.0f, 2.0f, 246.94f), // Si3
            (19.2f, 2.6f, 196.00f), // Sol3
            (21.8f, 2.2f, 220.00f)  // La3
        };

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;

            // Determinar acorde según tiempo
            int chordIdx = Mathf.Clamp(Mathf.FloorToInt(t / 6.0f), 0, 3);
            float[] freqs = chordFreqs[chordIdx];
            float chordLocalT = t - (chordIdx * 6.0f);

            // Envolvente suave de cambio de acorde
            float chordEnv = Mathf.Clamp01(chordLocalT / 0.8f) * Mathf.Clamp01((6.0f - chordLocalT) / 0.8f);

            float pad = 0f;
            for (int k = 0; k < freqs.Length; k++)
            {
                float f = freqs[k];
                // Onda senoidal con armónico sutil para calidez analógica
                float w = Mathf.Sin(2f * Mathf.PI * f * t) * 0.22f +
                          Mathf.Sin(4f * Mathf.PI * f * t) * 0.08f;
                pad += w;
            }
            pad *= chordEnv * 0.40f;

            // Melodía tipo campana / música de estrellas
            float melodySample = 0f;
            foreach (var note in melodyNotes)
            {
                if (t >= note.start && t < note.start + note.dur)
                {
                    float nt = t - note.start;
                    float mEnv = Mathf.Clamp01(nt / 0.015f) * Mathf.Exp(-nt * 1.8f);
                    float mWave = Mathf.Sin(2f * Mathf.PI * note.freq * nt) * 0.26f +
                                  Mathf.Sin(4f * Mathf.PI * note.freq * nt) * 0.10f;
                    melodySample += mWave * mEnv;
                }
            }

            samples[i] = Mathf.Clamp(pad + melodySample, -0.92f, 0.92f);
        }

        AudioClip music = AudioClip.Create("MUS_Reflective_Ending_Theme", count, 1, sampleRate, false);
        music.SetData(samples, 0);
        return music;
    }

    #endregion
}
