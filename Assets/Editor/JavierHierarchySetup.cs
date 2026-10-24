using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Authors missing, editable Javier scene objects in the Editor, never during Play.</summary>
[InitializeOnLoad]
internal static class JavierHierarchySetup
{
    private const string ScenePath = "Assets/Scenes/Javier.unity";
    private const string NotebookName = "Canvas_Cuaderno";
    private static bool running;

    static JavierHierarchySetup()
    {
        EditorApplication.delayCall += EnsureJavierSceneObjects;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += EnsureWhenEditorIsReady;
    }

    private static void EnsureWhenEditorIsReady()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= EnsureWhenEditorIsReady;
        EnsureJavierSceneObjects();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += EnsureJavierSceneObjects;
    }

    [MenuItem("Jam3/Prepare Javier Editable Objects")]
    private static void PrepareFromMenu() => EnsureJavierSceneObjects();

    private static void EnsureJavierSceneObjects()
    {
        if (running || EditorApplication.isPlayingOrWillChangePlaymode || !System.IO.File.Exists(ScenePath)) return;
        running = true;
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool wasLoaded = scene.IsValid() && scene.isLoaded;
        try
        {
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            bool changed = EnsureFloors(scene);
            changed |= EnsureBall(scene);
            changed |= EnsureMovementAudio(scene);
            changed |= MoveInteractionTextToTop(scene);
            if (FindNotebookRoot(scene) == null)
            {
                BuildNotebook(scene);
                changed = true;
            }
            changed |= EnsureNotebookStampsHierarchy(scene);
            changed |= EnsureEventSystem(scene);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("Javier: pisos, pelota, audio de movimiento y Cuaderno quedaron guardados en la jerarquía.");
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            if (!wasLoaded && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
            running = false;
        }
    }

    private static bool EnsureFloors(Scene scene)
    {
        bool changed = false;
        foreach (string name in new[] { "LAB_Piso_Laboratorio", "LAB_Piso_Pasillo" })
        {
            GameObject floor = FindObject(scene, name);
            if (floor && !floor.GetComponent<Collider>())
            {
                floor.AddComponent<BoxCollider>();
                changed = true;
            }
        }
        return changed;
    }

    private static bool EnsureBall(Scene scene)
    {
        GameObject ball = FindObject(scene, "LAB_Pelota");
        if (!ball) return false;
        bool changed = false;

        var pickupCollider = ball.GetComponent<BoxCollider>();
        if (!pickupCollider)
        {
            pickupCollider = ball.AddComponent<BoxCollider>();
            pickupCollider.isTrigger = true;
            changed = true;
        }
        else if (!pickupCollider.isTrigger)
        {
            pickupCollider.isTrigger = true;
            changed = true;
        }

        var physicalCollider = ball.GetComponent<SphereCollider>();
        if (!physicalCollider)
        {
            physicalCollider = ball.AddComponent<SphereCollider>();
            physicalCollider.radius = .5f;
            physicalCollider.isTrigger = false;
            changed = true;
        }

        var body = ball.GetComponent<Rigidbody>();
        if (!body)
        {
            body = ball.AddComponent<Rigidbody>();
            changed = true;
        }
        body.mass = .35f;
        body.useGravity = true;
        body.constraints = RigidbodyConstraints.FreezePositionZ |
                           RigidbodyConstraints.FreezeRotationX |
                           RigidbodyConstraints.FreezeRotationY;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (!ball.GetComponent<CORE_RollingBall>())
        {
            ball.AddComponent<CORE_RollingBall>();
            changed = true;
        }
        return changed;
    }

    private static bool EnsureMovementAudio(Scene scene)
    {
        GameObject player = FindObject(scene, "CORE_Siete");
        if (!player) return false;
        var controller = player.GetComponent<CORE_PlayerController>();
        if (!controller) return false;

        Transform audioTransform = player.transform.Find("Audio_Movimiento");
        bool changed = false;
        if (!audioTransform)
        {
            var audioObject = new GameObject("Audio_Movimiento");
            audioObject.transform.SetParent(player.transform, false);
            audioTransform = audioObject.transform;
            changed = true;
        }
        var source = audioTransform.GetComponent<AudioSource>();
        if (!source)
        {
            source = audioTransform.gameObject.AddComponent<AudioSource>();
            changed = true;
        }

        AudioClip movementClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sonidos/robotPasos.mp3");
        source.clip = null;
        source.playOnAwake = false;
        source.loop = true;
        source.volume = .45f;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.minDistance = 2f;
        source.maxDistance = 25f;

        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("movimientoRobot").objectReferenceValue = movementClip;
        serializedController.FindProperty("movimientoSource").objectReferenceValue = source;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(source);
        EditorUtility.SetDirty(controller);
        return changed;
    }

    private static bool MoveInteractionTextToTop(Scene scene)
    {
        GameObject hud = FindObject(scene, "HUD_Panel_Interaccion");
        if (!hud || !(hud.transform is RectTransform rect)) return false;
        bool changed = rect.anchorMin != new Vector2(.5f, 1f) ||
                       rect.anchorMax != new Vector2(.5f, 1f) ||
                       rect.anchoredPosition != new Vector2(0f, -42f) ||
                       rect.pivot != new Vector2(.5f, 1f);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -42f);
        rect.sizeDelta = new Vector2(820f, 132f);
        return changed;
    }

    private static void BuildNotebook(Scene scene)
    {
        var root = new GameObject(NotebookName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(NotebookController));
        SceneManager.MoveGameObjectToScene(root, scene);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;

        var panel = MakeRect("Panel_Cuaderno", root.transform, new Vector2(1120f, 720f), Vector2.zero, new Color(.035f, .075f, .10f, .99f));
        Text heading = MakeText("Titulo", panel.transform, new Vector2(960f, 56f), new Vector2(-25f, 308f), "CUADERNO · Notas recogidas", 26, TextAnchor.MiddleCenter, Color.white);
        MakeText("Ayuda", panel.transform, new Vector2(1000f, 42f), new Vector2(0f, -320f), "Clic en una nota para abrirla · Clic otra vez para leer · Tab / Esc para cerrar", 18, TextAnchor.MiddleCenter, Color.white);
        Button close = MakeButton("Cerrar", panel.transform, new Vector2(100f, 44f), new Vector2(490f, 310f), "Cerrar");

        var pageArea = MakeRect("AreaLectura", panel.transform, new Vector2(730f, 540f), new Vector2(165f, -5f), new Color(.90f, .87f, .75f, 1f));
        Text preview = MakeText("PaginaCerrada", pageArea.transform, new Vector2(650f, 460f), Vector2.zero,
            "Selecciona una nota del índice.\n\nSolo puedes consultar las notas que hayas recogido.", 24, TextAnchor.MiddleCenter, new Color(.08f, .10f, .11f));

        var indexPanel = MakeRect("Indice_Notas", panel.transform, new Vector2(330f, 540f), new Vector2(-375f, -5f), new Color(.055f, .10f, .13f, .8f));
        GameObject[] pages = new GameObject[NotebookController.NoteCount];
        Button[] buttons = new Button[NotebookController.NoteCount];
        for (int i = 0; i < NotebookController.NoteCount; i++)
        {
            int index = i;
            buttons[i] = MakeButton($"Boton_Nota_{i + 1:00}", indexPanel.transform, new Vector2(300f, 44f),
                new Vector2(0f, 240f - i * 51f), $"Nota {i + 1} · No recogida");
            pages[i] = MakeRect($"Pagina_Nota_{i + 1:00}", pageArea.transform, new Vector2(730f, 540f), Vector2.zero, new Color(.90f, .87f, .75f, 1f));
            MakeText("Contenido", pages[i].transform, new Vector2(650f, 460f), Vector2.zero,
                $"NOTA {i + 1}\n\nContenido pendiente.", 25, TextAnchor.UpperLeft, new Color(.08f, .10f, .11f));
            pages[i].SetActive(false);
        }

        var open = MakeButton("Abrir_Cuaderno", root.transform, new Vector2(210f, 44f), Vector2.zero, "Cuaderno [Tab]");
        RectTransform openRect = open.GetComponent<RectTransform>();
        openRect.anchorMin = openRect.anchorMax = openRect.pivot = Vector2.one;
        openRect.anchoredPosition = new Vector2(-24f, -24f);

        Button stampsButton = null;
        GameObject stampsPanel = null;
        Button backToNotes = null;
        TMP_Text[] stampSlots = new TMP_Text[3];
        GameObject stampNotification = null;
        TMP_Text stampNotificationText = null;
        var stampsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI_Sellos/UI_Sellos.prefab");
        if (stampsPrefab)
        {
            var stampsRoot = (GameObject)PrefabUtility.InstantiatePrefab(stampsPrefab, scene);
            PrefabUtility.UnpackPrefabInstance(stampsRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            stampsRoot.name = "UI_Sellos";
            stampsRoot.transform.SetParent(panel.transform, false);
            Stretch(stampsRoot.GetComponent<RectTransform>());
            Transform stampsPanelTransform = stampsRoot.transform.Find("Panel_Sellos");
            Transform stampsButtonTransform = stampsRoot.transform.Find("Sellos");
            Transform backTransform = stampsPanelTransform ? stampsPanelTransform.Find("Volver_Notas") : null;
            string[] slotNames = { "Espacio_Laboratorio", "Espacio_Taller", "Espacio_Bodega" };
            Transform[] slotTransforms = new Transform[slotNames.Length];
            for (int i = 0; i < slotNames.Length; i++)
                if (stampsPanelTransform) slotTransforms[i] = stampsPanelTransform.Find(slotNames[i]);
            if (stampsPanelTransform)
            {
                stampsPanelTransform.SetParent(pageArea.transform, false);
                Stretch(stampsPanelTransform.GetComponent<RectTransform>());
                stampsPanel = stampsPanelTransform.gameObject;
                stampsPanel.SetActive(false);
            }
            if (stampsButtonTransform) stampsButton = stampsButtonTransform.GetComponent<Button>();
            if (backTransform) backToNotes = backTransform.GetComponent<Button>();
            for (int i = 0; i < slotNames.Length; i++)
            {
                Transform slot = slotTransforms[i];
                if (slot) stampSlots[i] = slot.GetComponent<TMP_Text>();
            }
            Transform notification = stampsRoot.transform.Find("Notificacion_Sello");
            if (notification) stampNotification = notification.gameObject;
            Transform notificationText = stampsRoot.transform.Find("Notificacion_Sello/Mensaje");
            if (notificationText) stampNotificationText = notificationText.GetComponent<TMP_Text>();
        }

        var controller = root.GetComponent<NotebookController>();
        controller.ConfigureForScene(pages, buttons);
        controller.ConfigureSceneUi(panel, pageArea.GetComponent<RectTransform>(), open, close, preview, heading);
        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("stampsButton").objectReferenceValue = stampsButton;
        serializedController.FindProperty("stampsPanel").objectReferenceValue = stampsPanel;
        SerializedProperty serializedSlots = serializedController.FindProperty("stampSlots");
        serializedSlots.arraySize = stampSlots.Length;
        for (int i = 0; i < stampSlots.Length; i++) serializedSlots.GetArrayElementAtIndex(i).objectReferenceValue = stampSlots[i];
        serializedController.FindProperty("backToNotesButton").objectReferenceValue = backToNotes;
        serializedController.FindProperty("stampNotification").objectReferenceValue = stampNotification;
        serializedController.FindProperty("stampNotificationText").objectReferenceValue = stampNotificationText;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        panel.SetActive(false);
    }

    private static bool EnsureNotebookStampsHierarchy(Scene scene)
    {
        GameObject notebook = FindNotebookRoot(scene);
        if (!notebook) return false;

        Transform stampsRoot = FindChild(notebook.transform, "UI_Sellos");
        Transform stampsPanel = stampsRoot ? stampsRoot.Find("Panel_Sellos") : null;
        Transform pageArea = FindChild(notebook.transform, "AreaLectura");
        if (!stampsRoot || !stampsPanel || !pageArea) return false;

        bool changed = false;
        if (PrefabUtility.IsPartOfPrefabInstance(stampsRoot.gameObject))
        {
            PrefabUtility.UnpackPrefabInstance(stampsRoot.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            changed = true;
            stampsRoot = FindChild(notebook.transform, "UI_Sellos");
            stampsPanel = stampsRoot ? stampsRoot.Find("Panel_Sellos") : null;
        }

        if (stampsPanel && stampsPanel.parent != pageArea)
        {
            stampsPanel.SetParent(pageArea, false);
            Stretch(stampsPanel.GetComponent<RectTransform>());
            changed = true;
        }
        return changed;
    }

    private static bool EnsureEventSystem(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.GetComponentInChildren<EventSystem>(true)) return false;
        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        SceneManager.MoveGameObjectToScene(eventSystem, scene);
        GameObject notebook = FindNotebookRoot(scene);
        if (notebook) eventSystem.transform.SetParent(notebook.transform, false);
        return true;
    }

    private static GameObject MakeRect(string name, Transform parent, Vector2 size, Vector2 position, Color color)
    {
        var result = new GameObject(name, typeof(RectTransform), typeof(Image));
        result.transform.SetParent(parent, false);
        var rect = (RectTransform)result.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        var image = result.GetComponent<Image>();
        image.color = color;
        return result;
    }

    private static Text MakeText(string name, Transform parent, Vector2 size, Vector2 position, string value, int fontSize, TextAnchor alignment, Color color)
    {
        var result = new GameObject(name, typeof(RectTransform), typeof(Text));
        result.transform.SetParent(parent, false);
        var rect = (RectTransform)result.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        var text = result.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.text = value;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static Button MakeButton(string name, Transform parent, Vector2 size, Vector2 position, string caption)
    {
        var result = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        result.transform.SetParent(parent, false);
        var rect = (RectTransform)result.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        var image = result.GetComponent<Image>();
        image.color = new Color(.12f, .27f, .33f, 1f);
        var button = result.GetComponent<Button>();
        button.targetGraphic = image;
        Text label = MakeText("Texto", result.transform, size, Vector2.zero, caption, 18, TextAnchor.MiddleCenter, Color.white);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, .5f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == name) return root;
        return null;
    }

    private static GameObject FindNotebookRoot(Scene scene)
    {
        GameObject notebook = FindRoot(scene, NotebookName);
        return notebook ? notebook : FindRoot(scene, NotebookName + "(Clone)");
    }

    private static GameObject FindObject(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform result = FindChild(root.transform, name);
            if (result) return result.gameObject;
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
