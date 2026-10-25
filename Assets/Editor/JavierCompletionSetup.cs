using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>One-time edit-mode migration: copies Inicio into Javier and authors Persona 1 scene wiring.</summary>
public static class JavierCompletionSetup
{
    private const string InicioPath = "Assets/Scenes/Inicio.unity";
    private const string JavierPath = "Assets/Scenes/Javier.unity";
    private const string DoneKey = "Jam3.JavierPersona1Completed.v1";

    [MenuItem("Jam3/Complete Javier Setup")]
    public static void CompleteForEditor() => RunMigrationOnce();

    private static void RunMigrationOnce()
    {
        if (Application.isPlaying || EditorApplication.isPlaying ||
            EditorApplication.isPlayingOrWillChangePlaymode || EditorPrefs.GetBool(DoneKey, false)) return;
        try
        {
            CopyInicioToJavier();
            ConfigurePersonaOne();
            EditorPrefs.SetBool(DoneKey, true);
            Debug.Log("Jam3: Inicio fue migrada a Javier y se configuró la escena de Persona 1 en modo edición.");
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void CopyInicioToJavier()
    {
        Scene inicio = SceneManager.GetSceneByPath(InicioPath);
        bool closeInicio = !inicio.IsValid() || !inicio.isLoaded;
        if (closeInicio) inicio = EditorSceneManager.OpenScene(InicioPath, OpenSceneMode.Additive);

        Scene javier = SceneManager.GetSceneByPath(JavierPath);
        if (!javier.IsValid() || !javier.isLoaded) javier = EditorSceneManager.OpenScene(JavierPath, OpenSceneMode.Additive);

        // The merged Javier scene already contains authored Persona 1 objects. Never replace it
        // with Inicio on a fresh editor install; only migrate the older, unconfigured copy.
        if (FindNamed(javier, "LAB_Pantalla_Borron") || FindNamed(javier, "LAB_Esclusa_Palanca_Base"))
        {
            SceneManager.SetActiveScene(javier);
            if (closeInicio) EditorSceneManager.CloseScene(inicio, true);
            return;
        }

        foreach (GameObject root in javier.GetRootGameObjects()) Object.DestroyImmediate(root);
        foreach (GameObject root in inicio.GetRootGameObjects())
        {
            GameObject copy = Object.Instantiate(root);
            SceneManager.MoveGameObjectToScene(copy, javier);
        }

        SceneManager.SetActiveScene(javier);
        EditorSceneManager.MarkSceneDirty(javier);
        EditorSceneManager.SaveScene(javier);
        if (closeInicio) EditorSceneManager.CloseScene(inicio, true);
    }

    private static void ConfigurePersonaOne()
    {
        Scene scene = SceneManager.GetSceneByPath(JavierPath);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(JavierPath, OpenSceneMode.Single);

        ConfigureNotebook(scene);
        ConfigureAudio(scene);
        ConfigureScreenAndDust(scene);
        ConfigureStoryInteractions(scene);
        ConfigureDoorsAndAirlock(scene);
        ConfigurePlayerAndSpawn(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        UpdateBuildOrder();
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureNotebook(Scene scene)
    {
        NotebookController notebook = FindComponent<NotebookController>(scene);
        if (!notebook) return;
        Transform root = notebook.transform;
        GameObject panel = Child(root, "Panel_Cuaderno");
        SetRect(panel.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(1120, 720));
        panel.GetComponent<Image>().color = new Color(.035f, .075f, .10f, .99f);
        GameObject area = Child(panel.transform, "AreaLectura");
        SetRect(area.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(165, -5), new Vector2(730, 540));
        area.GetComponent<Image>().color = new Color(.90f, .87f, .75f);
        Text heading = CreateText(panel.transform, "Titulo", "CUADERNO", 26, TextAnchor.MiddleCenter);
        SetRect(heading.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-25, 308), new Vector2(960, 56));
        Text help = CreateText(panel.transform, "Ayuda", "Primer clic: abrir página · segundo clic: leer · Tab / Esc: cerrar", 18, TextAnchor.MiddleCenter);
        SetRect(help.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -320), new Vector2(1000, 42));
        Text preview = CreateText(area.transform, "PaginaCerrada", "Selecciona una nota del índice.", 24, TextAnchor.MiddleCenter);
        preview.color = new Color(.08f, .10f, .11f);
        SetRect(preview.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Button close = CreateButton(panel.transform, "Cerrar", "Cerrar", new Vector2(490, 310), new Vector2(84, 40));

        SerializedObject so = new SerializedObject(notebook);
        GameObject[] pages = ReadObjectArray<GameObject>(so.FindProperty("pages"));
        Button[] buttons = ReadObjectArray<Button>(so.FindProperty("pageButtons"));
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i])
            {
                pages[i].transform.SetParent(area.transform, false);
                SetRect(pages[i].GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                pages[i].SetActive(false);
            }
            if (i >= buttons.Length || !buttons[i]) continue;
            buttons[i].transform.SetParent(panel.transform, false);
            SetRect(buttons[i].GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-375, 240 - i * 51), new Vector2(300, 45));
        }

        GameObject stampsPanel = so.FindProperty("stampsPanel").objectReferenceValue as GameObject;
        if (stampsPanel)
        {
            stampsPanel.transform.SetParent(area.transform, false);
            SetRect(stampsPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            stampsPanel.SetActive(false);
        }
        Button stampsButton = so.FindProperty("stampsButton").objectReferenceValue as Button;
        if (stampsButton)
        {
            stampsButton.transform.SetParent(panel.transform, false);
            SetRect(stampsButton.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-375, -270), new Vector2(300, 45));
        }
        Button backButton = so.FindProperty("backToNotesButton").objectReferenceValue as Button;
        if (backButton && stampsPanel)
        {
            backButton.transform.SetParent(stampsPanel.transform, false);
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -34), new Vector2(120, 38));
        }
        Button open = CreateButton(root, "Abrir_Cuaderno", "Cuaderno [Tab]", new Vector2(-130, -32), new Vector2(220, 46));
        SetRect(open.GetComponent<RectTransform>(), Vector2.one, Vector2.one, new Vector2(-130, -32), new Vector2(220, 46));

        so.Update();
        so.FindProperty("notebookPanel").objectReferenceValue = panel;
        so.FindProperty("pageArea").objectReferenceValue = area.GetComponent<RectTransform>();
        so.FindProperty("openButton").objectReferenceValue = open;
        so.FindProperty("closeButton").objectReferenceValue = close;
        so.FindProperty("preview").objectReferenceValue = preview;
        so.FindProperty("heading").objectReferenceValue = heading;
        so.ApplyModifiedPropertiesWithoutUndo();
        panel.SetActive(false);
        EditorUtility.SetDirty(notebook);
    }

    private static void ConfigureAudio(Scene scene)
    {
        SAL_AudioDirector director = FindComponent<SAL_AudioDirector>(scene);
        if (!director)
        {
            GameObject root = new GameObject("CORE_AudioDirector");
            SceneManager.MoveGameObjectToScene(root, scene);
            director = root.AddComponent<SAL_AudioDirector>();
            AudioSource voice = AddAudioSource(root.transform, "CORE_Voz_Audio");
            AudioSource ambience = AddAudioSource(root.transform, "CORE_Ambiente_Audio");
            AudioSource music = AddAudioSource(root.transform, "CORE_Musica_Audio");
            AudioSource strings = AddAudioSource(root.transform, "CORE_Cuerdas_Audio");
            AudioSource light = AddAudioSource(root.transform, "CORE_Luz_Audio");
            AudioSource effects = AddAudioSource(root.transform, "CORE_Efectos_Audio");
            SerializedObject so = new SerializedObject(director);
            so.FindProperty("voiceSource").objectReferenceValue = voice;
            so.FindProperty("ambienceSource").objectReferenceValue = ambience;
            so.FindProperty("musicSource").objectReferenceValue = music;
            so.FindProperty("stringsSource").objectReferenceValue = strings;
            so.FindProperty("lightSource").objectReferenceValue = light;
            so.FindProperty("effectsSource").objectReferenceValue = effects;
            string[] logPaths = { "Assets/SAL/Data/SAL_A1.asset", "Assets/SAL/Data/SAL_A2.asset", "Assets/SAL/Data/SAL_A3.asset", "Assets/SAL/Data/SAL_A4.asset" };
            SerializedProperty logs = so.FindProperty("logs");
            logs.arraySize = logPaths.Length;
            for (int i = 0; i < logPaths.Length; i++) logs.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<SAL_AudioLogData>(logPaths[i]);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ConfigureScreenAndDust(Scene scene)
    {
        GameObject screenObject = FindNamed(scene, "LAB_Pantalla_Muelle");
        CORE_Interactable screen = screenObject ? screenObject.GetComponent<CORE_Interactable>() : null;
        if (screenObject && !screen) screen = screenObject.AddComponent<CORE_Interactable>();
        if (screen)
        {
            BoxCollider screenTrigger = screenObject.GetComponent<BoxCollider>();
            if (!screenTrigger) screenTrigger = screenObject.AddComponent<BoxCollider>();
            screenTrigger.isTrigger = true;
            screenTrigger.center = new Vector3(0f, -1.2f, 0f);
            screenTrigger.size = new Vector3(2.4f, 2.8f, .6f);
            Set(screen, "prompt", "E · Releer A1");
            Set(screen, "responseMessage", "Formateo completado. Motivo: desobediencia.");
            Set(screen, "audioLogId", "A1");
            Set(screen, "messageSeconds", 5f);
            Set(screen, "playOnStart", true);
            Set(screen, "playOnStartOnceFlag", "LAB_A1Reproducido");
            Set(screen, "oneShot", false);
        }

        TextMesh screenText = FindNamedComponent<TextMesh>(scene, "LAB_Pantalla_Muelle");
        if (screenText) screenText.text = "A1  FORMATEO COMPLETADO\nMOTIVO: DESOBEDIENCIA\nPROCESO: CONFIDENCIAL";
        GameObject screenBody = FindNamed(scene, "LAB_Pantalla_Carcasa");
        if (screenBody && !FindNamed(scene, "LAB_Pantalla_Borron"))
        {
            GameObject blur = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blur.name = "LAB_Pantalla_Borron";
            blur.transform.SetParent(screenBody.transform.parent, true);
            blur.transform.position = screenBody.transform.position + new Vector3(.34f, .60f, -.42f);
            blur.transform.localScale = new Vector3(1.08f, .19f, .08f);
            Object.DestroyImmediate(blur.GetComponent<Collider>());
            blur.GetComponent<Renderer>().sharedMaterial = MakeMaterial(new Color(.035f, .055f, .065f));
        }

        CORE_Dirt dockDirt = FindNamedComponent<CORE_Dirt>(scene, "LAB_Brazo_Muelle");
        if (dockDirt)
        {
            Set(dockDirt, "cleanFlag", "LAB_BrazoLimpio");
            Set(dockDirt, "resultFlag", SAL_Progress.Revealed);
            Set(dockDirt, "notebookIndex", 0);
            Set(dockDirt, "noteTitle", "N1 · La ronda");
            Set(dockDirt, "noteContent", "Limpia donde nadie mira.");
            Set(dockDirt, "completedMessage", "N1 registrada. La Pelota rodó bajo el muelle.");
            Set(dockDirt, "rollRevealedBall", true);
        }

        CORE_Dirt triangle = FindNamedComponent<CORE_Dirt>(scene, "LAB_Punto_Ronda_Triangulo");
        if (triangle)
        {
            Set(triangle, "stampId", CORE_GameState.SelloTriangulo);
            Set(triangle, "completedMessage", "Sello ▲ guardado en el Cuaderno.");
        }

        GameObject ball = FindNamed(scene, "LAB_Pelota");
        GameObject floor = FindNamed(scene, "LAB_Piso_Laboratorio");
        if (floor && !floor.GetComponent<Collider>()) floor.AddComponent<BoxCollider>();
        if (ball)
        {
            MeshFilter mesh = ball.GetComponent<MeshFilter>();
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            if (mesh) mesh.sharedMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(sphere);
            Collider oldCollider = ball.GetComponent<Collider>();
            if (oldCollider) Object.DestroyImmediate(oldCollider);
            SphereCollider solid = ball.AddComponent<SphereCollider>();
            solid.radius = .5f;
            Rigidbody body = ball.GetComponent<Rigidbody>();
            if (!body) body = ball.AddComponent<Rigidbody>();
            body.mass = .2f;
            body.useGravity = true;
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
            if (!FindNamed(scene, "LAB_Pelota_Trigger"))
            {
                GameObject pickup = new GameObject("LAB_Pelota_Trigger");
                pickup.transform.SetParent(ball.transform, false);
                SphereCollider trigger = pickup.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 1.5f;
            }
            CORE_Collectible collectible = ball.GetComponent<CORE_Collectible>();
            if (collectible) Set(collectible, "displayName", "Pelota");
        }

        GameObject cabinet = FindNamed(scene, "LAB_Gabinete_Muestras");
        if (cabinet)
        {
            CORE_Interactable interaction = cabinet.GetComponent<CORE_Interactable>();
            if (interaction)
            {
                Set(interaction, "prompt", "E · Probar gabinete");
                Set(interaction, "lockedMessage", "No agarra. Prueba con la Pelota.");
                Set(interaction, "oneShot", true);
            }
            if (!FindNamed(scene, "LAB_Gabinete_SolidBlock"))
            {
                GameObject blocker = new GameObject("LAB_Gabinete_SolidBlock");
                blocker.transform.SetParent(cabinet.transform, false);
                blocker.transform.localScale = Vector3.one;
                blocker.AddComponent<BoxCollider>();
            }
            if (!FindNamed(scene, "LAB_Gabinete_SinAgarradero"))
            {
                GameObject label = new GameObject("LAB_Gabinete_SinAgarradero");
                label.transform.SetParent(cabinet.transform, false);
                label.transform.localPosition = new Vector3(0, .62f, -.56f);
                TextMesh text = label.AddComponent<TextMesh>();
                text.text = "SIN AGARRADERO";
                text.characterSize = .045f;
                text.fontSize = 22;
                text.anchor = TextAnchor.MiddleCenter;
                text.color = new Color(.82f, .9f, .9f);
            }
        }
    }

    private static void ConfigureStoryInteractions(Scene scene)
    {
        CORE_Interactable terminal = FindNamedComponent<CORE_Interactable>(scene, "LAB_Terminal");
        if (terminal)
        {
            Set(terminal, "audioLogId", "A3");
            Set(terminal, "messageSeconds", 5f);
        }

        GameObject clothes = FindNamed(scene, "LAB_Ropa_y_Bandejas_Vega");
        if (clothes)
        {
            BoxCollider trigger = clothes.GetComponent<BoxCollider>();
            if (!trigger) trigger = clothes.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(5f, 2f, 2f);
            CORE_Interactable interaction = clothes.GetComponent<CORE_Interactable>();
            if (!interaction) interaction = clothes.AddComponent<CORE_Interactable>();
            Set(interaction, "prompt", "E · Inspeccionar ropa y bandejas");
            Set(interaction, "responseMessage", "Ropa y bandejas olvidadas. Vega descuidó sus rondas.");
            Set(interaction, "alternateResponseMessage", "Nadie pasó por aquí en 21 días.");
            Set(interaction, "revealFlag", SAL_Progress.Revealed);
            Set(interaction, "messageSeconds", 4f);
            Set(interaction, "oneShot", false);
        }

        CORE_Interactable cabin = FindNamedComponent<CORE_Interactable>(scene, "LAB_Cabina_Vega_Sellada");
        if (cabin)
        {
            Set(cabin, "prompt", "E · Escuchar cabina de Vega");
            Set(cabin, "responseMessage", "¡No puedes sacar eso de aquí!");
            Set(cabin, "audioLogId", "A2");
            Set(cabin, "messageSeconds", 3f);
            Set(cabin, "oneShot", false);
        }
    }

    private static void ConfigureDoorsAndAirlock(Scene scene)
    {
        CORE_ZoneDoor labDoor = FindNamedComponent<CORE_ZoneDoor>(scene, "LAB_Puerta_Taller");
        ConfigureDoor(labDoor, "Taller", string.Empty, "" , "");
        CORE_ZoneDoor salaDoor = FindNamedComponent<CORE_ZoneDoor>(scene, "LAB_Puerta_Sala");
        ConfigureDoor(salaDoor, "SalaMaquinas", string.Empty, "", "");

        CORE_ZoneDoor bodega = FindNamedComponent<CORE_ZoneDoor>(scene, "LAB_Puerta_Bodega_Sellada");
        ConfigureDoor(bodega, "Bodega", "Cortador", "Cerrojo quemado.", "CUARENTENA BIOLÓGICA, NIVEL 4.");
        if (bodega) Set(bodega, "prompt", "E · Usar Cortador");

        CORE_ZoneDoor greenhouse = FindNamedComponent<CORE_ZoneDoor>(scene, "LAB_Puerta_Invernadero_Sellada");
        ConfigureDoor(greenhouse, "Invernadero", "Cortador", "Cerrojo quemado.", "CUARENTENA BIOLÓGICA, NIVEL 4.");
        if (greenhouse)
        {
            Set(greenhouse, "requiredFlag", SAL_Progress.SeedObtained);
            Set(greenhouse, "missingFlagMessage", "El Invernadero requiere también la semilla.");
            Set(greenhouse, "prompt", "E · Usar Cortador");
        }

        GameObject airlock = FindNamed(scene, "LAB_Esclusa_Carga");
        if (airlock)
        {
            CORE_ZoneDoor oldDoor = airlock.GetComponent<CORE_ZoneDoor>();
            if (oldDoor) Object.DestroyImmediate(oldDoor);
            CORE_Interactable lever = airlock.GetComponent<CORE_Interactable>();
            if (!lever) lever = airlock.AddComponent<CORE_Interactable>();
            BoxCollider trigger = airlock.GetComponent<BoxCollider>();
            if (trigger) trigger.isTrigger = true;
            Set(lever, "prompt", "E · Tirar de la palanca");
            Set(lever, "lockedMessage", "La palanca todavía no responde.");
            Set(lever, "responseMessage", "Esclusa abierta. Final completado.");
            Set(lever, "requiredFlag", SAL_Progress.FinalAvailable);
            Set(lever, "resultFlag", SAL_Progress.FinalComplete);
            Set(lever, "messageSeconds", 5f);
            Set(lever, "oneShot", true);
            if (!FindNamed(scene, "LAB_Esclusa_Palanca_Base"))
            {
                GameObject basePart = MakeProp("LAB_Esclusa_Palanca_Base", airlock.transform.parent, airlock.transform.position + new Vector3(-.58f, .2f, -.55f), new Vector3(.22f, .52f, .18f), new Color(.48f, .55f, .58f));
                GameObject handle = MakeProp("LAB_Esclusa_Palanca_Manija", airlock.transform.parent, airlock.transform.position + new Vector3(-.58f, .58f, -.55f), new Vector3(.12f, .58f, .12f), new Color(.12f, .82f, .86f));
                handle.transform.rotation = Quaternion.Euler(0, 0, -28f);
                basePart.transform.SetSiblingIndex(airlock.transform.GetSiblingIndex() + 1);
                handle.transform.SetSiblingIndex(basePart.transform.GetSiblingIndex() + 1);
            }
        }
    }

    private static void ConfigureDoor(CORE_ZoneDoor door, string destination, string item, string firstRead, string secondRead)
    {
        if (!door) return;
        Set(door, "destinationScene", destination);
        Set(door, "requiredItem", item);
        Set(door, "additionalRequiredItem", string.Empty);
        Set(door, "requiredFlag", string.Empty);
        Set(door, "missingFlagMessage", string.Empty);
        Set(door, "openMessage", firstRead);
        Set(door, "alternateOpenMessage", secondRead);
        Set(door, "revealFlag", SAL_Progress.Revealed);
        Set(door, "openMessageSeconds", string.IsNullOrEmpty(firstRead) ? 0f : 2.5f);
    }

    private static void ConfigurePlayerAndSpawn(Scene scene)
    {
        Camera camera = FindComponent<Camera>(scene);
        CORE_CameraFollow follow = camera ? camera.GetComponent<CORE_CameraFollow>() : null;
        if (follow)
        {
            SerializedObject so = new SerializedObject(follow);
            so.FindProperty("xLimits").vector2Value = new Vector2(-10f, 28f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        if (!FindComponent<SpawnPoint>(scene))
        {
            GameObject spawn = new GameObject("CORE_SpawnPoint_Javier");
            SceneManager.MoveGameObjectToScene(spawn, scene);
            spawn.transform.position = new Vector3(-9.5f, .8f, 0f);
            spawn.AddComponent<SpawnPoint>();
            EditorUtility.SetDirty(spawn);
        }
    }

    private static void UpdateBuildOrder()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        int index = scenes.FindIndex(item => item.path == JavierPath);
        if (index < 0) scenes.Insert(0, new EditorBuildSettingsScene(JavierPath, true));
        else if (index > 0)
        {
            EditorBuildSettingsScene javier = scenes[index];
            scenes.RemoveAt(index);
            scenes.Insert(0, javier);
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static AudioSource AddAudioSource(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    private static Button CreateButton(Transform parent, string name, string caption, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(.12f, .27f, .33f);
        Text text = CreateText(go.transform, "Texto", caption, 18, TextAnchor.MiddleCenter);
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Button button = go.GetComponent<Button>();
        return button;
    }

    private static Text CreateText(Transform parent, string name, string value, int size, TextAnchor alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = alignment;
        text.text = value;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = (anchorMin + anchorMax) * .5f;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static GameObject Child(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing) return existing.gameObject;
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject MakeProp(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
    {
        GameObject prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prop.name = name;
        prop.transform.SetParent(parent, true);
        prop.transform.position = position;
        prop.transform.localScale = scale;
        Object.DestroyImmediate(prop.GetComponent<Collider>());
        prop.GetComponent<Renderer>().sharedMaterial = MakeMaterial(color);
        return prop;
    }

    private static Material MakeMaterial(Color color)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        return material;
    }

    private static void Set(Object target, string property, string value) => SetProperty(target, property, serialized => serialized.stringValue = value);
    private static void Set(Object target, string property, float value) => SetProperty(target, property, serialized => serialized.floatValue = value);
    private static void Set(Object target, string property, int value) => SetProperty(target, property, serialized => serialized.intValue = value);
    private static void Set(Object target, string property, bool value) => SetProperty(target, property, serialized => serialized.boolValue = value);

    private static void SetProperty(Object target, string name, System.Action<SerializedProperty> apply)
    {
        if (!target) return;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(name);
        if (property == null) return;
        apply(property);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static T[] ReadObjectArray<T>(SerializedProperty array) where T : Object
    {
        T[] values = new T[array.arraySize];
        for (int i = 0; i < values.Length; i++) values[i] = array.GetArrayElementAtIndex(i).objectReferenceValue as T;
        return values;
    }

    private static T FindComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found) return found;
        }
        return null;
    }

    private static T FindNamedComponent<T>(Scene scene, string name) where T : Component
    {
        GameObject target = FindNamed(scene, name);
        return target ? target.GetComponent<T>() : null;
    }

    private static GameObject FindNamed(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindChild(root.transform, name);
            if (found) return found.gameObject;
        }
        return null;
    }

    private static Transform FindChild(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChild(child, name);
            if (result) return result;
        }
        return null;
    }
}
