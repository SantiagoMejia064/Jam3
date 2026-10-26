using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Instala y conecta automáticamente todos los AudioSource (y el AudioListener de INV_Camara_25D)
/// exclusivamente dentro de la escena Invernadero en el Editor, sin tocar ninguna otra escena ni sobrescribir
/// los audios o volúmenes que el usuario vaya asignando en el Inspector.
/// </summary>
[InitializeOnLoad]
public static class INV_AudioAutoInstaller
{
    static INV_AudioAutoInstaller()
    {
        EditorApplication.delayCall += TrySetupActiveInvernaderoScene;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (scene.IsValid() && scene.isLoaded && scene.name == "Invernadero")
        {
            InstallAudioSourcesInScene(scene, logSummary: false);
        }
    }

    private static void TrySetupActiveInvernaderoScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.IsValid() && scene.isLoaded && scene.name == "Invernadero")
            {
                InstallAudioSourcesInScene(scene, logSummary: false);
            }
        }
    }

    [MenuItem("Jam3/Invernadero/Crear y Conectar AudioSources")]
    public static void ManualInstallMenu()
    {
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || active.name != "Invernadero")
        {
            EditorUtility.DisplayDialog(
                "Audio Invernadero",
                "Abre primero la escena 'Invernadero' para crear o verificar sus AudioSources.",
                "Entendido"
            );
            return;
        }

        InstallAudioSourcesInScene(active, logSummary: true);
    }

    private static void InstallAudioSourcesInScene(Scene scene, bool logSummary)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != "Invernadero") return;

        bool wasDirtyBefore = scene.isDirty;
        bool changed = false;

        // 1. Garantizar AudioListener en la cámara activa INV_Camara_25D
        Transform cam25D = FindInSceneByName(scene, "INV_Camara_25D");
        if (cam25D != null && cam25D.GetComponent<AudioListener>() == null)
        {
            var listener = cam25D.gameObject.AddComponent<AudioListener>();
            listener.enabled = true;
            EditorUtility.SetDirty(cam25D.gameObject);
            changed = true;
        }

        // 2. INV_ControladorNivel (Ambiente, Vega Umbral A4 y Marchitamiento de Hojas)
        INV_RoomController roomCtrl = FindComponentInScene<INV_RoomController>(scene);
        if (roomCtrl != null)
        {
            AudioSource amb = EnsureChildAudioSource(roomCtrl.transform, "Audio_Ambiente_Invernadero", 0.32f, loop: true, isUI: false, ref changed);
            AudioSource vegaA4 = EnsureChildAudioSource(roomCtrl.transform, "Audio_Vega_Entrada_A4", 0.90f, loop: false, isUI: false, ref changed);
            AudioSource wither = EnsureChildAudioSource(roomCtrl.transform, "Audio_Marchitamiento_Plantas", 0.80f, loop: false, isUI: false, ref changed);

            changed |= WireObjectReference(roomCtrl, "ambienteInvernaderoSource", amb);
            changed |= WireObjectReference(roomCtrl, "vegaEntradaA4Source", vegaA4);
            changed |= WireObjectReference(roomCtrl, "marchitamientoPlantasSource", wither);
        }

        // 3. INV_Incubadora_Interactable (4 piezas, germinación, alarma en loop y las 3 intervenciones de Vega)
        INV_IncubatorPuzzle puzzle = FindComponentInScene<INV_IncubatorPuzzle>(scene);
        if (puzzle != null)
        {
            AudioSource sCalor = EnsureChildAudioSource(puzzle.transform, "Audio_Montaje_1_Calor", 0.75f, loop: false, isUI: false, ref changed);
            AudioSource sOsc = EnsureChildAudioSource(puzzle.transform, "Audio_Montaje_2_Oscuridad", 0.80f, loop: false, isUI: false, ref changed);
            AudioSource sPulso = EnsureChildAudioSource(puzzle.transform, "Audio_Montaje_3_Pulso", 0.80f, loop: false, isUI: false, ref changed);
            AudioSource sBruma = EnsureChildAudioSource(puzzle.transform, "Audio_Montaje_4_Bruma", 0.75f, loop: false, isUI: false, ref changed);
            AudioSource sGerm = EnsureChildAudioSource(puzzle.transform, "Audio_Germinacion_Ella", 0.95f, loop: false, isUI: false, ref changed);
            AudioSource sAlarma = EnsureChildAudioSource(puzzle.transform, "Audio_Alarma_Esporas_Loop", 0.45f, loop: false, isUI: false, ref changed);
            if (sAlarma != null && sAlarma.loop)
            {
                sAlarma.loop = false;
                EditorUtility.SetDirty(sAlarma);
                changed = true;
            }
            AudioSource sVega1 = EnsureChildAudioSource(puzzle.transform, "Audio_Vega_Alerta_1", 0.92f, loop: false, isUI: false, ref changed);
            AudioSource sVega2 = EnsureChildAudioSource(puzzle.transform, "Audio_Vega_Alerta_2", 0.92f, loop: false, isUI: false, ref changed);
            AudioSource sVega3 = EnsureChildAudioSource(puzzle.transform, "Audio_Vega_Alerta_3", 0.92f, loop: false, isUI: false, ref changed);

            changed |= WireObjectReference(puzzle, "audioMontajeCalor", sCalor);
            changed |= WireObjectReference(puzzle, "audioMontajeOscuridad", sOsc);
            changed |= WireObjectReference(puzzle, "audioMontajePulso", sPulso);
            changed |= WireObjectReference(puzzle, "audioMontajeBruma", sBruma);
            changed |= WireObjectReference(puzzle, "audioGerminacionElla", sGerm);
            changed |= WireObjectReference(puzzle, "audioAlarmaEsporas", sAlarma);
            changed |= WireObjectReference(puzzle, "audioVegaAlerta1", sVega1);
            changed |= WireObjectReference(puzzle, "audioVegaAlerta2", sVega2);
            changed |= WireObjectReference(puzzle, "audioVegaAlerta3", sVega3);
        }

        // 4. INV_MesaTrabajo_Interactable (Planilla de Cultivo N8)
        INV_WorkTable workTable = FindComponentInScene<INV_WorkTable>(scene);
        if (workTable != null)
        {
            AudioSource sMesa = EnsureDirectAudioSource(workTable.gameObject, 0.80f, loop: false, ref changed);
            changed |= WireObjectReference(workTable, "audioSource", sMesa);
        }

        // 5. INV_RestosElla_Interactable (Maceta quemada)
        INV_RestosElla restosElla = FindComponentInScene<INV_RestosElla>(scene);
        if (restosElla != null)
        {
            AudioSource sRestos = EnsureDirectAudioSource(restosElla.gameObject, 0.75f, loop: false, ref changed);
            changed |= WireObjectReference(restosElla, "audioSource", sRestos);
        }

        // 6. INV_Ventanal_Interactable (Ventanal)
        INV_Ventanal ventanal = FindComponentInScene<INV_Ventanal>(scene);
        if (ventanal != null)
        {
            AudioSource sVentanal = EnsureDirectAudioSource(ventanal.gameObject, 0.70f, loop: false, ref changed);
            changed |= WireObjectReference(ventanal, "audioSource", sVentanal);
        }

        // 7. INV_Puerta_Pasillo (Salida hacia el Pasillo / Javier)
        Transform puertaPasillo = FindInSceneByName(scene, "INV_Puerta_Pasillo");
        if (puertaPasillo != null)
        {
            AudioSource sPuerta = EnsureDirectAudioSource(puertaPasillo.gameObject, 0.80f, loop: false, ref changed);
            INV_DoorAudioTrigger doorTrigger = puertaPasillo.GetComponent<INV_DoorAudioTrigger>();
            if (doorTrigger == null)
            {
                doorTrigger = puertaPasillo.gameObject.AddComponent<INV_DoorAudioTrigger>();
                EditorUtility.SetDirty(puertaPasillo.gameObject);
                changed = true;
            }
            changed |= WireObjectReference(doorTrigger, "audioSource", sPuerta);
        }

        // 8. INV_Carrito_Incubadora (Extracción de Ella y Rodaje del carrito)
        INV_IncubatorCart cart = FindComponentInScene<INV_IncubatorCart>(scene);
        if (cart != null)
        {
            AudioSource sSacar = EnsureChildAudioSource(cart.transform, "Audio_Sacar_Ella_Carrito", 0.85f, loop: false, isUI: false, ref changed);
            AudioSource sRuedas = EnsureChildAudioSource(cart.transform, "Audio_Ruedas_Carrito", 0.55f, loop: true, isUI: false, ref changed);

            changed |= WireObjectReference(cart, "sacarEllaSource", sSacar);
            changed |= WireObjectReference(cart, "ruedasCarritoSource", sRuedas);
        }

        // 9. INV_Canvas_Incubadora (Panel_Afinacion, Audio_Afinacion_Completada y Panel_Revelacion)
        INV_IncubatorUI incUI = FindComponentInScene<INV_IncubatorUI>(scene);
        if (incUI != null)
        {
            AudioSource sClick = EnsureDirectAudioSource(incUI.gameObject, 0.70f, loop: false, ref changed);
            changed |= WireObjectReference(incUI, "perillaClickSource", sClick);

            Transform canvasParent = incUI.transform.parent != null ? incUI.transform.parent : incUI.transform;
            AudioSource sComplete = EnsureChildAudioSource(canvasParent, "Audio_Afinacion_Completada", 0.85f, loop: false, isUI: true, ref changed);
            changed |= WireObjectReference(incUI, "afinacionCompletadaSource", sComplete);
        }

        INV_RevelationUI revUI = FindComponentInScene<INV_RevelationUI>(scene);
        if (revUI != null)
        {
            AudioSource sRev = EnsureDirectAudioSource(revUI.gameObject, 0.85f, loop: false, ref changed);
            changed |= WireObjectReference(revUI, "vegaRevelationSource", sRev);
        }

        // 10. CORE_Siete de referencia en Invernadero (para pruebas directas en la escena)
        Transform coreSieteTr = FindInSceneByName(scene, "CORE_Siete");
        if (coreSieteTr != null && coreSieteTr.TryGetComponent(out CORE_PlayerController sietePc))
        {
            AudioSource sRobot = EnsureDirectAudioSource(coreSieteTr.gameObject, 0.55f, loop: true, ref changed);
            changed |= WireObjectReference(sietePc, "movimientoSource", sRobot);

            AudioClip pasosClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sonidos/pasosRobot.mp3");
            if (pasosClip != null)
            {
                if (sRobot.clip == null && sRobot.resource == null)
                {
                    sRobot.clip = pasosClip;
                    EditorUtility.SetDirty(sRobot);
                    changed = true;
                }
                changed |= WireObjectReferenceIfEmpty(sietePc, "movimientoRobot", pasosClip);
            }
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!wasDirtyBefore)
            {
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("[Invernadero Audio] Todos los AudioSources de la escena Invernadero están creados y conectados en la Hierarchy.");
        }
        else if (logSummary)
        {
            Debug.Log("[Invernadero Audio] Verificación completada: todos los AudioSources del Invernadero ya están listos y conectados.");
        }
    }

    private static AudioSource EnsureDirectAudioSource(GameObject go, float defaultVol, bool loop, ref bool changed)
    {
        AudioSource src = go.GetComponent<AudioSource>();
        if (src == null)
        {
            src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            src.volume = defaultVol;
            EditorUtility.SetDirty(go);
            changed = true;
        }
        return src;
    }

    private static AudioSource EnsureChildAudioSource(
        Transform parent,
        string childName,
        float defaultVol,
        bool loop,
        bool isUI,
        ref bool changed)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
        {
            return EnsureDirectAudioSource(existing.gameObject, defaultVol, loop, ref changed);
        }

        GameObject childGo = new GameObject(childName);
        if (isUI)
        {
            RectTransform rt = childGo.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }
        else
        {
            childGo.transform.SetParent(parent, false);
            childGo.transform.localPosition = Vector3.zero;
        }

        AudioSource src = childGo.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 0f;
        src.volume = defaultVol;
        EditorUtility.SetDirty(childGo);
        EditorUtility.SetDirty(parent.gameObject);
        changed = true;
        return src;
    }

    private static bool WireObjectReference(Object targetComponent, string fieldName, Object value)
    {
        if (targetComponent == null || value == null) return false;
        SerializedObject so = new SerializedObject(targetComponent);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null && prop.objectReferenceValue != value)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(targetComponent);
            return true;
        }
        return false;
    }

    private static bool WireObjectReferenceIfEmpty(Object targetComponent, string fieldName, Object value)
    {
        if (targetComponent == null || value == null) return false;
        SerializedObject so = new SerializedObject(targetComponent);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null && prop.objectReferenceValue == null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(targetComponent);
            return true;
        }
        return false;
    }

    private static Transform FindInSceneByName(Scene scene, string exactName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t != null && t.name == exactName)
                {
                    return t;
                }
            }
        }
        return null;
    }

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }
}
