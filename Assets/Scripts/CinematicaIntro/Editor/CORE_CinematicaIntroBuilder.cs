#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Herramienta manual de Editor para reconstruir o verificar CORE_CinematicaIntro bajo demanda.
/// IMPORTANTE: NO usa [InitializeOnLoad] ni ImportAsset síncrono al recargar dominio para evitar
/// agotar cursores LMDB (MDB_CURSOR_FULL) en el ArtifactDB de Unity 6 durante PostprocessAllAssets.
/// </summary>
public static class CORE_CinematicaIntroBuilder
{
    private const string RutaPrefabPrincipal = "Assets/CinematicaIntro/CORE_CinematicaIntro.prefab";
    private const string RutaPrefabResources = "Assets/CinematicaIntro/Resources/CORE_CinematicaIntro.prefab";
    private const string RutaWavOrdenRechazada = "Assets/CinematicaIntro/Audio/CORE_SFX_OrdenRechazada.wav";
    private const string RutaWavReleMecanico = "Assets/CinematicaIntro/Audio/CORE_SFX_ReleMecanico.wav";
    private const string RutaCuadro05 = "Assets/CinematicaIntro/Resources/Cuadros/CORE_Cuadro_05.asset";
    private const string RutaCuadro08 = "Assets/CinematicaIntro/Resources/Cuadros/CORE_Cuadro_08.asset";
    private const string NombreEscenaObjetivo = "Inicio-Laboratorio";

    [MenuItem("Tools/Cinematica Intro/Montar o Actualizar CORE_CinematicaIntro")]
    public static void MontarOActualizarManual()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[CORE_CinematicaIntro] Espera a que Unity termine de compilar o sal de Play Mode antes de ejecutar esta herramienta.");
            return;
        }

        AsegurarWavsPorDefectoYEnlazarCuadros();
        AsegurarJerarquiaCompletaEnPrefab(RutaPrefabPrincipal, forzarReconstruccionUI: true);
        AsegurarJerarquiaCompletaEnPrefab(RutaPrefabResources, forzarReconstruccionUI: true);
        AsegurarInstanciaEnEscenaAbierta();
        AssetDatabase.SaveAssets();
        Debug.Log("[CORE_CinematicaIntro] Prefab, audios de apoyo y escena Inicio-Laboratorio verificados correctamente.");
    }

    private static void AsegurarWavsPorDefectoYEnlazarCuadros()
    {
        string dirAbs = Path.GetFullPath("Assets/CinematicaIntro/Audio");
        if (!Directory.Exists(dirAbs))
            Directory.CreateDirectory(dirAbs);

        bool creadosNuevos = false;

        string absRechazo = Path.GetFullPath(RutaWavOrdenRechazada);
        if (!File.Exists(absRechazo))
        {
            EscribirWavOrdenRechazada(absRechazo);
            creadosNuevos = true;
        }

        string absRele = Path.GetFullPath(RutaWavReleMecanico);
        if (!File.Exists(absRele))
        {
            EscribirWavReleMecanico(absRele);
            creadosNuevos = true;
        }

        if (creadosNuevos)
            AssetDatabase.Refresh();

        CuadroCinematicaData c5 = AssetDatabase.LoadAssetAtPath<CuadroCinematicaData>(RutaCuadro05);
        AudioClip clipRechazo = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaWavOrdenRechazada);
        if (c5 != null && clipRechazo != null && c5.subtitulos != null && c5.subtitulos.Count >= 2 && c5.subtitulos[1].sonidoExtra == null)
        {
            c5.subtitulos[1].sonidoExtra = clipRechazo;
            EditorUtility.SetDirty(c5);
        }

        CuadroCinematicaData c8 = AssetDatabase.LoadAssetAtPath<CuadroCinematicaData>(RutaCuadro08);
        AudioClip clipRele = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaWavReleMecanico);
        if (c8 != null && clipRele != null && c8.subtitulos != null && c8.subtitulos.Count >= 1 && c8.subtitulos[0].sonidoExtra == null)
        {
            c8.subtitulos[0].sonidoExtra = clipRele;
            EditorUtility.SetDirty(c8);
        }
    }

    private static void AsegurarJerarquiaCompletaEnPrefab(string rutaPrefab, bool forzarReconstruccionUI = false)
    {
        if (!File.Exists(Path.GetFullPath(rutaPrefab)))
            return;

        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab);
        if (prefabAsset == null)
            return;

        bool tieneHijosUI = prefabAsset.transform.Find("CORE_MarcoAlertaSuperior") != null;
        if (tieneHijosUI && !forzarReconstruccionUI)
            return;

        GameObject root = PrefabUtility.LoadPrefabContents(rutaPrefab);
        try
        {
            CinematicaIntroController ctrl = root.GetComponent<CinematicaIntroController>();
            if (ctrl != null)
            {
                ctrl.ConstruirOVincularUI();
                PrefabUtility.SaveAsPrefabAsset(root, rutaPrefab);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void AsegurarInstanciaEnEscenaAbierta()
    {
        Scene activa = SceneManager.GetActiveScene();
        if (!activa.IsValid() || !activa.isLoaded)
            return;

        if (!string.Equals(activa.name, NombreEscenaObjetivo, StringComparison.OrdinalIgnoreCase))
            return;

        CinematicaIntroController existente = UnityEngine.Object.FindFirstObjectByType<CinematicaIntroController>(FindObjectsInactive.Include);
        if (existente != null)
            return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabPrincipal);
        if (prefab == null)
            return;

        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, activa);
        if (inst != null)
        {
            inst.name = "CORE_CinematicaIntro";
            EditorSceneManager.MarkSceneDirty(activa);
        }
    }

    private static void EscribirWavOrdenRechazada(string rutaAbsoluta)
    {
        const int sr = 22050;
        const float dur = 0.52f;
        int n = Mathf.RoundToInt(sr * dur);
        float[] buf = new float[n];

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            float s = 0f;
            if (t < 0.17f)
            {
                float env = Mathf.Clamp01(t * 80f) * Mathf.Exp(-t * 4.5f);
                float sq = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 520f * t)) * 0.42f;
                float sub = Mathf.Sin(2f * Mathf.PI * 260f * t) * 0.35f;
                s = (sq + sub) * env;
            }
            else if (t >= 0.20f)
            {
                float t2 = t - 0.20f;
                float env = Mathf.Clamp01(t2 * 80f) * Mathf.Clamp01((0.32f - t2) / 0.12f) * Mathf.Exp(-t2 * 3.0f);
                float sq1 = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 311.13f * t2)) * 0.40f;
                float sq2 = Mathf.Sin(2f * Mathf.PI * 293.66f * t2) * 0.35f;
                float sub = Mathf.Sin(2f * Mathf.PI * 155.56f * t2) * 0.30f;
                s = (sq1 + sq2 + sub) * env;
            }
            buf[i] = s * 0.72f;
        }
        GuardarPcm16MonoWav(rutaAbsoluta, sr, buf);
    }

    private static void EscribirWavReleMecanico(string rutaAbsoluta)
    {
        const int sr = 22050;
        const float dur = 0.36f;
        int n = Mathf.RoundToInt(sr * dur);
        float[] buf = new float[n];
        System.Random rnd = new System.Random(707);

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
            float clack1 = (noise * 0.65f + Mathf.Sin(2f * Mathf.PI * 185f * t) * 0.75f) * Mathf.Exp(-t * 95f);
            float clack2 = 0f;
            if (t >= 0.048f)
            {
                float t2 = t - 0.048f;
                clack2 = (noise * 0.50f + Mathf.Sin(2f * Mathf.PI * 420f * t2) * 0.55f + Mathf.Sin(2f * Mathf.PI * 1180f * t2) * 0.28f)
                         * Mathf.Exp(-t2 * 78f);
            }
            float hum = Mathf.Sin(2f * Mathf.PI * 120f * t) * Mathf.Exp(-t * 11f) * 0.22f;
            buf[i] = (clack1 + clack2 + hum) * 0.80f;
        }
        GuardarPcm16MonoWav(rutaAbsoluta, sr, buf);
    }

    private static void GuardarPcm16MonoWav(string ruta, int sampleRate, float[] samples)
    {
        int numSamples = samples.Length;
        int dataBytes = numSamples * 2;
        using MemoryStream ms = new MemoryStream(44 + dataBytes);
        using BinaryWriter bw = new BinaryWriter(ms, Encoding.ASCII);

        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + dataBytes);
        bw.Write(Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)1);
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2);
        bw.Write((short)2);
        bw.Write((short)16);
        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write(dataBytes);

        for (int i = 0; i < numSamples; i++)
        {
            float v = Mathf.Clamp(samples[i], -0.98f, 0.98f);
            short s16 = (short)Mathf.RoundToInt(v * 32767f);
            bw.Write(s16);
        }

        bw.Flush();
        File.WriteAllBytes(ruta, ms.ToArray());
    }
}
#endif
