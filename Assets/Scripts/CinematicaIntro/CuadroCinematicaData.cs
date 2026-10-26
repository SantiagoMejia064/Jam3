using System;
using System.Collections.Generic;
using UnityEngine;

public enum HablanteCinematica
{
    Vega = 0,
    Siete = 1,
    Sistema = 2
}

public enum EfectoCuadro
{
    Ninguno = 0,
    Glitch = 1,
    PulsoRojo = 2,
    Temblor = 3
}

public enum RespaldoSonidoExtra
{
    Ninguno = 0,
    PitidoOrdenRechazada = 1,
    ReleMecanico = 2
}

[Serializable]
public class LineaSubtitulo
{
    [Tooltip("Quién pronuncia o emite esta línea. Decide qué generador de voz suena.")]
    public HablanteCinematica hablante = HablanteCinematica.Siete;

    [TextArea(2, 5)]
    [Tooltip("Texto del subtítulo que se escribirá en pantalla.")]
    public string texto = string.Empty;

    [Min(0f)]
    [Tooltip("Duración mínima en segundos (piso editable). La duración total se calcula automáticamente según la longitud del texto y la pausa de lectura.")]
    public float duracionMinima = 2.8f;

    [Tooltip("AudioClip opcional para efectos puntuales (ej. pitido de 'orden rechazada' o relé mecánico).")]
    public AudioClip sonidoExtra;

    [Min(0f)]
    [Tooltip("Segundos desde que comienza la línea (o desde que termina de escribirse si 'dispararAlTerminarTexto' está activo).")]
    public float retrasoSonidoExtra = 0f;

    [Tooltip("Si está activo, el sonidoExtra se reproduce exactamente al terminar la escritura del texto más retrasoSonidoExtra.")]
    public bool dispararAlTerminarTexto = false;

    [Min(0f)]
    [Tooltip("Silencio adicional en segundos al terminar el texto antes de avanzar automáticamente (ej. 1 s en el cuadro 5).")]
    public float silencioFinalExtra = 0f;

    [Tooltip("Sonido procedural de respaldo si el campo sonidoExtra aún está vacío.")]
    public RespaldoSonidoExtra respaldoProcedural = RespaldoSonidoExtra.Ninguno;
}

/// <summary>
/// Datos configurables desde el Inspector para cada viñeta/cuadro de la cinemática inicial "Borrando recuerdos".
/// </summary>
[CreateAssetMenu(
    fileName = "CORE_Cuadro_Nuevo",
    menuName = "Lo Que Guarde/Cinematica Intro/Cuadro Cinematica Data",
    order = 10)]
public class CuadroCinematicaData : ScriptableObject
{
    [Header("Identificación e Imagen")]
    [Tooltip("Nombre descriptivo del cuadro. Se muestra en el placeholder si 'imagen' está vacío.")]
    public string tituloCuadro = string.Empty;

    [Tooltip("Ilustración del cuadro. Si está vacío, se dibuja un placeholder de color con el nombre del cuadro.")]
    public Sprite imagen;

    [Tooltip("Color de fondo del placeholder cuando aún no se ha arrastrado una imagen.")]
    public Color colorPlaceholder = new Color(0.08f, 0.12f, 0.16f, 1f);

    [Header("Barra de Purga (0% a 100%)")]
    [Range(0f, 100f)]
    public float barraInicio = 0f;

    [Range(0f, 100f)]
    public float barraFin = 12f;

    [Header("Líneas de Subtítulo")]
    public List<LineaSubtitulo> subtitulos = new List<LineaSubtitulo>();

    [Header("Efectos Visuales y Transiciones")]
    public EfectoCuadro efecto = EfectoCuadro.Ninguno;

    [Tooltip("Permite combinar un segundo efecto en el mismo cuadro (ej. PulsoRojo + Temblor en el cuadro 6).")]
    public EfectoCuadro efectoSecundario = EfectoCuadro.Ninguno;

    [Tooltip("Tinte de luz opcional sobre la viñeta (ej. luz ámbar en el cuadro 5). El canal Alfa controla la intensidad.")]
    public Color tinteAmbiental = new Color(1f, 0.68f, 0.22f, 0f);

    [Tooltip("Disuelve progresivamente la imagen del cuadro en bloques de memoria (Cuadro 7).")]
    public bool disolverImagen = false;

    [Tooltip("Pantalla en negro con escritura línea por línea en verde fósforo estilo terminal (Cuadro 8).")]
    public bool modoTerminalVerde = false;

    [Min(0f)]
    public float fundidoEntrada = 0.55f;

    [Min(0f)]
    public float fundidoSalida = 0.45f;

    public bool TieneEfecto(EfectoCuadro buscado)
    {
        return efecto == buscado || efectoSecundario == buscado;
    }

    public string ObtenerNombreVisible(int indiceUnoBase)
    {
        if (!string.IsNullOrWhiteSpace(tituloCuadro))
            return tituloCuadro;
        return !string.IsNullOrWhiteSpace(name) ? name : $"Cuadro {indiceUnoBase}";
    }
}

/// <summary>
/// Contenedor reutilizable de líneas de diálogo compatibles con los generadores de voz de Vega y Siete
/// (preparado como gancho de datos para la escena posterior de Fase 2).
/// </summary>
[CreateAssetMenu(
    fileName = "CORE_Dialogo_Nuevo",
    menuName = "Lo Que Guarde/Cinematica Intro/Secuencia Dialogo Data",
    order = 11)]
public class CORE_DialogoSecuenciaData : ScriptableObject
{
    [Tooltip("Líneas compatibles con HablanteCinematica (Vega / Siete / Sistema) y CORE_CinematicaVoiceGenerator.")]
    public List<LineaSubtitulo> lineas = new List<LineaSubtitulo>();
}
