using System;
using UnityEngine;

[Serializable]
public class CORE_ParametrosVoz
{
    [Range(80f, 1200f)]
    [Tooltip("Frecuencia fundamental de la voz en Hz.")]
    public float tonoBase = 250f;

    [Range(0f, 0.45f)]
    [Tooltip("Variación relativa de tono entre sílabas (0 = monótono, 0.15 = expresivo).")]
    public float variacionTono = 0.12f;

    [Range(8f, 120f)]
    [Tooltip("Velocidad de aparición del texto en caracteres por segundo.")]
    public float velocidadEscritura = 34f;

    [Range(0f, 1f)]
    [Tooltip("Volumen de salida de esta voz.")]
    public float volumen = 0.82f;

    [Range(1, 5)]
    [Tooltip("Cuántas letras visibles disparan un blip/sílaba de voz.")]
    public int letrasPorBlip = 2;

    [Range(0.025f, 0.22f)]
    [Tooltip("Duración en segundos de cada sílaba/blip individual.")]
    public float duracionBlip = 0.075f;

    public static CORE_ParametrosVoz CrearPorDefectoVega()
    {
        return new CORE_ParametrosVoz
        {
            tonoBase = 248f,
            variacionTono = 0.11f,
            velocidadEscritura = 31f,
            volumen = 0.86f,
            letrasPorBlip = 2,
            duracionBlip = 0.080f
        };
    }

    public static CORE_ParametrosVoz CrearPorDefectoSiete()
    {
        return new CORE_ParametrosVoz
        {
            tonoBase = 565f,
            variacionTono = 0.14f,
            velocidadEscritura = 36f,
            volumen = 0.74f,
            letrasPorBlip = 2,
            duracionBlip = 0.044f
        };
    }
}

/// <summary>
/// Sintetizador dinámico de voces por código compatible con WebGL (AudioClip.Create, sin hilos).
/// - Vega: reutiliza directamente INV_SoundHelper.CreateVegaDynamicSyllable (mismos formantes e intercomunicador).
/// - Siete: síntesis digital/metálica (onda cuadrada + diente de sierra + modulación FM + bitcrush).
/// - Sistema: clic sutil de terminal sin voz.
/// Reutilizable también en diálogos in-game posteriores (Fase 2).
/// </summary>
public class CORE_CinematicaVoiceGenerator : MonoBehaviour
{
    private const int SampleRate = 22050;
    private const int VariacionesBanco = 12;

    [Header("Voz de Vega (Reutiliza INV_SoundHelper)")]
    public CORE_ParametrosVoz vozVega = CORE_ParametrosVoz.CrearPorDefectoVega();

    [Header("Voz de Siete (Robot Digital / Metálico)")]
    public CORE_ParametrosVoz vozSiete = CORE_ParametrosVoz.CrearPorDefectoSiete();

    [Header("Terminal del Sistema (Cuadro 8 - Solo Clic)")]
    [Range(10f, 120f)]
    public float velocidadEscrituraSistema = 38f;

    [Range(0f, 1f)]
    public float volumenClicSistema = 0.48f;

    [Range(1, 4)]
    public int letrasPorClicSistema = 2;

    [Header("Pausas de Puntuación (Segundos extra)")]
    [Range(0f, 0.5f)]
    public float pausaComa = 0.14f;

    [Range(0f, 0.8f)]
    public float pausaPunto = 0.26f;

    [SerializeField, HideInInspector]
    private AudioSource voiceSource;

    private readonly AudioClip[] bancoVega = new AudioClip[VariacionesBanco];
    private readonly AudioClip[] bancoSiete = new AudioClip[VariacionesBanco];
    private AudioClip clicTerminalClip;

    private float hashVega = -1f;
    private float hashSiete = -1f;
    private int contadorLetrasSonoras;
    private int indiceVariacionActual;

    private static AudioClip cachedHumLoop;
    private static AudioClip cachedEraseBeepsLoop;
    private static AudioClip cachedSoftStaticLoop;
    private static AudioClip cachedRejectedOrderFallback;
    private static AudioClip cachedMechanicalRelayFallback;
    private static AudioClip cachedCountdownTick;
    private static AudioClip cachedCountdownFinal;

    private void Awake()
    {
        AsegurarAudioSource();
        AsegurarBancosActualizados();
    }

    public void AsegurarAudioSource()
    {
        if (voiceSource == null)
        {
            Transform child = transform.Find("CORE_Audio_VocesDinamicas");
            if (child != null)
                voiceSource = child.GetComponent<AudioSource>();

            if (voiceSource == null)
            {
                GameObject go = child != null ? child.gameObject : new GameObject("CORE_Audio_VocesDinamicas");
                if (child == null)
                    go.transform.SetParent(transform, false);
                voiceSource = go.AddComponent<AudioSource>();
            }
        }

        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
        voiceSource.spatialBlend = 0f;
    }

    public void ReiniciarEstadoLinea(HablanteCinematica hablante)
    {
        AsegurarAudioSource();
        AsegurarBancosActualizados();
        contadorLetrasSonoras = 0;

        // Pequeño pitido sutil de apertura de intercomunicador solo al iniciar intervención de Vega
        if (hablante == HablanteCinematica.Vega && vozVega.volumen > 0.01f)
        {
            AudioClip cue = INV_SoundHelper.CreateVegaIntercomOpenCue();
            if (cue != null)
                voiceSource.PlayOneShot(cue, vozVega.volumen * 0.55f);
        }
    }

    /// <summary>Detiene cualquier sílaba activa en el instante exacto en que termina de escribirse el texto.</summary>
    public void DetenerVozInmediato()
    {
        if (voiceSource != null)
            voiceSource.Stop();
    }

    public float ObtenerVelocidadEscritura(HablanteCinematica hablante)
    {
        return hablante switch
        {
            HablanteCinematica.Vega => Mathf.Max(5f, vozVega.velocidadEscritura),
            HablanteCinematica.Siete => Mathf.Max(5f, vozSiete.velocidadEscritura),
            _ => Mathf.Max(5f, velocidadEscrituraSistema)
        };
    }

    /// <summary>Calcula cuánto tarda en escribirse una cadena respetando pausas de comas y puntos.</summary>
    public float CalcularTiempoEscritura(HablanteCinematica hablante, string textoPlano)
    {
        if (string.IsNullOrEmpty(textoPlano))
            return 0f;

        float baseDelay = 1f / ObtenerVelocidadEscritura(hablante);
        float total = 0f;
        for (int i = 0; i < textoPlano.Length; i++)
        {
            total += ObtenerDemoraCaracter(textoPlano, i, baseDelay);
        }
        return total;
    }

    public float ObtenerDemoraCaracter(string texto, int indice, float demoraBaseCaracter)
    {
        if (string.IsNullOrEmpty(texto) || indice < 0 || indice >= texto.Length)
            return demoraBaseCaracter;

        char c = texto[indice];
        if (c == ',' || c == ';' || c == ':' || c == '—')
            return demoraBaseCaracter + pausaComa;

        if (c == '!' || c == '?' || c == '…')
            return demoraBaseCaracter + pausaPunto;

        if (c == '.')
        {
            // Si forma parte de puntos suspensivos "...", repartimos una pausa suave sin triplicar el freno
            bool siguienteEsPunto = (indice + 1 < texto.Length) && texto[indice + 1] == '.';
            bool anteriorEsPunto = (indice > 0) && texto[indice - 1] == '.';
            if (siguienteEsPunto)
                return demoraBaseCaracter + (pausaPunto * 0.28f);
            if (anteriorEsPunto)
                return demoraBaseCaracter + (pausaPunto * 0.65f);
            return demoraBaseCaracter + pausaPunto;
        }

        return demoraBaseCaracter;
    }

    public static bool EsCaracterSilencioso(char c)
    {
        if (char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsSymbol(c))
            return true;
        return c == '«' || c == '»' || c == '—' || c == '¡' || c == '¿' || c == '…';
    }

    /// <summary>
    /// Llamado cada vez que aparece un nuevo carácter visible en pantalla.
    /// Guarda silencio en espacios y signos de puntuación.
    /// </summary>
    public void ProcesarCaracterVisible(HablanteCinematica hablante, char c)
    {
        if (EsCaracterSilencioso(c))
            return;

        AsegurarAudioSource();
        AsegurarBancosActualizados();

        contadorLetrasSonoras++;

        if (hablante == HablanteCinematica.Sistema)
        {
            int pasoClic = Mathf.Max(1, letrasPorClicSistema);
            if (((contadorLetrasSonoras - 1) % pasoClic) == 0 && clicTerminalClip != null && volumenClicSistema > 0.001f)
            {
                voiceSource.pitch = 0.94f + ((contadorLetrasSonoras * 7) % 5) * 0.03f;
                voiceSource.PlayOneShot(clicTerminalClip, volumenClicSistema);
            }
            return;
        }

        CORE_ParametrosVoz param = hablante == HablanteCinematica.Vega ? vozVega : vozSiete;
        AudioClip[] banco = hablante == HablanteCinematica.Vega ? bancoVega : bancoSiete;

        int paso = Mathf.Max(1, param.letrasPorBlip);
        if (((contadorLetrasSonoras - 1) % paso) != 0 || param.volumen <= 0.001f)
            return;

        // Selección determinista pseudoaleatoria para que suene orgánico sin repetir exactamente el mismo tono seguido
        indiceVariacionActual = (indiceVariacionActual + 1 + (char.ToLowerInvariant(c) % 4)) % VariacionesBanco;
        AudioClip blip = banco[indiceVariacionActual];
        if (blip != null)
        {
            voiceSource.pitch = 1f;
            voiceSource.PlayOneShot(blip, param.volumen);
        }
    }

    private void AsegurarBancosActualizados()
    {
        float nuevoHashVega = CalcularHashParametros(vozVega);
        if (!Mathf.Approximately(hashVega, nuevoHashVega) || bancoVega[0] == null)
        {
            hashVega = nuevoHashVega;
            for (int i = 0; i < VariacionesBanco; i++)
            {
                float norm = (VariacionesBanco > 1) ? ((float)i / (VariacionesBanco - 1)) * 2f - 1f : 0f;
                float mult = 1f + norm * vozVega.variacionTono;
                // Reutiliza directamente el generador matemático de Vega en INV_SoundHelper
                bancoVega[i] = INV_SoundHelper.CreateVegaDynamicSyllable(
                    vozVega.tonoBase,
                    mult,
                    vozVega.duracionBlip,
                    i);
            }
        }

        float nuevoHashSiete = CalcularHashParametros(vozSiete);
        if (!Mathf.Approximately(hashSiete, nuevoHashSiete) || bancoSiete[0] == null)
        {
            hashSiete = nuevoHashSiete;
            for (int i = 0; i < VariacionesBanco; i++)
            {
                float norm = (VariacionesBanco > 1) ? ((float)i / (VariacionesBanco - 1)) * 2f - 1f : 0f;
                float mult = 1f + norm * vozSiete.variacionTono;
                bancoSiete[i] = CrearBlipDigitalSiete(
                    vozSiete.tonoBase * mult,
                    vozSiete.duracionBlip,
                    i);
            }
        }

        if (clicTerminalClip == null)
        {
            clicTerminalClip = CrearClicTerminal();
        }
    }

    private static float CalcularHashParametros(CORE_ParametrosVoz p)
    {
        if (p == null) return 0f;
        return p.tonoBase * 17.3f + p.variacionTono * 311.7f + p.duracionBlip * 997.1f;
    }

    /// <summary>
    /// Generador procedural equivalente para Siete: agudo, metálico y digital
    /// (onda cuadrada + diente de sierra + modulación FM metálica + bitcrush y sample-hold).
    /// </summary>
    public static AudioClip CrearBlipDigitalSiete(float frecuenciaHz, float duracionSeg, int seed)
    {
        float dur = Mathf.Clamp(duracionSeg, 0.025f, 0.22f);
        float f0 = Mathf.Clamp(frecuenciaHz, 180f, 1600f);
        int sampleCount = Mathf.Max(64, Mathf.RoundToInt(dur * SampleRate));
        float[] samples = new float[sampleCount];

        // Salto leve de segunda frecuencia a mitad del blip para timbre robótico de telemetría
        float saltoDigital = ((seed % 3) == 0) ? 1.12f : (((seed % 3) == 1) ? 0.92f : 1.05f);
        const int sampleHoldStep = 3; // Decimación de frecuencia de muestreo (efecto digital nítido)
        const float nivelesBitcrush = 9f; // Cuantización de amplitud

        float heldSample = 0f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float norm = (float)i / sampleCount;

            if ((i % sampleHoldStep) == 0)
            {
                float freqActual = (norm < 0.52f) ? f0 : (f0 * saltoDigital);

                // Onda cuadrada precisa + diente de sierra + armónico metálico inarmónico (2.71x)
                float modFM = Mathf.Sin(2f * Mathf.PI * (freqActual * 2.71f) * t) * 0.65f;
                float cuadrada = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freqActual * t + modFM)) * 0.40f;
                float sierra = (2f * Mathf.Repeat(freqActual * t, 1f) - 1f) * 0.32f;
                float metal = Mathf.Sin(2f * Mathf.PI * (freqActual * 3.85f) * t) * 0.22f;

                float raw = cuadrada + sierra + metal;
                // Bitcrush
                heldSample = Mathf.Round(raw * nivelesBitcrush) / nivelesBitcrush;
            }

            // Envolvente digital corta y precisa, sin vibrato humano
            float ataque = Mathf.Clamp01(t / 0.0035f);
            float caida = Mathf.Exp(-norm * 3.1f) * Mathf.Clamp01((1f - norm) * 8f);
            samples[i] = Mathf.Clamp(heldSample * ataque * caida * 0.58f, -0.85f, 0.85f);
        }

        AudioClip clip = AudioClip.Create($"CORE_SieteBlip_{seed}", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static AudioClip CrearClicTerminal()
    {
        const float dur = 0.024f;
        int n = Mathf.RoundToInt(dur * SampleRate);
        float[] samples = new float[n];
        var rnd = new System.Random(512);

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float env = Mathf.Exp(-t * 210f);
            float tick = Mathf.Sin(2f * Mathf.PI * 1480f * t) * 0.55f;
            float body = Mathf.Sin(2f * Mathf.PI * 620f * t) * 0.28f;
            float crisp = ((float)rnd.NextDouble() * 2f - 1f) * 0.22f;
            samples[i] = Mathf.Clamp((tick + body + crisp) * env * 0.65f, -0.8f, 0.8f);
        }

        AudioClip clip = AudioClip.Create("CORE_TerminalClick", n, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // =========================================================================
    // AUDIO AMBIENTAL FIJO Y EFECTOS PROCEDURALES DE RESPALDO (WEBGL SAFE)
    // =========================================================================

    public static AudioClip CrearLoopZumbidoDesmagnetizacion()
    {
        if (cachedHumLoop != null) return cachedHumLoop;
        const float dur = 6.0f; // Ciclos enteros exactos para loop sin clic
        int n = Mathf.RoundToInt(dur * SampleRate);
        float[] samples = new float[n];

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float pulsoLento = 0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * (t / 3.0f));
            float sub55 = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.46f;
            float grave82 = Mathf.Sin(2f * Mathf.PI * 82.5f * t) * 0.24f;
            float arm110 = Mathf.Sin(2f * Mathf.PI * 110f * t + Mathf.Sin(2f * Mathf.PI * 0.5f * t) * 0.8f) * 0.22f;
            samples[i] = Mathf.Clamp((sub55 + grave82 + arm110) * pulsoLento * 0.38f, -0.75f, 0.75f);
        }

        cachedHumLoop = AudioClip.Create("CORE_Loop_ZumbidoDesmagnetizacion", n, 1, SampleRate, false);
        cachedHumLoop.SetData(samples, 0);
        return cachedHumLoop;
    }

    public static AudioClip CrearLoopPitidosBorrado()
    {
        if (cachedEraseBeepsLoop != null) return cachedEraseBeepsLoop;
        const float dur = 4.8f;
        int n = Mathf.RoundToInt(dur * SampleRate);
        float[] samples = new float[n];

        float[] tiempos = { 0.35f, 0.52f, 1.65f, 2.85f, 3.02f, 3.19f, 4.20f };
        float[] tonos = { 1320f, 990f, 1480f, 1240f, 1240f, 932f, 1568f };

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float s = 0f;
            for (int k = 0; k < tiempos.Length; k++)
            {
                float dt = t - tiempos[k];
                if (dt >= 0f && dt <= 0.085f)
                {
                    float env = Mathf.Sin(Mathf.PI * (dt / 0.085f)) * Mathf.Exp(-dt * 18f);
                    s += Mathf.Sin(2f * Mathf.PI * tonos[k] * dt) * env * 0.22f;
                }
            }
            samples[i] = Mathf.Clamp(s, -0.6f, 0.6f);
        }

        cachedEraseBeepsLoop = AudioClip.Create("CORE_Loop_PitidosBorrado", n, 1, SampleRate, false);
        cachedEraseBeepsLoop.SetData(samples, 0);
        return cachedEraseBeepsLoop;
    }

    public static AudioClip CrearLoopEstaticaSuave()
    {
        if (cachedSoftStaticLoop != null) return cachedSoftStaticLoop;
        const float dur = 4.0f;
        int n = Mathf.RoundToInt(dur * SampleRate);
        float[] samples = new float[n];
        var rnd = new System.Random(909);
        float lp = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float white = (float)rnd.NextDouble() * 2f - 1f;
            lp = lp * 0.84f + white * 0.16f;
            float crackle = (rnd.NextDouble() > 0.9965) ? ((float)rnd.NextDouble() * 2f - 1f) * 0.28f : 0f;
            // Envolvente de cruce suave en los extremos para evitar salto en loop
            float loopSmooth = Mathf.Clamp01(t * 20f) * Mathf.Clamp01((dur - t) * 20f);
            samples[i] = Mathf.Clamp((lp * 0.30f + crackle) * (0.85f + 0.15f * loopSmooth), -0.5f, 0.5f);
        }

        cachedSoftStaticLoop = AudioClip.Create("CORE_Loop_EstaticaSuave", n, 1, SampleRate, false);
        cachedSoftStaticLoop.SetData(samples, 0);
        return cachedSoftStaticLoop;
    }

    public static AudioClip CrearPitidoOrdenRechazadaRespaldo()
    {
        if (cachedRejectedOrderFallback != null) return cachedRejectedOrderFallback;
        const float dur = 0.52f;
        int n = Mathf.RoundToInt(dur * SampleRate);
        float[] samples = new float[n];

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float s = 0f;
            if (t < 0.17f)
            {
                float env = Mathf.Clamp01(t * 80f) * Mathf.Exp(-t * 4.5f);
                float sq = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 520f * t)) * 0.40f;
                float sub = Mathf.Sin(2f * Mathf.PI * 260f * t) * 0.34f;
                s = (sq + sub) * env;
            }
            else if (t >= 0.20f)
            {
                float t2 = t - 0.20f;
                float env = Mathf.Clamp01(t2 * 80f) * Mathf.Clamp01((0.32f - t2) / 0.11f) * Mathf.Exp(-t2 * 3.0f);
                float sq1 = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 311.13f * t2)) * 0.38f;
                float sq2 = Mathf.Sin(2f * Mathf.PI * 293.66f * t2) * 0.34f;
                float sub = Mathf.Sin(2f * Mathf.PI * 155.56f * t2) * 0.28f;
                s = (sq1 + sq2 + sub) * env;
            }
            samples[i] = Mathf.Clamp(s * 0.72f, -0.88f, 0.88f);
        }

        cachedRejectedOrderFallback = AudioClip.Create("CORE_SFX_OrdenRechazada_Fallback", n, 1, SampleRate, false);
        cachedRejectedOrderFallback.SetData(samples, 0);
        return cachedRejectedOrderFallback;
    }

    public static AudioClip CrearReleMecanicoRespaldo()
    {
        if (cachedMechanicalRelayFallback != null) return cachedMechanicalRelayFallback;
        const float dur = 0.36f;
        int n = Mathf.RoundToInt(dur * SampleRate);
        float[] samples = new float[n];
        var rnd = new System.Random(707);

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float noise = (float)rnd.NextDouble() * 2f - 1f;
            float clack1 = (noise * 0.62f + Mathf.Sin(2f * Mathf.PI * 185f * t) * 0.72f) * Mathf.Exp(-t * 95f);
            float clack2 = 0f;
            if (t >= 0.048f)
            {
                float t2 = t - 0.048f;
                clack2 = (noise * 0.48f + Mathf.Sin(2f * Mathf.PI * 420f * t2) * 0.52f + Mathf.Sin(2f * Mathf.PI * 1180f * t2) * 0.26f)
                         * Mathf.Exp(-t2 * 78f);
            }
            float hum = Mathf.Sin(2f * Mathf.PI * 120f * t) * Mathf.Exp(-t * 11f) * 0.20f;
            samples[i] = Mathf.Clamp((clack1 + clack2 + hum) * 0.78f, -0.88f, 0.88f);
        }

        cachedMechanicalRelayFallback = AudioClip.Create("CORE_SFX_ReleMecanico_Fallback", n, 1, SampleRate, false);
        cachedMechanicalRelayFallback.SetData(samples, 0);
        return cachedMechanicalRelayFallback;
    }

    public static AudioClip CrearPitidoCuentaRegresiva(bool final)
    {
        if (final && cachedCountdownFinal != null) return cachedCountdownFinal;
        if (!final && cachedCountdownTick != null) return cachedCountdownTick;

        float dur = final ? 0.34f : 0.12f;
        float freq = final ? 1046.5f : 784f;
        int n = Mathf.RoundToInt(dur * SampleRate);
        float[] samples = new float[n];

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float env = Mathf.Clamp01(t * 120f) * Mathf.Exp(-t * (final ? 8.5f : 22f));
            float s = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.45f
                      + Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * 0.20f;
            samples[i] = Mathf.Clamp(s * env, -0.75f, 0.75f);
        }

        AudioClip clip = AudioClip.Create(final ? "CORE_BootBeep_Final" : "CORE_BootBeep_Tick", n, 1, SampleRate, false);
        clip.SetData(samples, 0);
        if (final) cachedCountdownFinal = clip;
        else cachedCountdownTick = clip;
        return clip;
    }
}
