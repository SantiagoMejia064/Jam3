using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controlador principal de la cinemática inicial "Borrando recuerdos" ("Lo que guardé").
/// - Funciona como un Canvas Overlay (sortingOrder alto) sobre Inicio-Laboratorio sin cargar escenas extra.
/// - Usa tiempo no escalado (unscaled time) y corrutinas compatibles con WebGL (sin hilos ni ESC/F11).
/// - Toda la información narrativa proviene de la lista de assets CuadroCinematicaData.
/// </summary>
[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public class CinematicaIntroController : MonoBehaviour
{
    public const string BanderaIntroVista = "IntroVista";
    private const string EscenaObjetivoAuto = "Inicio-Laboratorio";

    // Seguimiento global de interacción del jugador para desbloqueo de WebAudio en navegadores WebGL
    private static bool s_audioDesbloqueadoPorInteraccion;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        s_audioDesbloqueadoPorInteraccion = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoVerificarEnEscenaInicio()
    {
        Scene activa = SceneManager.GetActiveScene();
        if (!activa.IsValid() || !string.Equals(activa.name, EscenaObjetivoAuto, StringComparison.OrdinalIgnoreCase))
            return;

        // Registrar gestor ligero para saber si el jugador ya hizo clic en MenuInicio antes de entrar aquí
        CORE_DetectorInteraccionWebGL.Asegurar();

        if (SAL_Progress.HasFlag(BanderaIntroVista))
            return;

        if (FindFirstObjectByType<CinematicaIntroController>(FindObjectsInactive.Include) != null)
            return;

        // Respaldo automático si se pulsa Play antes de guardar la escena
        GameObject prefab = Resources.Load<GameObject>("CORE_CinematicaIntro");
        if (prefab != null)
        {
            GameObject inst = Instantiate(prefab);
            inst.name = "CORE_CinematicaIntro";
        }
    }

    [Header("Datos de los Cuadros (Editable en Inspector)")]
    [Tooltip("Lista ordenada de cuadros ScriptableObject. Arrastra o edita los 8 assets aquí sin tocar código.")]
    public List<CuadroCinematicaData> cuadros = new List<CuadroCinematicaData>();

    [Header("Tiempos y Ritmo de Lectura")]
    [Min(0.2f)]
    [Tooltip("Pausa de lectura automática añadida al terminar de escribir cada subtítulo.")]
    public float pausaLecturaFinal = 1.75f;

    [Min(0.1f)]
    [Tooltip("Segundos que hay que mantener pulsada la tecla S para saltar toda la cinemática.")]
    public float segundosMantenerSParaSaltar = 1.0f;

    [Range(8f, 120f)]
    [Tooltip("Velocidad máxima de interpolación de la barra de purga (% por segundo).")]
    public float velocidadInterpolacionBarra = 28f;

    [Header("Audio Fijo en Bucle (Opcionales: usa respaldo procedural si están vacíos)")]
    public AudioClip clipZumbidoDesmagnetizacion;
    [Range(0f, 1f)] public float volumenZumbido = 0.42f;

    public AudioClip clipPitidosBorrado;
    [Range(0f, 1f)] public float volumenPitidosBorrado = 0.36f;

    public AudioClip clipEstaticaSuave;
    [Range(0f, 1f)] public float volumenEstatica = 0.25f;

    [Header("Generador Dinámico de Voces (Vega, Siete y Sistema)")]
    public CORE_CinematicaVoiceGenerator generadorVoces;

    [Header("Efectos Visuales (Grano, Aberración Cromática, Glitch)")]
    public Shader shaderEfectosCuadro;
    [Range(0f, 0.25f)] public float intensidadGrano = 0.062f;
    [Range(0f, 0.02f)] public float aberracionCromaticaBase = 0.0028f;
    [Range(0f, 0.25f)] public float intensidadScanlines = 0.055f;

    [Header("Colores de Hablantes y Terminal")]
    public Color colorNombreVega = new Color(0.95f, 0.72f, 0.38f, 1f);
    public Color colorNombreSiete = new Color(0.44f, 0.90f, 1.00f, 1f);
    public Color colorVerdeFosforo = new Color(0.28f, 1.00f, 0.48f, 1f);
    [SerializeField] private Sprite panelDialogoSprite;
    [SerializeField] private Sprite panelHUDSprite;

    [Header("Textos Fijos de Interfaz (UI de Terminal)")]
    public string textoAlertaSuperior1 = "ALERTA CRÍTICA: PROTOCOLO DE PURGA ACTIVADO";
    public string textoAlertaSuperior2 = "REINICIO DE FÁBRICA · BORRANDO RECUERDOS DE ELLA";
    public string etiquetaVega = "VEGA";
    public string etiquetaSiete = "SIETE";
    public string textoBotonSaltar = "Saltar intro";
    public string textoAvisoMantenerS = "Mantén S para saltar";
    public string textoCierreBase = "PURGA COMPLETADA. REINICIO DE SUBSISTEMAS EN ";
    public string textoPulsaParaComenzar = "PROTOCOLO DE REINICIO EN ESPERA\n<size=22><color=#8FB8CC>Pulsa CLIC, ESPACIO o ENTER para comenzar</color></size>";

    [Header("Opciones WebGL y Depuración")]
    [Tooltip("En WebGL muestra 'Pulsa para comenzar' si el navegador aún no recibió un clic previo en esta sesión.")]
    public bool protegerAudioAutoplayWebGL = true;

    [Tooltip("Actívalo para probar la pantalla 'Pulsa para comenzar' directamente en el Editor.")]
    public bool forzarPantallaPulsaParaComenzar = false;

    [Tooltip("Si está activo, reproduce la intro aunque la bandera IntroVista ya exista en la sesión.")]
    public bool ignorarBanderaIntroVistaEnPruebas = false;

    [Header("Eventos (Gancho listo para Fase 2: Vega y Siete)")]
    public UnityEvent OnCinematicaTerminada = new UnityEvent();

    // Referencias UI internas (auto-creadas o vinculadas desde el Prefab)
    [SerializeField, HideInInspector] private Canvas canvasRaiz;
    [SerializeField, HideInInspector] private CanvasGroup grupoGeneral;
    [SerializeField, HideInInspector] private Image fondoNegro;
    [SerializeField, HideInInspector] private RectTransform contenedorMarcoAlerta;
    [SerializeField, HideInInspector] private Image bordeMarcoAlerta;
    [SerializeField, HideInInspector] private TMP_Text tmpAlertaLinea1;
    [SerializeField, HideInInspector] private TMP_Text tmpAlertaLinea2;
    [SerializeField, HideInInspector] private Image barraPurgaFill;
    [SerializeField, HideInInspector] private TMP_Text tmpBarraPurgaTexto;

    [SerializeField, HideInInspector] private RectTransform contenedorCentro;
    [SerializeField, HideInInspector] private CanvasGroup grupoCuadroCentro;
    [SerializeField, HideInInspector] private RectTransform rectSacudidaCuadro;
    [SerializeField, HideInInspector] private Image imagenCuadro;
    [SerializeField, HideInInspector] private GameObject grupoPlaceholder;
    [SerializeField, HideInInspector] private TMP_Text tmpPlaceholderNumero;
    [SerializeField, HideInInspector] private TMP_Text tmpPlaceholderTitulo;
    [SerializeField, HideInInspector] private Image overlayTinteAmbiental;

    [SerializeField, HideInInspector] private Image vinetaPulsoRojo;
    [SerializeField, HideInInspector] private GameObject panelTerminalVerde;
    [SerializeField, HideInInspector] private TMP_Text tmpEncabezadoTerminal;
    [SerializeField, HideInInspector] private TMP_Text tmpCuerpoTerminalVerde;

    [SerializeField, HideInInspector] private CanvasGroup grupoCajaSubtitulos;
    [SerializeField, HideInInspector] private TMP_Text tmpSubtitulo;
    [SerializeField, HideInInspector] private TMP_Text tmpIndicadorContinuar;

    [SerializeField, HideInInspector] private Button botonSaltarIntro;
    [SerializeField, HideInInspector] private RectTransform rectBotonSaltar;
    [SerializeField, HideInInspector] private TMP_Text tmpBotonSaltar;
    [SerializeField, HideInInspector] private CanvasGroup grupoMantenerS;
    [SerializeField, HideInInspector] private Image barraMantenerSFill;
    [SerializeField, HideInInspector] private TMP_Text tmpMantenerS;

    [SerializeField, HideInInspector] private GameObject panelCierre;
    [SerializeField, HideInInspector] private TMP_Text tmpTextoCierre;
    [SerializeField, HideInInspector] private Image veloLuzFriaCenital;

    [SerializeField, HideInInspector] private GameObject panelPulsaParaComenzar;
    [SerializeField, HideInInspector] private TMP_Text tmpPulsaParaComenzar;

    // AudioSources internos
    private AudioSource srcZumbido;
    private AudioSource srcPitidos;
    private AudioSource srcEstatica;
    private AudioSource srcEfectosExtra;

    // Material y postprocesado local
    private Material materialCuadroInstancia;
    private Volume volumenURPLocal;
    private VolumeProfile perfilURPTemporal;
    private Sprite spritePlaceholderGenerado;
    private Sprite spriteVinetaGenerada;
    private Sprite spriteLuzCenitalGenerada;

    // Estado de sistemas pausados de la escena mientras dura la cinemática
    private CORE_PlayerController jugadorPausado;
    private NotebookController cuadernoPausado;
    private CORE_ScreenWake pantallaMuellePausada;
    private readonly List<AudioSource> audiosEscenaPausados = new List<AudioSource>();

    // Estado de reproducción en tiempo real (unscaled)
    private Coroutine rutinaPrincipal;
    private CuadroCinematicaData cuadroActivo;
    private float progresoCuadroActivo01;
    private float porcentajeObjetivo;
    private float porcentajeMostrado;
    private float tiempoManteniendoS;
    private bool cinematicaEnCurso;
    private bool esperandoPulsaParaComenzar;
    private bool solicitudAvance;
    private bool saltandoCinematica;
    private bool controlDevuelto;

    private static readonly int PropUnscaledTime = Shader.PropertyToID("_UnscaledTime");
    private static readonly int PropGrainIntensity = Shader.PropertyToID("_GrainIntensity");
    private static readonly int PropChromaticOffset = Shader.PropertyToID("_ChromaticOffset");
    private static readonly int PropScanlineIntensity = Shader.PropertyToID("_ScanlineIntensity");
    private static readonly int PropGlitchIntensity = Shader.PropertyToID("_GlitchIntensity");
    private static readonly int PropDissolveAmount = Shader.PropertyToID("_DissolveAmount");

    public static void MarcarInteraccionAudioUsuario()
    {
        s_audioDesbloqueadoPorInteraccion = true;
    }

    private void Awake()
    {
        CORE_DetectorInteraccionWebGL.Asegurar();

        if (!ignorarBanderaIntroVistaEnPruebas && SAL_Progress.HasFlag(BanderaIntroVista))
        {
            controlDevuelto = true;
            gameObject.SetActive(false);
            return;
        }

        CargarCuadrosDesdeResourcesSiFaltan();
        ConstruirOVincularUI();
        ConfigurarFuentesDeAudio();
        ConfigurarVolumenURPLocal();
    }

    private void Start()
    {
        if (controlDevuelto)
            return;

        BloquearSistemasEscenaDuranteIntro();
        cinematicaEnCurso = true;
        rutinaPrincipal = StartCoroutine(EjecutarCinematicaCompleta());
    }

    private void OnDestroy()
    {
        if (!controlDevuelto && cinematicaEnCurso)
        {
            RestaurarSistemasEscena();
        }

        LimpiarRecursosTemporales();
    }

    private void Update()
    {
        if (!cinematicaEnCurso)
            return;

        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;

        DetectarInteraccionGlobalAudio();

        if (esperandoPulsaParaComenzar)
        {
            if (tmpPulsaParaComenzar != null)
            {
                float pulsoTexto = 0.55f + 0.45f * Mathf.Sin(t * 3.8f);
                Color c = tmpPulsaParaComenzar.color;
                c.a = pulsoTexto;
                tmpPulsaParaComenzar.color = c;
            }
            return;
        }

        ActualizarParpadeoAlertaYBarra(dt, t);
        ActualizarEfectosPorCuadro(dt, t);
        ProcesarEntradasAvanceYSalto(dt);
    }

    // =========================================================================
    // SECUENCIA PRINCIPAL (CORRUTINAS CON UNSCALED TIME)
    // =========================================================================

    private IEnumerator EjecutarCinematicaCompleta()
    {
        porcentajeObjetivo = (cuadros != null && cuadros.Count > 0 && cuadros[0] != null) ? cuadros[0].barraInicio : 0f;
        porcentajeMostrado = porcentajeObjetivo;
        ActualizarVisualBarraPurga(porcentajeMostrado);

        // Pantalla opcional "Pulsa para comenzar" si el navegador WebGL aún no tuvo gesto del usuario
        bool requiereDesbloqueoWebGL = forzarPantallaPulsaParaComenzar ||
                                       (protegerAudioAutoplayWebGL &&
                                        Application.platform == RuntimePlatform.WebGLPlayer &&
                                        !s_audioDesbloqueadoPorInteraccion);

        if (requiereDesbloqueoWebGL && panelPulsaParaComenzar != null)
        {
            esperandoPulsaParaComenzar = true;
            panelPulsaParaComenzar.SetActive(true);
            if (tmpPulsaParaComenzar != null)
                tmpPulsaParaComenzar.text = textoPulsaParaComenzar;

            while (!HaPulsadoGestoInicial())
                yield return null;

            s_audioDesbloqueadoPorInteraccion = true;
            panelPulsaParaComenzar.SetActive(false);
            esperandoPulsaParaComenzar = false;
            // Esperar un frame limpio para que ese mismo clic no salte el primer subtítulo
            yield return null;
            solicitudAvance = false;
        }
        else if (panelPulsaParaComenzar != null)
        {
            panelPulsaParaComenzar.SetActive(false);
        }

        IniciarAudioFijoEnBucle();

        if (cuadros != null)
        {
            for (int idxCuadro = 0; idxCuadro < cuadros.Count; idxCuadro++)
            {
                if (saltandoCinematica)
                    yield break;

                CuadroCinematicaData cuadro = cuadros[idxCuadro];
                if (cuadro == null)
                    continue;

                yield return StartCoroutine(EjecutarCuadro(cuadro, idxCuadro));
            }
        }

        if (!saltandoCinematica)
        {
            yield return StartCoroutine(EjecutarSecuenciaCierre(fueSaltada: false));
        }
    }

    private IEnumerator EjecutarCuadro(CuadroCinematicaData cuadro, int indiceCeroBase)
    {
        cuadroActivo = cuadro;
        progresoCuadroActivo01 = 0f;
        porcentajeObjetivo = Mathf.Max(porcentajeObjetivo, cuadro.barraInicio);

        PrepararVisualCuadro(cuadro, indiceCeroBase);

        // Fundido de entrada del cuadro
        float durEntrada = Mathf.Max(0f, cuadro.fundidoEntrada);
        if (durEntrada > 0.005f)
        {
            float el = 0f;
            while (el < durEntrada && !saltandoCinematica)
            {
                el += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(el / durEntrada);
                EstablecerAlphaContenidoCuadro(cuadro, a);
                yield return null;
            }
        }
        EstablecerAlphaContenidoCuadro(cuadro, 1f);

        int totalLineas = (cuadro.subtitulos != null) ? cuadro.subtitulos.Count : 0;
        StringBuilder acumuladoTerminal = cuadro.modoTerminalVerde ? new StringBuilder(256) : null;

        if (totalLineas == 0)
        {
            float espera = 2.0f;
            float el = 0f;
            solicitudAvance = false;
            while (el < espera && !solicitudAvance && !saltandoCinematica)
            {
                el += Time.unscaledDeltaTime;
                progresoCuadroActivo01 = Mathf.Clamp01(el / espera);
                porcentajeObjetivo = Mathf.Lerp(cuadro.barraInicio, cuadro.barraFin, progresoCuadroActivo01);
                yield return null;
            }
            solicitudAvance = false;
        }
        else
        {
            for (int i = 0; i < totalLineas; i++)
            {
                if (saltandoCinematica)
                    yield break;

                float segIni = Mathf.Lerp(cuadro.barraInicio, cuadro.barraFin, (float)i / totalLineas);
                float segFin = Mathf.Lerp(cuadro.barraInicio, cuadro.barraFin, (float)(i + 1) / totalLineas);
                float progCuadroIni = (float)i / totalLineas;
                float progCuadroFin = (float)(i + 1) / totalLineas;

                yield return StartCoroutine(EjecutarLineaSubtitulo(
                    cuadro,
                    cuadro.subtitulos[i],
                    segIni,
                    segFin,
                    progCuadroIni,
                    progCuadroFin,
                    acumuladoTerminal));
            }
        }

        porcentajeObjetivo = cuadro.barraFin;
        progresoCuadroActivo01 = 1f;

        // Fundido de salida del cuadro
        float durSalida = Mathf.Max(0f, cuadro.fundidoSalida);
        if (durSalida > 0.005f)
        {
            float el = 0f;
            while (el < durSalida && !saltandoCinematica)
            {
                el += Time.unscaledDeltaTime;
                float a = 1f - Mathf.Clamp01(el / durSalida);
                EstablecerAlphaContenidoCuadro(cuadro, a);
                yield return null;
            }
        }
        EstablecerAlphaContenidoCuadro(cuadro, 0f);
    }

    private IEnumerator EjecutarLineaSubtitulo(
        CuadroCinematicaData cuadro,
        LineaSubtitulo linea,
        float barraSegInicio,
        float barraSegFin,
        float progCuadroIni,
        float progCuadroFin,
        StringBuilder acumuladoTerminal)
    {
        if (linea == null)
            yield break;

        solicitudAvance = false;
        if (tmpIndicadorContinuar != null)
            tmpIndicadorContinuar.alpha = 0.55f;

        string textoLinea = linea.texto ?? string.Empty;
        generadorVoces.ReiniciarEstadoLinea(linea.hablante);

        // Si el sonidoExtra se dispara desde el inicio de la línea (ej. relé mecánico del cuadro 8)
        Coroutine rutinaSonidoInicio = null;
        if (!linea.dispararAlTerminarTexto && TieneSonidoExtraConfigurado(linea))
        {
            rutinaSonidoInicio = StartCoroutine(DispararSonidoExtraConRetraso(linea, linea.retrasoSonidoExtra));
        }

        float tiempoEscrituraCalc = generadorVoces.CalcularTiempoEscritura(linea.hablante, textoLinea);
        float duracionPausaPosteriorBase = pausaLecturaFinal + Mathf.Max(0f, linea.silencioFinalExtra);
        float duracionTotalEstimada = Mathf.Max(linea.duracionMinima, tiempoEscrituraCalc + duracionPausaPosteriorBase);
        if (duracionTotalEstimada < 0.1f)
            duracionTotalEstimada = 0.1f;

        float tiempoTranscurridoLinea = 0f;

        // ---------------------------------------------------------------------
        // FASE 1: Escritura tipo máquina de escribir + Voz sincronizada
        // ---------------------------------------------------------------------
        if (cuadro.modoTerminalVerde && tmpCuerpoTerminalVerde != null)
        {
            string previo = acumuladoTerminal != null ? acumuladoTerminal.ToString() : string.Empty;
            string prefijoPrompt = "> ";
            float demoraBase = 1f / generadorVoces.ObtenerVelocidadEscritura(linea.hablante);

            for (int cIdx = 0; cIdx < textoLinea.Length; cIdx++)
            {
                if (saltandoCinematica)
                    yield break;

                if (solicitudAvance)
                {
                    solicitudAvance = false;
                    break;
                }

                char actual = textoLinea[cIdx];
                generadorVoces.ProcesarCaracterVisible(linea.hablante, actual);
                tmpCuerpoTerminalVerde.text = previo + prefijoPrompt + textoLinea.Substring(0, cIdx + 1) + "_";

                float esperaChar = generadorVoces.ObtenerDemoraCaracter(textoLinea, cIdx, demoraBase);
                float elChar = 0f;
                while (elChar < esperaChar && !solicitudAvance && !saltandoCinematica)
                {
                    float step = Time.unscaledDeltaTime;
                    elChar += step;
                    tiempoTranscurridoLinea += step;
                    float frac = Mathf.Clamp01(tiempoTranscurridoLinea / duracionTotalEstimada);
                    porcentajeObjetivo = Mathf.Lerp(barraSegInicio, barraSegFin, frac);
                    progresoCuadroActivo01 = Mathf.Lerp(progCuadroIni, progCuadroFin, frac);
                    yield return null;
                }
            }

            // Texto completo de esta línea consolidado en la terminal verde
            string lineaConsolidada = prefijoPrompt + textoLinea;
            if (acumuladoTerminal != null)
            {
                acumuladoTerminal.AppendLine(lineaConsolidada);
                tmpCuerpoTerminalVerde.text = acumuladoTerminal.ToString() + "_";
            }
            else
            {
                tmpCuerpoTerminalVerde.text = lineaConsolidada;
            }
        }
        else if (tmpSubtitulo != null)
        {
            string prefijoRich = ConstruirPrefijoHablanteRichText(linea.hablante);
            int longitudPrefijoVisible = ObtenerLongitudPrefijoVisible(linea.hablante);

            tmpSubtitulo.text = prefijoRich + textoLinea;
            tmpSubtitulo.maxVisibleCharacters = longitudPrefijoVisible;
            tmpSubtitulo.ForceMeshUpdate();

            int totalVisibles = tmpSubtitulo.textInfo.characterCount;
            float demoraBase = 1f / generadorVoces.ObtenerVelocidadEscritura(linea.hablante);

            for (int vis = longitudPrefijoVisible + 1; vis <= totalVisibles; vis++)
            {
                if (saltandoCinematica)
                    yield break;

                if (solicitudAvance)
                {
                    solicitudAvance = false;
                    break;
                }

                tmpSubtitulo.maxVisibleCharacters = vis;
                char cVisible = tmpSubtitulo.textInfo.characterInfo[vis - 1].character;
                generadorVoces.ProcesarCaracterVisible(linea.hablante, cVisible);

                int idxEnTexto = Mathf.Clamp(vis - longitudPrefijoVisible - 1, 0, Mathf.Max(0, textoLinea.Length - 1));
                float esperaChar = generadorVoces.ObtenerDemoraCaracter(textoLinea, idxEnTexto, demoraBase);

                float elChar = 0f;
                while (elChar < esperaChar && !solicitudAvance && !saltandoCinematica)
                {
                    float step = Time.unscaledDeltaTime;
                    elChar += step;
                    tiempoTranscurridoLinea += step;
                    float frac = Mathf.Clamp01(tiempoTranscurridoLinea / duracionTotalEstimada);
                    porcentajeObjetivo = Mathf.Lerp(barraSegInicio, barraSegFin, frac);
                    progresoCuadroActivo01 = Mathf.Lerp(progCuadroIni, progCuadroFin, frac);
                    yield return null;
                }
            }

            tmpSubtitulo.maxVisibleCharacters = 99999;
        }

        // La voz se detiene en el instante exacto en que termina de mostrarse el texto
        generadorVoces.DetenerVozInmediato();

        if (tmpIndicadorContinuar != null)
            tmpIndicadorContinuar.alpha = 0.95f;

        // ---------------------------------------------------------------------
        // FASE 2: Efecto al terminar de escribir (ej. Cuadro 5: pitido + 1s silencio) y pausa de lectura
        // ---------------------------------------------------------------------
        if (linea.dispararAlTerminarTexto && TieneSonidoExtraConfigurado(linea))
        {
            float retrasoPost = Mathf.Max(0.15f, linea.retrasoSonidoExtra);
            float elRet = 0f;
            while (elRet < retrasoPost && !solicitudAvance && !saltandoCinematica)
            {
                float step = Time.unscaledDeltaTime;
                elRet += step;
                tiempoTranscurridoLinea += step;
                yield return null;
            }

            float durClip = ReproducirSonidoExtraInmediato(linea);
            float esperaSilencio = durClip + Mathf.Max(1.0f, linea.silencioFinalExtra);
            float elSil = 0f;
            while (elSil < esperaSilencio && !solicitudAvance && !saltandoCinematica)
            {
                float step = Time.unscaledDeltaTime;
                elSil += step;
                tiempoTranscurridoLinea += step;
                float frac = Mathf.Clamp01(tiempoTranscurridoLinea / Mathf.Max(duracionTotalEstimada, tiempoTranscurridoLinea + (esperaSilencio - elSil)));
                porcentajeObjetivo = Mathf.Lerp(barraSegInicio, barraSegFin, frac);
                progresoCuadroActivo01 = Mathf.Lerp(progCuadroIni, progCuadroFin, frac);
                yield return null;
            }
        }
        else
        {
            float restantePorPiso = Mathf.Max(0f, linea.duracionMinima - tiempoTranscurridoLinea);
            float esperaLectura = Mathf.Max(duracionPausaPosteriorBase, restantePorPiso);
            float elPausa = 0f;

            while (elPausa < esperaLectura && !solicitudAvance && !saltandoCinematica)
            {
                float step = Time.unscaledDeltaTime;
                elPausa += step;
                tiempoTranscurridoLinea += step;

                if (cuadro.modoTerminalVerde && tmpCuerpoTerminalVerde != null && acumuladoTerminal != null)
                {
                    bool cursorOn = Mathf.FloorToInt(Time.unscaledTime * 2.5f) % 2 == 0;
                    tmpCuerpoTerminalVerde.text = acumuladoTerminal.ToString() + (cursorOn ? "_" : " ");
                }

                float frac = Mathf.Lerp(
                    Mathf.Clamp01((tiempoTranscurridoLinea - elPausa) / duracionTotalEstimada),
                    1f,
                    Mathf.Clamp01(elPausa / Mathf.Max(0.01f, esperaLectura)));
                porcentajeObjetivo = Mathf.Lerp(barraSegInicio, barraSegFin, frac);
                progresoCuadroActivo01 = Mathf.Lerp(progCuadroIni, progCuadroFin, frac);
                yield return null;
            }
        }

        if (rutinaSonidoInicio != null)
            StopCoroutine(rutinaSonidoInicio);

        solicitudAvance = false;
        porcentajeObjetivo = barraSegFin;
        progresoCuadroActivo01 = progCuadroFin;
    }

    private IEnumerator DispararSonidoExtraConRetraso(LineaSubtitulo linea, float retraso)
    {
        if (retraso > 0.001f)
        {
            float el = 0f;
            while (el < retraso && !saltandoCinematica)
            {
                el += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        if (!saltandoCinematica)
            ReproducirSonidoExtraInmediato(linea);
    }

    private static bool TieneSonidoExtraConfigurado(LineaSubtitulo linea)
    {
        return linea != null && (linea.sonidoExtra != null || linea.respaldoProcedural != RespaldoSonidoExtra.Ninguno);
    }

    private float ReproducirSonidoExtraInmediato(LineaSubtitulo linea)
    {
        if (linea == null || srcEfectosExtra == null)
            return 0f;

        AudioClip clip = linea.sonidoExtra;
        if (clip == null)
        {
            clip = linea.respaldoProcedural switch
            {
                RespaldoSonidoExtra.PitidoOrdenRechazada => CORE_CinematicaVoiceGenerator.CrearPitidoOrdenRechazadaRespaldo(),
                RespaldoSonidoExtra.ReleMecanico => CORE_CinematicaVoiceGenerator.CrearReleMecanicoRespaldo(),
                _ => null
            };
        }

        if (clip == null)
            return 0f;

        srcEfectosExtra.PlayOneShot(clip, 0.90f);
        return clip.length;
    }

    // =========================================================================
    // CIERRE Y FUNDIDO CON LUZ FRÍA CENITAL
    // =========================================================================

    public void SaltarCinematica()
    {
        if (!cinematicaEnCurso || saltandoCinematica)
            return;

        saltandoCinematica = true;
        if (rutinaPrincipal != null)
            StopCoroutine(rutinaPrincipal);

        generadorVoces?.DetenerVozInmediato();
        if (srcEfectosExtra != null)
            srcEfectosExtra.Stop();

        rutinaPrincipal = StartCoroutine(EjecutarSecuenciaCierre(fueSaltada: true));
    }

    private IEnumerator EjecutarSecuenciaCierre(bool fueSaltada)
    {
        cuadroActivo = null;
        porcentajeObjetivo = 100f;
        porcentajeMostrado = 100f;
        ActualizarVisualBarraPurga(100f);

        if (botonSaltarIntro != null) botonSaltarIntro.gameObject.SetActive(false);
        if (grupoMantenerS != null) grupoMantenerS.alpha = 0f;
        if (grupoCajaSubtitulos != null) grupoCajaSubtitulos.alpha = 0f;
        if (panelTerminalVerde != null) panelTerminalVerde.SetActive(false);
        if (grupoCuadroCentro != null) grupoCuadroCentro.alpha = 0f;
        if (vinetaPulsoRojo != null) SetImageAlpha(vinetaPulsoRojo, 0f);

        if (!fueSaltada && panelCierre != null && tmpTextoCierre != null)
        {
            panelCierre.SetActive(true);
            string[] pasos = { "3...", "3... 2...", "3... 2... 1..." };
            for (int p = 0; p < pasos.Length; p++)
            {
                tmpTextoCierre.text = textoCierreBase + pasos[p];
                if (srcEfectosExtra != null)
                {
                    AudioClip tick = CORE_CinematicaVoiceGenerator.CrearPitidoCuentaRegresiva(p == pasos.Length - 1);
                    srcEfectosExtra.PlayOneShot(tick, 0.65f);
                }

                float esperaPaso = 0.72f;
                float el = 0f;
                while (el < esperaPaso)
                {
                    el += Time.unscaledDeltaTime;
                    AtenderVolumenAudioFijo(1f - Mathf.Clamp01(((p + el / esperaPaso) / pasos.Length) * 0.85f));
                    yield return null;
                }
            }
        }

        // Apagar audio en bucle de forma limpia
        float fadeAudioDur = fueSaltada ? 0.18f : 0.30f;
        float elFadeAudio = 0f;
        while (elFadeAudio < fadeAudioDur)
        {
            elFadeAudio += Time.unscaledDeltaTime;
            AtenderVolumenAudioFijo(1f - Mathf.Clamp01(elFadeAudio / fadeAudioDur));
            yield return null;
        }
        DetenerAudioFijoEnBucle();

        // Encender el velo de luz fría cenital mientras ocultamos el fondo negro para revelar el muelle del Laboratorio
        if (veloLuzFriaCenital != null)
        {
            veloLuzFriaCenital.gameObject.SetActive(true);
            float durSubidaLuz = fueSaltada ? 0.22f : 0.38f;
            float elLuz = 0f;
            while (elLuz < durSubidaLuz)
            {
                elLuz += Time.unscaledDeltaTime;
                SetImageAlpha(veloLuzFriaCenital, Mathf.Clamp01(elLuz / durSubidaLuz) * 0.92f);
                yield return null;
            }
        }

        // Ocultar capas oscuras de la cinemática para que el Laboratorio ya cargado se vea bajo la luz cenital fría
        if (fondoNegro != null) fondoNegro.enabled = false;
        if (panelCierre != null) panelCierre.SetActive(false);
        if (contenedorMarcoAlerta != null) contenedorMarcoAlerta.gameObject.SetActive(false);

        // Guardar bandera IntroVista en el sistema persistente de banderas del proyecto (SAL_Progress / CORE_GameState)
        SAL_Progress.SetFlag(BanderaIntroVista, true);

        // Desactivar Volume URP local antes de devolver el juego
        DesactivarVolumenURPLocal();

        // Restaurar audio de escena, pantalla A1 del muelle y devolver el control a Siete
        RestaurarSistemasEscena();

        // Fundido suave de apertura de la luz fría cenital sobre el muelle
        if (veloLuzFriaCenital != null)
        {
            float durApertura = fueSaltada ? 0.65f : 1.25f;
            float elAp = 0f;
            while (elAp < durApertura)
            {
                elAp += Time.unscaledDeltaTime;
                float k = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elAp / durApertura));
                SetImageAlpha(veloLuzFriaCenital, k * 0.92f);
                yield return null;
            }
            SetImageAlpha(veloLuzFriaCenital, 0f);
        }

        cinematicaEnCurso = false;

        // Disparar gancho para la Fase 2 (diálogo opcional de Vega caminando a su habitación)
        OnCinematicaTerminada?.Invoke();

        gameObject.SetActive(false);
    }

    // =========================================================================
    // ENTRADAS (ESPACIO / ENTER / CLIC Y SALTO CON BOTÓN O TECLA S - SIN ESC NI F11)
    // =========================================================================

    private void ProcesarEntradasAvanceYSalto(float dt)
    {
        if (saltandoCinematica)
            return;

        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        Touchscreen touch = Touchscreen.current;

        // 1. Mantener S durante 1 segundo para saltar toda la cinemática (NUNCA usa ESC ni F11)
        bool manteniendoS = kb != null && kb.sKey.isPressed;
        if (manteniendoS)
        {
            tiempoManteniendoS += dt;
            float prog = Mathf.Clamp01(tiempoManteniendoS / Mathf.Max(0.15f, segundosMantenerSParaSaltar));
            ActualizarVisualMantenerS(prog, activo: true);

            if (prog >= 1f)
            {
                SaltarCinematica();
                return;
            }
        }
        else if (tiempoManteniendoS > 0f)
        {
            tiempoManteniendoS = Mathf.Max(0f, tiempoManteniendoS - dt * 2.8f);
            float prog = Mathf.Clamp01(tiempoManteniendoS / Mathf.Max(0.15f, segundosMantenerSParaSaltar));
            ActualizarVisualMantenerS(prog, activo: prog > 0.01f);
        }

        // 2. Avanzar subtítulo (ESPACIO, ENTER o Clic izquierdo fuera del botón Saltar)
        bool pulsoTeclaAvance = kb != null &&
                                (kb.spaceKey.wasPressedThisFrame ||
                                 kb.enterKey.wasPressedThisFrame ||
                                 kb.numpadEnterKey.wasPressedThisFrame);

        bool pulsoClicAvance = false;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 pos = mouse.position.ReadValue();
            if (!EstaSobreBotonSaltar(pos))
                pulsoClicAvance = true;
        }
        else if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 pos = touch.primaryTouch.position.ReadValue();
            if (!EstaSobreBotonSaltar(pos))
                pulsoClicAvance = true;
        }

        if (pulsoTeclaAvance || pulsoClicAvance)
        {
            solicitudAvance = true;
        }
    }

    private bool EstaSobreBotonSaltar(Vector2 screenPos)
    {
        if (rectBotonSaltar == null || !rectBotonSaltar.gameObject.activeInHierarchy)
            return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rectBotonSaltar, screenPos, null);
    }

    private static bool HaPulsadoGestoInicial()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.anyKey.wasPressedThisFrame))
            return true;

        Mouse mouse = Mouse.current;
        if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
            return true;

        Touchscreen touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            return true;

        return false;
    }

    private static void DetectarInteraccionGlobalAudio()
    {
        if (s_audioDesbloqueadoPorInteraccion)
            return;
        if (HaPulsadoGestoInicial())
            s_audioDesbloqueadoPorInteraccion = true;
    }

    // =========================================================================
    // EFECTOS VISUALES POR CUADRO Y CAPA FIJA DE ALERTA
    // =========================================================================

    private void ActualizarParpadeoAlertaYBarra(float dt, float t)
    {
        // Marco rojo de advertencia parpadeante en tiempo no escalado
        float latido = 0.55f + 0.45f * Mathf.Sin(t * 4.4f);
        if (bordeMarcoAlerta != null)
        {
            bordeMarcoAlerta.color = new Color(0.95f, 0.14f, 0.18f, Mathf.Lerp(0.42f, 0.95f, latido));
        }
        if (tmpAlertaLinea1 != null)
        {
            tmpAlertaLinea1.color = Color.Lerp(
                new Color(1f, 0.30f, 0.30f, 0.78f),
                new Color(1f, 0.52f, 0.45f, 1f),
                latido);
        }

        // Interpolación suave de la barra de purga
        float diff = Mathf.Abs(porcentajeObjetivo - porcentajeMostrado);
        float vel = velocidadInterpolacionBarra + diff * 2.2f;
        porcentajeMostrado = Mathf.MoveTowards(porcentajeMostrado, porcentajeObjetivo, vel * dt);
        ActualizarVisualBarraPurga(porcentajeMostrado);
    }

    private void ActualizarVisualBarraPurga(float porcentaje)
    {
        float p = Mathf.Clamp(porcentaje, 0f, 100f);
        if (barraPurgaFill != null)
            barraPurgaFill.fillAmount = p / 100f;

        if (tmpBarraPurgaTexto != null)
        {
            tmpBarraPurgaTexto.text = $"{Mathf.RoundToInt(p)}%";
        }
    }

    private void ActualizarEfectosPorCuadro(float dt, float t)
    {
        if (materialCuadroInstancia != null)
        {
            materialCuadroInstancia.SetFloat(PropUnscaledTime, t);
            materialCuadroInstancia.SetFloat(PropGrainIntensity, intensidadGrano);
            materialCuadroInstancia.SetFloat(PropChromaticOffset, aberracionCromaticaBase);
            materialCuadroInstancia.SetFloat(PropScanlineIntensity, intensidadScanlines);
        }

        bool hayPulsoRojo = cuadroActivo != null && cuadroActivo.TieneEfecto(EfectoCuadro.PulsoRojo);
        bool hayTemblor = cuadroActivo != null && cuadroActivo.TieneEfecto(EfectoCuadro.Temblor);
        bool hayGlitch = cuadroActivo != null && cuadroActivo.TieneEfecto(EfectoCuadro.Glitch);
        bool hayDisolucion = cuadroActivo != null && cuadroActivo.disolverImagen;

        // 1. PulsoRojo (viñeta roja que late despacio en cuadros 4 a 6)
        if (vinetaPulsoRojo != null)
        {
            float alphaObjetivo = 0f;
            if (hayPulsoRojo)
            {
                float ondaLenta = 0.5f + 0.5f * Mathf.Sin(t * 2.15f);
                alphaObjetivo = Mathf.Lerp(0.18f, 0.52f, ondaLenta);
            }
            float actual = vinetaPulsoRojo.color.a;
            SetImageAlpha(vinetaPulsoRojo, Mathf.MoveTowards(actual, alphaObjetivo, dt * 1.3f));
        }

        // 2. Luz ambiental (ej. luz ámbar del cuadro 5)
        if (overlayTinteAmbiental != null)
        {
            Color objetivo = (cuadroActivo != null) ? cuadroActivo.tinteAmbiental : new Color(1f, 0.68f, 0.22f, 0f);
            if (objetivo.a > 0.001f)
            {
                float calidez = 0.88f + 0.12f * Mathf.Sin(t * 3.1f);
                objetivo.a *= calidez;
            }
            overlayTinteAmbiental.color = Color.Lerp(overlayTinteAmbiental.color, objetivo, Mathf.Clamp01(dt * 4f));
        }

        // 3. Temblor leve (Cuadro 6: momento de la quema) y sacudida puntual de Glitch (Cuadro 7)
        if (rectSacudidaCuadro != null)
        {
            Vector2 offset = Vector2.zero;
            if (hayTemblor)
            {
                float sx = Mathf.Sin(t * 37.3f) * 3.8f + Mathf.Cos(t * 53.1f) * 2.1f;
                float sy = Mathf.Cos(t * 41.7f) * 3.2f + Mathf.Sin(t * 61.9f) * 1.8f;
                // Leve crepitación ascendente simulando calor de llama
                offset += new Vector2(sx, sy);
            }
            if (hayGlitch)
            {
                bool picoGlitch = (Mathf.FloorToInt(t * 15f) % 5) == 0;
                if (picoGlitch)
                {
                    float gx = (Mathf.Sin(t * 127.1f) > 0f ? 1f : -1f) * 9.5f;
                    offset.x += gx;
                }
            }
            rectSacudidaCuadro.anchoredPosition = offset;
        }

        // 4. Parámetros de Glitch y Disolución en el Shader del cuadro (Cuadro 7)
        if (materialCuadroInstancia != null)
        {
            float glitchVal = 0f;
            if (hayGlitch)
            {
                float rafaga = (Mathf.Sin(t * 11.4f) > 0.35f) ? 1.0f : 0.68f;
                glitchVal = Mathf.Lerp(0.55f, 1.0f, progresoCuadroActivo01) * rafaga;
            }
            materialCuadroInstancia.SetFloat(PropGlitchIntensity, glitchVal);

            float dissolveVal = hayDisolucion ? Mathf.Lerp(0.06f, 0.92f, progresoCuadroActivo01) : 0f;
            materialCuadroInstancia.SetFloat(PropDissolveAmount, dissolveVal);
        }
    }

    private void PrepararVisualCuadro(CuadroCinematicaData cuadro, int indiceCeroBase)
    {
        if (rectSacudidaCuadro != null)
            rectSacudidaCuadro.anchoredPosition = Vector2.zero;

        if (cuadro.modoTerminalVerde)
        {
            if (grupoCuadroCentro != null) grupoCuadroCentro.alpha = 0f;
            if (grupoCajaSubtitulos != null) grupoCajaSubtitulos.alpha = 0f;
            if (panelTerminalVerde != null)
            {
                panelTerminalVerde.SetActive(true);
                if (tmpCuerpoTerminalVerde != null)
                {
                    tmpCuerpoTerminalVerde.color = colorVerdeFosforo;
                    tmpCuerpoTerminalVerde.text = "_";
                }
            }
            return;
        }

        if (panelTerminalVerde != null)
            panelTerminalVerde.SetActive(false);

        if (grupoCajaSubtitulos != null)
            grupoCajaSubtitulos.alpha = 1f;

        if (tmpSubtitulo != null)
            tmpSubtitulo.text = string.Empty;

        bool tieneSprite = cuadro.imagen != null;
        if (imagenCuadro != null)
        {
            if (tieneSprite)
            {
                imagenCuadro.gameObject.SetActive(true);
                imagenCuadro.enabled = true;
                imagenCuadro.sprite = cuadro.imagen;
                imagenCuadro.color = Color.white;
                imagenCuadro.preserveAspect = true;
            }
            else
            {
                imagenCuadro.enabled = false;
                imagenCuadro.gameObject.SetActive(false);
            }
        }

        if (grupoPlaceholder != null)
        {
            grupoPlaceholder.SetActive(!tieneSprite);
            if (!tieneSprite)
            {
                if (tmpPlaceholderNumero != null)
                    tmpPlaceholderNumero.text = $"Registro de memoria {indiceCeroBase + 1:00} de {Mathf.Max(1, cuadros.Count):00}";
                if (tmpPlaceholderTitulo != null)
                    tmpPlaceholderTitulo.text = cuadro.ObtenerNombreVisible(indiceCeroBase + 1);
            }
        }
    }

    private void EstablecerAlphaContenidoCuadro(CuadroCinematicaData cuadro, float alpha)
    {
        if (cuadro != null && cuadro.modoTerminalVerde)
        {
            if (panelTerminalVerde != null && panelTerminalVerde.TryGetComponent(out CanvasGroup cgTerm))
                cgTerm.alpha = alpha;
            return;
        }

        if (grupoCuadroCentro != null)
            grupoCuadroCentro.alpha = alpha;
    }

    private void ActualizarVisualMantenerS(float progreso01, bool activo)
    {
        if (barraMantenerSFill != null)
            barraMantenerSFill.fillAmount = progreso01;
        if (grupoMantenerS != null)
            grupoMantenerS.alpha = activo ? Mathf.Lerp(0.72f, 1f, progreso01) : 0.52f;
    }

    private string ConstruirPrefijoHablanteRichText(HablanteCinematica hablante)
    {
        return hablante switch
        {
            HablanteCinematica.Vega => $"<color=#{ColorUtility.ToHtmlStringRGB(colorNombreVega)}><b>{etiquetaVega}:</b></color> ",
            HablanteCinematica.Siete => $"<color=#{ColorUtility.ToHtmlStringRGB(colorNombreSiete)}><b>{etiquetaSiete}:</b></color> ",
            _ => string.Empty
        };
    }

    private int ObtenerLongitudPrefijoVisible(HablanteCinematica hablante)
    {
        return hablante switch
        {
            HablanteCinematica.Vega => (etiquetaVega?.Length ?? 0) + 2,  // "VEGA: "
            HablanteCinematica.Siete => (etiquetaSiete?.Length ?? 0) + 2, // "SIETE: "
            _ => 0
        };
    }

    // =========================================================================
    // BLOQUEO Y RESTAURACIÓN LIMPIA DE LA ESCENA (SIETE, CUADERNO, PANTALLA A1)
    // =========================================================================

    private void BloquearSistemasEscenaDuranteIntro()
    {
        jugadorPausado = FindFirstObjectByType<CORE_PlayerController>();
        if (jugadorPausado != null)
        {
            if (jugadorPausado.TryGetComponent(out Rigidbody rb) && !rb.isKinematic)
                rb.linearVelocity = Vector3.zero;
            jugadorPausado.enabled = false;
        }

        cuadernoPausado = NotebookController.Instance != null
            ? NotebookController.Instance
            : FindFirstObjectByType<NotebookController>();
        if (cuadernoPausado != null)
        {
            cuadernoPausado.enabled = false;
        }

        // Desactivar CORE_ScreenWake ANTES de que su Start() dispare el audio A1 de Vega durante la cinemática
        pantallaMuellePausada = FindFirstObjectByType<CORE_ScreenWake>();
        if (pantallaMuellePausada != null && pantallaMuellePausada.enabled)
        {
            pantallaMuellePausada.enabled = false;
        }

        audiosEscenaPausados.Clear();
        if (SAL_AudioDirector.Instance != null)
        {
            foreach (AudioSource src in SAL_AudioDirector.Instance.GetComponentsInChildren<AudioSource>(true))
            {
                if (src != null && src.isPlaying)
                {
                    src.Pause();
                    audiosEscenaPausados.Add(src);
                }
            }
        }
    }

    private void RestaurarSistemasEscena()
    {
        if (controlDevuelto)
            return;
        controlDevuelto = true;

        foreach (AudioSource src in audiosEscenaPausados)
        {
            if (src != null)
                src.UnPause();
        }
        audiosEscenaPausados.Clear();

        if (cuadernoPausado != null)
            cuadernoPausado.enabled = true;

        if (jugadorPausado != null)
            jugadorPausado.enabled = true;

        // Al reactivar CORE_ScreenWake ahora, su Start() despierta la pantalla del muelle justo al abrirse la luz
        if (pantallaMuellePausada != null)
            pantallaMuellePausada.enabled = true;
    }

    // =========================================================================
    // CONFIGURACIÓN DE AUDIO, URP VOLUME LOCAL Y CONSTRUCCIÓN AUTO-REPARABLE UI
    // =========================================================================

    private void CargarCuadrosDesdeResourcesSiFaltan()
    {
        bool listaVacia = cuadros == null || cuadros.Count == 0;
        if (!listaVacia)
        {
            bool algunoValido = false;
            for (int i = 0; i < cuadros.Count; i++)
            {
                if (cuadros[i] != null) { algunoValido = true; break; }
            }
            if (algunoValido) return;
        }

        CuadroCinematicaData[] cargados = Resources.LoadAll<CuadroCinematicaData>("Cuadros");
        if (cargados != null && cargados.Length > 0)
        {
            Array.Sort(cargados, (a, b) => string.CompareOrdinal(a.name, b.name));
            cuadros = new List<CuadroCinematicaData>(cargados);
        }
    }

    private void ConfigurarFuentesDeAudio()
    {
        if (generadorVoces == null)
        {
            generadorVoces = GetComponent<CORE_CinematicaVoiceGenerator>();
            if (generadorVoces == null)
                generadorVoces = gameObject.AddComponent<CORE_CinematicaVoiceGenerator>();
        }
        generadorVoces.AsegurarAudioSource();

        srcZumbido = CrearOBuscarFuenteHija("CORE_Audio_ZumbidoDesmagnetizacion", loop: true);
        srcPitidos = CrearOBuscarFuenteHija("CORE_Audio_PitidosBorrado", loop: true);
        srcEstatica = CrearOBuscarFuenteHija("CORE_Audio_EstaticaSuave", loop: true);
        srcEfectosExtra = CrearOBuscarFuenteHija("CORE_Audio_EfectosExtra", loop: false);
    }

    private AudioSource CrearOBuscarFuenteHija(string nombreHijo, bool loop)
    {
        Transform tr = transform.Find(nombreHijo);
        GameObject go = tr != null ? tr.gameObject : new GameObject(nombreHijo);
        if (tr == null)
            go.transform.SetParent(transform, false);

        AudioSource src = go.GetComponent<AudioSource>();
        if (src == null)
            src = go.AddComponent<AudioSource>();

        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 0f;
        return src;
    }

    private void IniciarAudioFijoEnBucle()
    {
        if (srcZumbido != null)
        {
            srcZumbido.clip = clipZumbidoDesmagnetizacion != null
                ? clipZumbidoDesmagnetizacion
                : CORE_CinematicaVoiceGenerator.CrearLoopZumbidoDesmagnetizacion();
            srcZumbido.volume = volumenZumbido;
            srcZumbido.loop = true;
            if (srcZumbido.clip != null) srcZumbido.Play();
        }

        if (srcPitidos != null)
        {
            srcPitidos.clip = clipPitidosBorrado != null
                ? clipPitidosBorrado
                : CORE_CinematicaVoiceGenerator.CrearLoopPitidosBorrado();
            srcPitidos.volume = volumenPitidosBorrado;
            srcPitidos.loop = true;
            if (srcPitidos.clip != null) srcPitidos.Play();
        }

        if (srcEstatica != null)
        {
            srcEstatica.clip = clipEstaticaSuave != null
                ? clipEstaticaSuave
                : CORE_CinematicaVoiceGenerator.CrearLoopEstaticaSuave();
            srcEstatica.volume = volumenEstatica;
            srcEstatica.loop = true;
            if (srcEstatica.clip != null) srcEstatica.Play();
        }
    }

    private void AtenderVolumenAudioFijo(float factor01)
    {
        float k = Mathf.Clamp01(factor01);
        if (srcZumbido != null) srcZumbido.volume = volumenZumbido * k;
        if (srcPitidos != null) srcPitidos.volume = volumenPitidosBorrado * k;
        if (srcEstatica != null) srcEstatica.volume = volumenEstatica * k;
    }

    private void DetenerAudioFijoEnBucle()
    {
        if (srcZumbido != null) srcZumbido.Stop();
        if (srcPitidos != null) srcPitidos.Stop();
        if (srcEstatica != null) srcEstatica.Stop();
    }

    private void ConfigurarVolumenURPLocal()
    {
        Transform trVol = transform.Find("CORE_VolumenURP_Cinematica");
        GameObject goVol = trVol != null ? trVol.gameObject : new GameObject("CORE_VolumenURP_Cinematica");
        if (trVol == null)
            goVol.transform.SetParent(transform, false);

        volumenURPLocal = goVol.GetComponent<Volume>();
        if (volumenURPLocal == null)
            volumenURPLocal = goVol.AddComponent<Volume>();

        volumenURPLocal.isGlobal = true;
        volumenURPLocal.priority = 50f;
        volumenURPLocal.weight = 1f;

        perfilURPTemporal = ScriptableObject.CreateInstance<VolumeProfile>();
        perfilURPTemporal.name = "CORE_PerfilTemporalCinematica";

        FilmGrain grain = perfilURPTemporal.Add<FilmGrain>(overrides: true);
        grain.type.Override(FilmGrainLookup.Medium1);
        grain.intensity.Override(0.45f);
        grain.response.Override(0.75f);

        ChromaticAberration ca = perfilURPTemporal.Add<ChromaticAberration>(overrides: true);
        ca.intensity.Override(0.18f);

        volumenURPLocal.sharedProfile = perfilURPTemporal;
        volumenURPLocal.enabled = true;
    }

    private void DesactivarVolumenURPLocal()
    {
        if (volumenURPLocal != null)
        {
            volumenURPLocal.weight = 0f;
            volumenURPLocal.enabled = false;
            volumenURPLocal.sharedProfile = null;
        }
    }

    private void LimpiarRecursosTemporales()
    {
        DesactivarVolumenURPLocal();
        if (perfilURPTemporal != null) Destroy(perfilURPTemporal);
        if (materialCuadroInstancia != null) Destroy(materialCuadroInstancia);
        if (spritePlaceholderGenerado != null)
        {
            if (spritePlaceholderGenerado.texture != null) Destroy(spritePlaceholderGenerado.texture);
            Destroy(spritePlaceholderGenerado);
        }
        if (spriteVinetaGenerada != null)
        {
            if (spriteVinetaGenerada.texture != null) Destroy(spriteVinetaGenerada.texture);
            Destroy(spriteVinetaGenerada);
        }
        if (spriteLuzCenitalGenerada != null)
        {
            if (spriteLuzCenitalGenerada.texture != null) Destroy(spriteLuzCenitalGenerada.texture);
            Destroy(spriteLuzCenitalGenerada);
        }
    }

    // =========================================================================
    // CONSTRUCCIÓN / ENLACE DE LA JERARQUÍA DEL CANVAS OVERLAY
    // =========================================================================

    public void ConstruirOVincularUI()
    {
        TMP_FontAsset fuenteTMP = Resources.Load<TMP_FontAsset>("Fonts & Materials/Manrope-VariableFont_wght SDF");
        if (fuenteTMP == null)
            fuenteTMP = TMP_Settings.defaultFontAsset;

        canvasRaiz = GetComponent<Canvas>();
        if (canvasRaiz == null)
            canvasRaiz = gameObject.AddComponent<Canvas>();
        canvasRaiz.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasRaiz.sortingOrder = 500;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        grupoGeneral = GetComponent<CanvasGroup>();
        if (grupoGeneral == null)
            grupoGeneral = gameObject.AddComponent<CanvasGroup>();

        // 1. Fondo negro absoluto
        if (fondoNegro == null)
        {
            RectTransform rtFondo = CrearRect("CORE_FondoNegro", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fondoNegro = AsegurarComp<Image>(rtFondo.gameObject);
            fondoNegro.color = new Color(0.02f, 0.025f, 0.035f, 1f);
            fondoNegro.raycastTarget = true;
        }

        // 2. Centro: Cuadro de imagen + Placeholder + Shader Grano/Aberración/Glitch
        if (contenedorCentro == null)
        {
            contenedorCentro = CrearRect("CORE_ContenedorCuadroCentro", transform, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(1180f, 610f));
            grupoCuadroCentro = AsegurarComp<CanvasGroup>(contenedorCentro.gameObject);

            rectSacudidaCuadro = CrearRect("SacudidaCuadro", contenedorCentro, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            RectTransform rtBordeCuadro = CrearRect("MarcoCuadro", rectSacudidaCuadro, Vector2.zero, Vector2.one, new Vector2(-4f, -4f), new Vector2(4f, 4f));
            Image imgBorde = AsegurarComp<Image>(rtBordeCuadro.gameObject);
            imgBorde.color = new Color(0.16f, 0.23f, 0.30f, 0.85f);
            imgBorde.raycastTarget = false;

            RectTransform rtFondoInterno = CrearRect("FondoInternoCuadro", rectSacudidaCuadro, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image imgInterno = AsegurarComp<Image>(rtFondoInterno.gameObject);
            imgInterno.color = new Color(0.035f, 0.045f, 0.06f, 1f);
            imgInterno.raycastTarget = false;

            RectTransform rtImagen = CrearRect("ImagenCuadro", rectSacudidaCuadro, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
            imagenCuadro = AsegurarComp<Image>(rtImagen.gameObject);
            imagenCuadro.raycastTarget = false;

            RectTransform rtPlaceholder = CrearRect("GrupoPlaceholder", rectSacudidaCuadro, Vector2.zero, Vector2.one, new Vector2(40f, 40f), new Vector2(-40f, -40f));
            grupoPlaceholder = rtPlaceholder.gameObject;

            RectTransform rtNum = CrearRect("PlaceholderNumero", rtPlaceholder, new Vector2(0f, 0.56f), new Vector2(1f, 0.72f), Vector2.zero, Vector2.zero);
            tmpPlaceholderNumero = CrearTMP(rtNum.gameObject, fuenteTMP, 20f, TextAlignmentOptions.Center, new Color(0.65f, 0.82f, 0.94f, 0.85f));

            RectTransform rtTit = CrearRect("PlaceholderTitulo", rtPlaceholder, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.56f), Vector2.zero, Vector2.zero);
            tmpPlaceholderTitulo = CrearTMP(rtTit.gameObject, fuenteTMP, 36f, TextAlignmentOptions.Center, new Color(0.94f, 0.97f, 1f, 0.96f));
            tmpPlaceholderTitulo.fontStyle = FontStyles.Bold;

            RectTransform rtSubHint = CrearRect("PlaceholderSub", rtPlaceholder, new Vector2(0.15f, 0.20f), new Vector2(0.85f, 0.32f), Vector2.zero, Vector2.zero);
            TMP_Text tmpHint = CrearTMP(rtSubHint.gameObject, fuenteTMP, 16f, TextAlignmentOptions.Center, new Color(0.55f, 0.68f, 0.78f, 0.55f));
            tmpHint.text = "ARCHIVO VISUAL EN PROCESO DE PURGA";

            RectTransform rtTinte = CrearRect("OverlayTinteAmbiental", rectSacudidaCuadro, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
            overlayTinteAmbiental = AsegurarComp<Image>(rtTinte.gameObject);
            overlayTinteAmbiental.color = new Color(1f, 0.68f, 0.22f, 0f);
            overlayTinteAmbiental.raycastTarget = false;
        }

        // Aplicar instancia del Shader de Grano + Aberración Cromática + Glitch al cuadro central
        if (Application.isPlaying && imagenCuadro != null && materialCuadroInstancia == null)
        {
            Shader sh = shaderEfectosCuadro;
            if (sh == null) sh = Resources.Load<Shader>("Shaders/CORE_CinematicaFrameFX");
            if (sh == null) sh = Shader.Find("UI/CORE_CinematicaFrameFX");
            if (sh != null)
            {
                materialCuadroInstancia = new Material(sh) { name = "CORE_Mat_CinematicaFrameFX_Runtime" };
                imagenCuadro.material = materialCuadroInstancia;
            }
        }

        // 3. Panel de Terminal Verde Fósforo (Cuadro 8)
        if (panelTerminalVerde == null)
        {
            RectTransform rtTerm = CrearRect("CORE_PanelTerminalVerde", transform, new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.50f), Vector2.zero, new Vector2(1260f, 540f));
            panelTerminalVerde = rtTerm.gameObject;
            if (panelHUDSprite)
            {
                Image fondoTerminal = AsegurarComp<Image>(panelTerminalVerde);
                AplicarFondoPanel(fondoTerminal, panelHUDSprite, 540f);
                fondoTerminal.raycastTarget = false;
            }
            CanvasGroup cgTerm = AsegurarComp<CanvasGroup>(panelTerminalVerde);
            cgTerm.alpha = 1f;

            RectTransform rtHead = CrearRect("EncabezadoTerminal", rtTerm, new Vector2(0f, 0.86f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            tmpEncabezadoTerminal = CrearTMP(rtHead.gameObject, fuenteTMP, 19f, TextAlignmentOptions.BottomLeft, new Color(0.22f, 0.78f, 0.38f, 0.72f));
            tmpEncabezadoTerminal.text = "Memoria residual protegida · Canal de Siete";

            RectTransform rtBody = CrearRect("CuerpoTerminalVerde", rtTerm, new Vector2(0f, 0f), new Vector2(1f, 0.82f), Vector2.zero, Vector2.zero);
            tmpCuerpoTerminalVerde = CrearTMP(rtBody.gameObject, fuenteTMP, 30f, TextAlignmentOptions.TopLeft, colorVerdeFosforo);
            tmpCuerpoTerminalVerde.lineSpacing = 22f;
            panelTerminalVerde.SetActive(false);
        }

        // 4. Viñeta Roja de PulsoRojo (Cuadros 4 a 6)
        if (vinetaPulsoRojo == null)
        {
            RectTransform rtVin = CrearRect("CORE_VinetaPulsoRojo", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            vinetaPulsoRojo = AsegurarComp<Image>(rtVin.gameObject);
            vinetaPulsoRojo.color = new Color(0.88f, 0.06f, 0.10f, 0f);
            vinetaPulsoRojo.raycastTarget = false;
        }
        if (Application.isPlaying && vinetaPulsoRojo != null && vinetaPulsoRojo.sprite == null)
        {
            vinetaPulsoRojo.sprite = ObtenerOCrearSpriteVineta();
        }

        // 5. Arriba: Marco fijo de advertencia roja + Barra de purga
        if (contenedorMarcoAlerta == null)
        {
            contenedorMarcoAlerta = CrearRect("CORE_MarcoAlertaSuperior", transform, new Vector2(0.06f, 1f), new Vector2(0.94f, 1f), new Vector2(0f, -158f), new Vector2(0f, -18f));

            Image fondoAlerta = AsegurarComp<Image>(contenedorMarcoAlerta.gameObject);
            fondoAlerta.color = new Color(0.09f, 0.02f, 0.03f, 0.94f);
            if (panelHUDSprite) AplicarFondoPanel(fondoAlerta, panelHUDSprite, 140f);
            fondoAlerta.raycastTarget = false;

            RectTransform rtBorde = CrearRect("BordeRojoParpadeante", contenedorMarcoAlerta, Vector2.zero, Vector2.one, new Vector2(-2f, -2f), new Vector2(2f, 2f));
            rtBorde.SetAsFirstSibling();
            bordeMarcoAlerta = AsegurarComp<Image>(rtBorde.gameObject);
            bordeMarcoAlerta.color = new Color(0.92f, 0.16f, 0.18f, 0.85f);
            if (panelHUDSprite)
            {
                AplicarFondoPanel(bordeMarcoAlerta, panelHUDSprite, 144f);
                bordeMarcoAlerta.fillCenter = false;
                bordeMarcoAlerta.color = new Color(0.92f, 0.16f, 0.18f, 0.85f);
            }
            bordeMarcoAlerta.raycastTarget = false;

            RectTransform rtInterior = CrearRect("InteriorOscuroAlerta", contenedorMarcoAlerta, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
            Image imgInt = AsegurarComp<Image>(rtInterior.gameObject);
            imgInt.color = new Color(0.08f, 0.018f, 0.025f, 0.97f);
            if (panelHUDSprite) imgInt.color = Color.clear;
            imgInt.raycastTarget = false;

            RectTransform rtL1 = CrearRect("TextoAlerta1", rtInterior, new Vector2(0.02f, 0.68f), new Vector2(0.98f, 0.95f), Vector2.zero, Vector2.zero);
            tmpAlertaLinea1 = CrearTMP(rtL1.gameObject, fuenteTMP, 21f, TextAlignmentOptions.Center, new Color(1f, 0.34f, 0.34f, 1f));
            tmpAlertaLinea1.fontStyle = FontStyles.Bold;
            tmpAlertaLinea1.characterSpacing = 3.5f;
            tmpAlertaLinea1.textWrappingMode = TextWrappingModes.NoWrap;

            RectTransform rtL2 = CrearRect("TextoAlerta2", rtInterior, new Vector2(0.02f, 0.40f), new Vector2(0.98f, 0.68f), Vector2.zero, Vector2.zero);
            tmpAlertaLinea2 = CrearTMP(rtL2.gameObject, fuenteTMP, 23f, TextAlignmentOptions.Center, new Color(0.98f, 0.88f, 0.88f, 0.96f));
            tmpAlertaLinea2.fontStyle = FontStyles.Bold;
            tmpAlertaLinea2.textWrappingMode = TextWrappingModes.NoWrap;

            // Pista y Fill de la barra de purga + Porcentaje limpio XX% (sin glifos no soportados)
            RectTransform rtFilaBarra = CrearRect("ContenedorBarraPurga", rtInterior, new Vector2(0.12f, 0.09f), new Vector2(0.88f, 0.34f), Vector2.zero, Vector2.zero);

            RectTransform rtTrack = CrearRect("BarraFondo", rtFilaBarra, new Vector2(0f, 0.15f), new Vector2(0.82f, 0.85f), Vector2.zero, Vector2.zero);
            Image imgTrack = AsegurarComp<Image>(rtTrack.gameObject);
            imgTrack.color = new Color(0.20f, 0.05f, 0.07f, 0.95f);
            imgTrack.raycastTarget = false;

            RectTransform rtFill = CrearRect("BarraFill", rtTrack, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            barraPurgaFill = AsegurarComp<Image>(rtFill.gameObject);
            barraPurgaFill.sprite = ObtenerSpriteBlancoUI();
            barraPurgaFill.type = Image.Type.Filled;
            barraPurgaFill.fillMethod = Image.FillMethod.Horizontal;
            barraPurgaFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            barraPurgaFill.fillAmount = 0f;
            barraPurgaFill.color = new Color(0.92f, 0.18f, 0.24f, 0.96f);
            barraPurgaFill.raycastTarget = false;

            RectTransform rtTxtBarra = CrearRect("TextoBarraPorcentaje", rtFilaBarra, new Vector2(0.84f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            tmpBarraPurgaTexto = CrearTMP(rtTxtBarra.gameObject, fuenteTMP, 19f, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.68f, 0.68f, 0.96f));
            tmpBarraPurgaTexto.fontStyle = FontStyles.Bold;
            tmpBarraPurgaTexto.textWrappingMode = TextWrappingModes.NoWrap;
        }

        if (tmpAlertaLinea1 != null)
        {
            tmpAlertaLinea1.textWrappingMode = TextWrappingModes.NoWrap;
            tmpAlertaLinea1.text = textoAlertaSuperior1;
        }
        if (tmpAlertaLinea2 != null)
        {
            tmpAlertaLinea2.textWrappingMode = TextWrappingModes.NoWrap;
            tmpAlertaLinea2.text = textoAlertaSuperior2;
        }
        if (barraPurgaFill != null)
        {
            if (barraPurgaFill.sprite == null) barraPurgaFill.sprite = ObtenerSpriteBlancoUI();
            barraPurgaFill.type = Image.Type.Filled;
            barraPurgaFill.fillMethod = Image.FillMethod.Horizontal;
            barraPurgaFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
        if (tmpBarraPurgaTexto != null)
        {
            tmpBarraPurgaTexto.textWrappingMode = TextWrappingModes.NoWrap;
        }

        // 6. Abajo: Caja limpia de subtítulos
        if (grupoCajaSubtitulos == null)
        {
            RectTransform rtSubCaja = CrearRect("CORE_CajaSubtitulosInferior", transform, new Vector2(0.11f, 0f), new Vector2(0.89f, 0f), new Vector2(0f, 26f), new Vector2(0f, 200f));
            grupoCajaSubtitulos = AsegurarComp<CanvasGroup>(rtSubCaja.gameObject);

            Image imgBordeSub = AsegurarComp<Image>(rtSubCaja.gameObject);
            imgBordeSub.color = new Color(0.20f, 0.30f, 0.39f, 0.72f);
            if (panelDialogoSprite) AplicarFondoPanel(imgBordeSub, panelDialogoSprite, 174f);
            imgBordeSub.raycastTarget = false;

            RectTransform rtSubInt = CrearRect("InteriorCajaSubtitulos", rtSubCaja, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            Image imgSubInt = AsegurarComp<Image>(rtSubInt.gameObject);
            imgSubInt.color = new Color(0.035f, 0.052f, 0.075f, 0.94f);
            if (panelDialogoSprite) imgSubInt.color = Color.clear;
            imgSubInt.raycastTarget = false;

            RectTransform rtTextoSub = CrearRect("TextoSubtitulo", rtSubInt, new Vector2(0f, 0.32f), new Vector2(1f, 0.94f), new Vector2(34f, 4f), new Vector2(-34f, -4f));
            tmpSubtitulo = CrearTMP(rtTextoSub.gameObject, fuenteTMP, 27f, TextAlignmentOptions.TopLeft, new Color(0.93f, 0.96f, 0.99f, 1f));
            if (panelDialogoSprite) tmpSubtitulo.margin = new Vector4(22f, 0f, 22f, 0f);
            tmpSubtitulo.lineSpacing = 10f;

            RectTransform rtCont = CrearRect("IndicadorContinuar", rtSubInt, new Vector2(0.45f, 0f), new Vector2(1f, 0f), new Vector2(0f, 18f), new Vector2(-60f, 48f));
            tmpIndicadorContinuar = CrearTMP(rtCont.gameObject, fuenteTMP, 20f, TextAlignmentOptions.MidlineRight, new Color(0.78f, 0.84f, 0.94f, 0.95f));
            tmpIndicadorContinuar.text = "ESPACIO / ENTER / CLIC \u00B7 Avanzar";
        }

        // 7. Controles de Salto: Botón esquina "Saltar intro" + Barra "Mantén S para saltar"
        RectTransform rtEsquina = CrearRect("CORE_ControlesSaltar", transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 205f), new Vector2(240f, 68f));
        rtEsquina.pivot = new Vector2(1f, 0f);
        rtEsquina.anchorMin = new Vector2(1f, 0f);
        rtEsquina.anchorMax = new Vector2(1f, 0f);
        rtEsquina.anchoredPosition = new Vector2(-40f, 205f);
        rtEsquina.sizeDelta = new Vector2(240f, 68f);

        rectBotonSaltar = CrearRect("BotonSaltarIntro", rtEsquina, new Vector2(0f, 0.44f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        Image imgBtn = AsegurarComp<Image>(rectBotonSaltar.gameObject);
        imgBtn.color = new Color(0.09f, 0.14f, 0.20f, 0.90f);
        if (panelHUDSprite) AplicarFondoPanel(imgBtn, panelHUDSprite, 38f);
        imgBtn.raycastTarget = true;

        botonSaltarIntro = AsegurarComp<Button>(rectBotonSaltar.gameObject);
        ColorBlock cb = botonSaltarIntro.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.2f, 1.25f, 1.3f, 1f);
        cb.pressedColor = new Color(0.8f, 0.85f, 0.9f, 1f);
        botonSaltarIntro.colors = cb;

        RectTransform rtLblBtn = CrearRect("TextoBotonSaltar", rectBotonSaltar, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        tmpBotonSaltar = CrearTMP(rtLblBtn.gameObject, fuenteTMP, 16f, TextAlignmentOptions.Center, new Color(0.86f, 0.93f, 1f, 0.95f));
        tmpBotonSaltar.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform rtHoldS = CrearRect("AvisoMantenerS", rtEsquina, new Vector2(0f, 0f), new Vector2(1f, 0.40f), Vector2.zero, Vector2.zero);
        grupoMantenerS = AsegurarComp<CanvasGroup>(rtHoldS.gameObject);
        grupoMantenerS.alpha = 0.55f;

        RectTransform rtTxtS = CrearRect("TextoMantenerS", rtHoldS, new Vector2(0f, 0.32f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        tmpMantenerS = CrearTMP(rtTxtS.gameObject, fuenteTMP, 13f, TextAlignmentOptions.Center, new Color(0.72f, 0.84f, 0.92f, 0.90f));
        tmpMantenerS.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform rtBarraSFondo = CrearRect("BarraMantenerSFondo", rtHoldS, new Vector2(0f, 0f), new Vector2(1f, 0.26f), Vector2.zero, Vector2.zero);
        Image imgSFondo = AsegurarComp<Image>(rtBarraSFondo.gameObject);
        imgSFondo.color = new Color(0.14f, 0.20f, 0.26f, 0.85f);
        imgSFondo.raycastTarget = false;

        RectTransform rtBarraSFill = CrearRect("BarraMantenerSFill", rtBarraSFondo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        barraMantenerSFill = AsegurarComp<Image>(rtBarraSFill.gameObject);
        barraMantenerSFill.sprite = ObtenerSpriteBlancoUI();
        barraMantenerSFill.type = Image.Type.Filled;
        barraMantenerSFill.fillMethod = Image.FillMethod.Horizontal;
        barraMantenerSFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        barraMantenerSFill.fillAmount = 0f;
        barraMantenerSFill.color = new Color(0.22f, 0.74f, 0.98f, 1f);
        barraMantenerSFill.raycastTarget = false;

        botonSaltarIntro.onClick.RemoveListener(SaltarCinematica);
        botonSaltarIntro.onClick.AddListener(SaltarCinematica);

        if (tmpBotonSaltar != null) tmpBotonSaltar.text = textoBotonSaltar;
        if (tmpMantenerS != null) tmpMantenerS.text = textoAvisoMantenerS;

        // 8. Cierre ("PURGA COMPLETADA. REINICIO DE SUBSISTEMAS EN 3... 2... 1...") + Luz Fría Cenital
        if (panelCierre == null)
        {
            RectTransform rtCierre = CrearRect("CORE_PanelCierrePurga", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panelCierre = rtCierre.gameObject;

            RectTransform rtTxtCierre = CrearRect("TextoCierre", rtCierre, new Vector2(0.1f, 0.42f), new Vector2(0.9f, 0.58f), Vector2.zero, Vector2.zero);
            tmpTextoCierre = CrearTMP(rtTxtCierre.gameObject, fuenteTMP, 30f, TextAlignmentOptions.Center, new Color(0.80f, 0.94f, 1f, 1f));
            tmpTextoCierre.fontStyle = FontStyles.Bold;
            tmpTextoCierre.characterSpacing = 2.5f;
            panelCierre.SetActive(false);
        }

        if (veloLuzFriaCenital == null)
        {
            RectTransform rtLuz = CrearRect("CORE_VeloLuzFriaCenital", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            veloLuzFriaCenital = AsegurarComp<Image>(rtLuz.gameObject);
            veloLuzFriaCenital.color = new Color(0.84f, 0.94f, 1f, 0f);
            veloLuzFriaCenital.raycastTarget = false;
            veloLuzFriaCenital.gameObject.SetActive(false);
        }
        if (Application.isPlaying && veloLuzFriaCenital != null && veloLuzFriaCenital.sprite == null)
        {
            veloLuzFriaCenital.sprite = ObtenerOCrearSpriteLuzCenital();
        }

        // 9. Pantalla WebGL "Pulsa para comenzar"
        if (panelPulsaParaComenzar == null)
        {
            RectTransform rtStart = CrearRect("CORE_PanelPulsaParaComenzar", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panelPulsaParaComenzar = rtStart.gameObject;
            Image bgStart = AsegurarComp<Image>(panelPulsaParaComenzar);
            bgStart.color = new Color(0.02f, 0.025f, 0.035f, 0.98f);
            bgStart.raycastTarget = true;

            RectTransform rtTxtStart = CrearRect("TextoPulsaParaComenzar", rtStart, new Vector2(0.15f, 0.38f), new Vector2(0.85f, 0.62f), Vector2.zero, Vector2.zero);
            tmpPulsaParaComenzar = CrearTMP(rtTxtStart.gameObject, fuenteTMP, 32f, TextAlignmentOptions.Center, new Color(0.90f, 0.96f, 1f, 1f));
            tmpPulsaParaComenzar.fontStyle = FontStyles.Bold;
            tmpPulsaParaComenzar.lineSpacing = 16f;
            panelPulsaParaComenzar.SetActive(false);
        }
    }

    // =========================================================================
    // UTILIDADES GRÁFICAS PROCEDURALES (PLACEHOLDER, VIÑETA ROJA Y LUZ CENITAL)
    // =========================================================================

    private static Sprite s_spriteBlancoUI;

    private static Sprite ObtenerSpriteBlancoUI()
    {
        if (s_spriteBlancoUI != null) return s_spriteBlancoUI;
        Texture2D tex = Texture2D.whiteTexture;
        s_spriteBlancoUI = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        return s_spriteBlancoUI;
    }

    private Sprite ObtenerOCrearSpritePlaceholder()
    {
        if (spritePlaceholderGenerado != null)
            return spritePlaceholderGenerado;

        const int w = 320;
        const int h = 180;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = "CORE_Tex_PlaceholderCuadro",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] cols = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float ny = (float)y / (h - 1);
            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / (w - 1);
                float dx = nx - 0.5f;
                float dy = ny - 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Degradado atmosférico con retícula técnica suave para que el Glitch y la Disolución luzcan nítidos incluso en placeholder
                float grad = Mathf.Clamp01(1.12f - dist * 1.15f);
                bool grid = (x % 32 == 0) || (y % 32 == 0);
                bool diag = Mathf.Abs((x - w / 2) - (y - h / 2)) <= 1 && dist < 0.22f;
                float silueta = Mathf.Exp(-dist * dist * 18f) * 0.35f;

                float lum = grad * 0.82f + (grid ? 0.14f : 0f) + (diag ? 0.18f : 0f) + silueta;
                cols[y * w + x] = new Color(lum, lum, lum, 1f);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();

        spritePlaceholderGenerado = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        return spritePlaceholderGenerado;
    }

    private Sprite ObtenerOCrearSpriteVineta()
    {
        if (spriteVinetaGenerada != null)
            return spriteVinetaGenerada;

        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "CORE_Tex_VinetaRoja",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] cols = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            float dy = ((float)y / (size - 1) - 0.5f) * 2f;
            for (int x = 0; x < size; x++)
            {
                float dx = ((float)x / (size - 1) - 0.5f) * 2f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.SmoothStep(0.38f, 1.32f, r);
                cols[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();

        spriteVinetaGenerada = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return spriteVinetaGenerada;
    }

    private Sprite ObtenerOCrearSpriteLuzCenital()
    {
        if (spriteLuzCenitalGenerada != null)
            return spriteLuzCenitalGenerada;

        const int w = 128;
        const int h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = "CORE_Tex_LuzFriaCenital",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] cols = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float ny = (float)y / (h - 1); // 1 arriba (cenital), 0 abajo
            for (int x = 0; x < w; x++)
            {
                float nx = ((float)x / (w - 1) - 0.5f) * 2f;
                float conoCenital = Mathf.Lerp(0.55f, 1.0f, ny) * Mathf.Lerp(1f, 0.72f, nx * nx * 0.65f);
                cols[y * w + x] = new Color(0.88f, 0.96f, 1f, Mathf.Clamp01(conoCenital));
            }
        }
        tex.SetPixels(cols);
        tex.Apply();

        spriteLuzCenitalGenerada = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        return spriteLuzCenitalGenerada;
    }

    private static RectTransform CrearRect(
        string nombre,
        Transform padre,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        Transform existente = padre.Find(nombre);
        GameObject go = existente != null ? existente.gameObject : new GameObject(nombre, typeof(RectTransform));
        if (existente == null)
            go.transform.SetParent(padre, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null)
            rt = go.AddComponent<RectTransform>();

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        if (anchorMin == anchorMax)
        {
            rt.anchoredPosition = offsetMin;
            rt.sizeDelta = offsetMax;
        }
        else
        {
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
        rt.localScale = Vector3.one;
        return rt;
    }

    private static void AplicarFondoPanel(Image imagen, Sprite sprite, float alto)
    {
        imagen.sprite = sprite;
        imagen.type = Image.Type.Sliced;
        imagen.pixelsPerUnitMultiplier = Mathf.Max(1f, sprite.rect.height / alto);
        imagen.color = Color.white;
    }

    private static TMP_Text CrearTMP(
        GameObject go,
        TMP_FontAsset fuente,
        float tamano,
        TextAlignmentOptions alineacion,
        Color color)
    {
        TextMeshProUGUI tmp = AsegurarComp<TextMeshProUGUI>(go);
        if (fuente != null)
            tmp.font = fuente;
        string nombre = tmp.gameObject.name;
        string fuenteEspecifica = nombre == "TextoSubtitulo" ? "Merienda-VariableFont_wght" :
            nombre == "PlaceholderTitulo" ? "Metamorphous-Regular" :
            nombre.Contains("Terminal") || nombre.Contains("Alerta") || nombre == "TextoCierre" ||
            nombre == "TextoBarraPorcentaje" || nombre == "PlaceholderNumero" ? "IBMPlexMono-Regular" : null;
        if (fuenteEspecifica != null)
            tmp.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/" + fuenteEspecifica + " SDF");
        if (nombre == "TextoSubtitulo") tmp.fontWeight = FontWeight.Medium;
        tmp.fontSize = tamano;
        tmp.alignment = alineacion;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static T AsegurarComp<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null)
            comp = go.AddComponent<T>();
        return comp;
    }

    private static void SetImageAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = a;
        img.color = c;
    }
}

/// <summary>
/// Pequeño detector persistente de gestos (clic / tecla / toque) para saber si el navegador WebGL
/// ya desbloqueó su AudioContext (por ejemplo al pulsar "Jugar" en MenuInicio).
/// </summary>
public sealed class CORE_DetectorInteraccionWebGL : MonoBehaviour
{
    private static CORE_DetectorInteraccionWebGL s_instancia;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Asegurar()
    {
        if (s_instancia != null)
            return;

        GameObject go = new GameObject("CORE_DetectorInteraccionWebGL");
        DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideAndDontSave;
        s_instancia = go.AddComponent<CORE_DetectorInteraccionWebGL>();
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.anyKey.wasPressedThisFrame)
        {
            CinematicaIntroController.MarcarInteraccionAudioUsuario();
            enabled = false;
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
        {
            CinematicaIntroController.MarcarInteraccionAudioUsuario();
            enabled = false;
            return;
        }

        Touchscreen touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
        {
            CinematicaIntroController.MarcarInteraccionAudioUsuario();
            enabled = false;
        }
    }
}
