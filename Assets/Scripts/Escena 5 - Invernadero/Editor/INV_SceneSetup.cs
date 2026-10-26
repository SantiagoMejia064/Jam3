using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Configurador del nivel Invernadero (Persona 5) para la Game Jam.</summary>
public static class INV_SceneSetup
{
    private const string ScenePath = "Assets/Scenes/Invernadero.unity";
    public const float InvernaderoZ = 0f;

    [MenuItem("Jam3/Invernadero/Configurar Escena Completa")]
    public static void ConfigureScene()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError("No se pudo abrir " + ScenePath);
            return;
        }

        Debug.Log("Iniciando configuración de Invernadero sin alterar el escenario existente...");

        // 1. Contenedor principal de jugabilidad
        GameObject gameplayRoot = GameObject.Find("INV_Gameplay");
        if (gameplayRoot != null) Object.DestroyImmediate(gameplayRoot);
        gameplayRoot = new GameObject("INV_Gameplay");

        // 2. SpawnPoint de entrada desde Javier
        GameObject spawnGo = new GameObject("INV_SpawnPoint");
        spawnGo.transform.SetParent(gameplayRoot.transform, false);
        spawnGo.transform.position = new Vector3(11.4f, 0.8f, InvernaderoZ);
        var spawnPoint = spawnGo.AddComponent<SpawnPoint>();
        // Campo vacío permite tanto entrada desde Javier como fallback de pruebas
        SetSerializedField(spawnPoint, "sceneName", "");
        SetSerializedField(spawnPoint, "entryFromScene", "");

        // 3. Puerta de regreso al Pasillo (Javier)
        GameObject doorGo = new GameObject("INV_Puerta_Pasillo");
        doorGo.transform.SetParent(gameplayRoot.transform, false);
        doorGo.transform.position = new Vector3(-14.3f, 1.3f, InvernaderoZ);
        var doorCol = doorGo.AddComponent<BoxCollider>();
        doorCol.isTrigger = true;
        doorCol.size = new Vector3(1.2f, 2.6f, 3.0f);
        var zoneDoor = doorGo.AddComponent<CORE_ZoneDoor>();
        SetSerializedField(zoneDoor, "destinationScene", "Javier");
        SetSerializedField(zoneDoor, "prompt", "E · Salir al Pasillo");
        SetSerializedField(zoneDoor, "openedFlag", "");

        // 4. Ubicar Mesa de Trabajo para N8
        GameObject mesaTablero = GameObject.Find("Mesa_Tablero");
        Vector3 mesaPos = mesaTablero ? mesaTablero.transform.position : new Vector3(-10.2f, 1.6f, InvernaderoZ);
        mesaPos.z = InvernaderoZ;

        GameObject workTableGo = new GameObject("INV_MesaTrabajo_Interactable");
        workTableGo.transform.SetParent(gameplayRoot.transform, false);
        workTableGo.transform.position = mesaPos;
        var tableCol = workTableGo.AddComponent<BoxCollider>();
        tableCol.isTrigger = true;
        tableCol.size = new Vector3(2.6f, 2.0f, 3.0f);
        var workTable = workTableGo.AddComponent<INV_WorkTable>();

        // Hoja visual de la nota N8 en la mesa
        GameObject sheetGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sheetGo.name = "INV_Hoja_N8_Visual";
        sheetGo.transform.SetParent(workTableGo.transform, false);
        sheetGo.transform.localPosition = new Vector3(0.2f, 0.45f, 0.1f);
        sheetGo.transform.localRotation = Quaternion.Euler(0, 15f, 0);
        sheetGo.transform.localScale = new Vector3(0.35f, 0.015f, 0.28f);
        Object.DestroyImmediate(sheetGo.GetComponent<Collider>());
        SetSerializedField(workTable, "noteVisualSheet", sheetGo);

        // 5. Restos de Ella (Maceta quemada)
        GameObject macetaMesa = GameObject.Find("Maceta_Mesa");
        Vector3 macetaPos = macetaMesa ? macetaMesa.transform.position : new Vector3(-7.5f, 1.6f, InvernaderoZ);
        macetaPos.z = InvernaderoZ;

        GameObject restosGo = new GameObject("INV_RestosElla_Interactable");
        restosGo.transform.SetParent(gameplayRoot.transform, false);
        restosGo.transform.position = macetaPos;
        var restosCol = restosGo.AddComponent<BoxCollider>();
        restosCol.isTrigger = true;
        restosCol.size = new Vector3(1.6f, 1.6f, 3.0f);
        restosGo.AddComponent<INV_RestosElla>();

        // 6. Ventanal al jardín exterior
        GameObject ventanalGo = new GameObject("INV_Ventanal_Interactable");
        ventanalGo.transform.SetParent(gameplayRoot.transform, false);
        ventanalGo.transform.position = new Vector3(-2.0f, 2.0f, InvernaderoZ);
        var ventanalCol = ventanalGo.AddComponent<BoxCollider>();
        ventanalCol.isTrigger = true;
        ventanalCol.size = new Vector3(6.5f, 3.5f, 3.5f);
        ventanalGo.AddComponent<INV_Ventanal>();

        // 7. Incubadora / Cámara de Cultivo
        GameObject camaraCultivo = GameObject.Find("Camara_Cultivo_Base");
        Vector3 incubadoraPos = camaraCultivo ? camaraCultivo.transform.position : new Vector3(-11.4f, 1.8f, InvernaderoZ);
        incubadoraPos.z = InvernaderoZ;

        GameObject incubatorGo = new GameObject("INV_Incubadora_Interactable");
        incubatorGo.transform.SetParent(gameplayRoot.transform, false);
        incubatorGo.transform.position = incubadoraPos;
        var incCol = incubatorGo.AddComponent<BoxCollider>();
        incCol.isTrigger = true;
        incCol.size = new Vector3(2.4f, 2.6f, 3.0f);
        var incubatorPuzzle = incubatorGo.AddComponent<INV_IncubatorPuzzle>();

        // Visual de Ella brotando
        GameObject sproutGo = new GameObject("INV_EllaSproutVisual");
        sproutGo.transform.SetParent(incubatorGo.transform, false);
        sproutGo.transform.localPosition = new Vector3(0, 0.85f, 0);
        var spriteRenderer = sproutGo.AddComponent<SpriteRenderer>();
        var ellaSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SAL/Art/Dibujos/Ella_N6.png");
        if (ellaSprite) spriteRenderer.sprite = ellaSprite;
        sproutGo.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        sproutGo.SetActive(false);
        SetSerializedField(incubatorPuzzle, "ellaSproutVisual", sproutGo);

        // Luz violeta de floración
        GameObject violetLightGo = new GameObject("INV_LuzCamaraVioleta");
        violetLightGo.transform.SetParent(incubatorGo.transform, false);
        violetLightGo.transform.localPosition = new Vector3(0, 1.2f, -0.5f);
        var vLight = violetLightGo.AddComponent<Light>();
        vLight.type = LightType.Point;
        vLight.color = new Color(0.72f, 0.35f, 0.98f);
        vLight.range = 7f;
        vLight.intensity = 4f;
        vLight.enabled = false;
        SetSerializedField(incubatorPuzzle, "violetChamberLight", vLight);

        // Luz roja de alarma
        GameObject redLightGo = new GameObject("INV_LuzAlarmaRoja");
        redLightGo.transform.SetParent(incubatorGo.transform, false);
        redLightGo.transform.localPosition = new Vector3(0, 2.5f, -0.8f);
        var rLight = redLightGo.AddComponent<Light>();
        rLight.type = LightType.Point;
        rLight.color = new Color(1f, 0.15f, 0.15f);
        rLight.range = 18f;
        rLight.intensity = 5.5f;
        rLight.enabled = false;
        SetSerializedField(incubatorPuzzle, "redAlarmLight", rLight);

        // Carrito visual
        GameObject cartVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cartVisual.name = "INV_CarritoVisual";
        cartVisual.transform.SetParent(incubatorGo.transform, false);
        cartVisual.transform.localPosition = new Vector3(0, -0.3f, 0);
        cartVisual.transform.localScale = new Vector3(1.4f, 0.25f, 1.4f);
        Object.DestroyImmediate(cartVisual.GetComponent<Collider>());
        cartVisual.SetActive(false);
        SetSerializedField(incubatorPuzzle, "incubatorCartVisual", cartVisual);

        // 8. Crear Canvases de UI
        GameObject canvasGo = GameObject.Find("INV_Canvas_Incubadora");
        if (canvasGo != null) Object.DestroyImmediate(canvasGo);
        canvasGo = new GameObject("INV_Canvas_Incubadora");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 25;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Panel de afinación
        var incubatorUI = CreateIncubatorUIPanel(canvasGo.transform);
        SetSerializedField(incubatorPuzzle, "incubatorUI", incubatorUI);

        // Panel de revelación
        var revelationUI = CreateRevelationUIPanel(canvasGo.transform);
        SetSerializedField(incubatorPuzzle, "revelationUI", revelationUI);

        // 9. Controlador de Sala
        GameObject controllerGo = GameObject.Find("INV_ControladorNivel");
        if (controllerGo != null) Object.DestroyImmediate(controllerGo);
        controllerGo = new GameObject("INV_ControladorNivel");
        var roomCtrl = controllerGo.AddComponent<INV_RoomController>();

        // Asignar plantas para desaturación
        GameObject plantasRoot = GameObject.Find("07_Plantas_Instancias_ROOT");
        GameObject enredaderasRoot = GameObject.Find("INV_Enredaderas");
        var plantsList = new System.Collections.Generic.List<GameObject>();
        if (plantasRoot) plantsList.Add(plantasRoot);
        if (enredaderasRoot) plantsList.Add(enredaderasRoot);
        SetSerializedField(roomCtrl, "greenPlantsToGray", plantsList.ToArray());

        // 10. Ajustar Cámara y Player local para pruebas
        var camGo = GameObject.Find("INV_Camara_25D");
        if (camGo)
        {
            camGo.SetActive(true);
            var follow = camGo.GetComponent<CORE_CameraFollow>();
            if (follow)
            {
                SetSerializedField(follow, "xLimits", new Vector2(-2f, 18f));
                SetSerializedField(follow, "offset", new Vector3(-3.7f, 5.2f, 24.7f));
                SetSerializedField(follow, "smooth", 6f);
            }
        }

        // Configurar player en la escena para modo directo
        GameObject playerGo = GameObject.Find("CORE_Siete");
        if (!playerGo) playerGo = GameObject.Find("CORE_Siete_SAL");
        if (playerGo)
        {
            playerGo.name = "CORE_Siete";
            playerGo.SetActive(true);
            playerGo.transform.position = new Vector3(11.4f, 0.8f, InvernaderoZ);
            var pc = playerGo.GetComponent<CORE_PlayerController>();
            if (pc) SetSerializedField(pc, "fixedZ", InvernaderoZ);
            if (!playerGo.GetComponent<PlayerPersistence>())
            {
                var pp = playerGo.AddComponent<PlayerPersistence>();
                SetSerializedField(pp, "playerObjectName", "CORE_Siete");
            }
        }

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("¡Escena Invernadero configurada y guardada con éxito!");
    }

    private static INV_IncubatorUI CreateIncubatorUIPanel(Transform parent)
    {
        GameObject panel = new GameObject("Panel_Afinacion");
        panel.transform.SetParent(parent, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.1f);
        panelRect.anchorMax = new Vector2(0.85f, 0.9f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var img = panel.AddComponent<Image>();
        img.color = new Color(0.06f, 0.08f, 0.12f, 0.94f);

        var incUI = panel.AddComponent<INV_IncubatorUI>();
        SetSerializedField(incUI, "panel", panel);

        // Título
        CreateText(panel.transform, "Titulo", "INCUBADORA IMPROVISADA · AFINACIÓN DE FRECUENCIA", 26,
            new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f), new Vector2(0, 0), new Vector2(900, 40),
            new Color(0.75f, 0.52f, 0.98f), TextAlignmentOptions.Center);

        CreateText(panel.transform, "Subtitulo", "Ajusta las 4 perillas según la planilla N8 para estabilizar la semilla", 17,
            new Vector2(0.5f, 0.86f), new Vector2(0.5f, 0.86f), new Vector2(0, 0), new Vector2(800, 30),
            new Color(0.65f, 0.65f, 0.72f), TextAlignmentOptions.Center);

        // 4 Perillas
        string[] names = { "CALOR (Llama)", "OSCURIDAD (Luna)", "PULSO (Huellas)", "BRUMA (Gota)" };
        float startX = -360f;
        float spacingX = 240f;

        TMP_Text[] valueTexts = new TMP_Text[4];
        Button[] plusBtns = new Button[4];
        Button[] minusBtns = new Button[4];
        Image[] rings = new Image[4];

        for (int i = 0; i < 4; i++)
        {
            float posX = startX + i * spacingX;

            GameObject col = new GameObject("Perilla_" + i);
            col.transform.SetParent(panel.transform, false);
            var colRect = col.AddComponent<RectTransform>();
            colRect.anchorMin = colRect.anchorMax = new Vector2(0.5f, 0.52f);
            colRect.anchoredPosition = new Vector2(posX, 20f);
            colRect.sizeDelta = new Vector2(200, 260);

            // Anillo visual
            GameObject ringGo = new GameObject("Ring_" + i);
            ringGo.transform.SetParent(col.transform, false);
            var ringRect = ringGo.AddComponent<RectTransform>();
            ringRect.anchoredPosition = new Vector2(0, 70f);
            ringRect.sizeDelta = new Vector2(70, 70);
            var ringImg = ringGo.AddComponent<Image>();
            ringImg.color = new Color(0.3f, 0.3f, 0.35f);
            rings[i] = ringImg;

            CreateText(col.transform, "Label_" + i, names[i], 18,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 15f), new Vector2(190, 30),
                new Color(0.85f, 0.85f, 0.9f), TextAlignmentOptions.Center);

            valueTexts[i] = CreateText(col.transform, "Val_" + i, "0", 38,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -35f), new Vector2(80, 50),
                Color.white, TextAlignmentOptions.Center);

            minusBtns[i] = CreateButton(col.transform, "Minus_" + i, "-", new Vector2(-50, -35), new Vector2(40, 40));
            plusBtns[i] = CreateButton(col.transform, "Plus_" + i, "+", new Vector2(50, -35), new Vector2(40, 40));
        }

        SetSerializedField(incUI, "calorValueText", valueTexts[0]);
        SetSerializedField(incUI, "oscuridadValueText", valueTexts[1]);
        SetSerializedField(incUI, "pulsoValueText", valueTexts[2]);
        SetSerializedField(incUI, "brumaValueText", valueTexts[3]);

        SetSerializedField(incUI, "calorMinusBtn", minusBtns[0]);
        SetSerializedField(incUI, "calorPlusBtn", plusBtns[0]);
        SetSerializedField(incUI, "oscuridadMinusBtn", minusBtns[1]);
        SetSerializedField(incUI, "oscuridadPlusBtn", plusBtns[1]);
        SetSerializedField(incUI, "pulsoMinusBtn", minusBtns[2]);
        SetSerializedField(incUI, "pulsoPlusBtn", plusBtns[2]);
        SetSerializedField(incUI, "brumaMinusBtn", minusBtns[3]);
        SetSerializedField(incUI, "brumaPlusBtn", plusBtns[3]);

        SetSerializedField(incUI, "ringCalor", rings[0]);
        SetSerializedField(incUI, "ringOscuridad", rings[1]);
        SetSerializedField(incUI, "ringPulso", rings[2]);
        SetSerializedField(incUI, "ringBruma", rings[3]);

        // Barra de estabilidad y estado
        var statusTxt = CreateText(panel.transform, "StatusText", "Ajusta las 4 perillas según la planilla N8.", 18,
            new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.18f), new Vector2(0, 0), new Vector2(700, 32),
            new Color(0.85f, 0.85f, 0.9f), TextAlignmentOptions.Center);
        SetSerializedField(incUI, "statusText", statusTxt);

        // Slider
        GameObject sliderGo = new GameObject("StabilitySlider");
        sliderGo.transform.SetParent(panel.transform, false);
        var sliderRect = sliderGo.AddComponent<RectTransform>();
        sliderRect.anchorMin = sliderRect.anchorMax = new Vector2(0.5f, 0.12f);
        sliderRect.anchoredPosition = Vector2.zero;
        sliderRect.sizeDelta = new Vector2(500, 16);
        var slider = sliderGo.AddComponent<Slider>();

        GameObject fillBg = new GameObject("Background");
        fillBg.transform.SetParent(sliderGo.transform, false);
        var bgRect = fillBg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
        var bgImg = fillBg.AddComponent<Image>();
        bgImg.color = new Color(0.18f, 0.18f, 0.22f);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGo.transform, false);
        var faRect = fillArea.AddComponent<RectTransform>();
        faRect.anchorMin = Vector2.zero; faRect.anchorMax = Vector2.one;
        faRect.offsetMin = faRect.offsetMax = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fRect = fill.AddComponent<RectTransform>();
        fRect.anchorMin = Vector2.zero; fRect.anchorMax = Vector2.one;
        fRect.offsetMin = fRect.offsetMax = Vector2.zero;
        var fImg = fill.AddComponent<Image>();
        fImg.color = new Color(0.72f, 0.35f, 0.98f);
        slider.fillRect = fRect;
        slider.value = 0f;
        SetSerializedField(incUI, "stabilitySlider", slider);

        // Botón cerrar
        var closeBtn = CreateButton(panel.transform, "CloseBtn", "Cerrar [Esc]", new Vector2(0, -280), new Vector2(160, 42));
        SetSerializedField(incUI, "closeButton", closeBtn);

        panel.SetActive(false);
        return incUI;
    }

    private static INV_RevelationUI CreateRevelationUIPanel(Transform parent)
    {
        GameObject panel = new GameObject("Panel_Revelacion");
        panel.transform.SetParent(parent, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

        var img = panel.AddComponent<Image>();
        img.color = new Color(0.02f, 0.02f, 0.05f, 0.97f);

        var revUI = panel.AddComponent<INV_RevelationUI>();
        SetSerializedField(revUI, "panel", panel);

        var numTxt = CreateText(panel.transform, "CardNumber", "EVIDENCIA 1 DE 6", 20,
            new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), Vector2.zero, new Vector2(800, 35),
            new Color(0.6f, 0.6f, 0.68f), TextAlignmentOptions.Center);
        SetSerializedField(revUI, "cardNumberText", numTxt);

        var vegaTxt = CreateText(panel.transform, "VegaVoice", "Vega: \"...\"", 28,
            new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(1100, 90),
            new Color(0.95f, 0.95f, 0.98f), TextAlignmentOptions.Center);
        SetSerializedField(revUI, "vegaVoiceText", vegaTxt);

        var read1Txt = CreateText(panel.transform, "Reading1", "Lectura 1...", 20,
            new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(1000, 50),
            new Color(0.5f, 0.5f, 0.55f), TextAlignmentOptions.Center);
        SetSerializedField(revUI, "reading1Text", read1Txt);

        var read2Txt = CreateText(panel.transform, "Reading2", "Lectura 2...", 24,
            new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(1100, 90),
            new Color(0.94f, 0.35f, 0.35f), TextAlignmentOptions.Center);
        SetSerializedField(revUI, "reading2Text", read2Txt);

        var advanceTxt = CreateText(panel.transform, "AdvanceHint", "Pulsa [E] o [Espacio] para continuar...", 18,
            new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(600, 30),
            new Color(0.7f, 0.7f, 0.75f), TextAlignmentOptions.Center);
        SetSerializedField(revUI, "hintAdvanceText", advanceTxt);

        panel.SetActive(false);
        return revUI;
    }

    private static TMP_Text CreateText(Transform parent, string name, string content, float size,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 delta, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        rect.anchoredPosition = pos; rect.sizeDelta = delta;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos; rect.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.28f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.45f);
        colors.pressedColor = new Color(0.5f, 0.3f, 0.7f);
        btn.colors = colors;

        CreateText(go.transform, "BtnText", label, 20, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            Color.white, TextAlignmentOptions.Center);

        return btn;
    }

    private static void SetSerializedField(Object target, string fieldName, object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            if (value is string s) prop.stringValue = s;
            else if (value is bool b) prop.boolValue = b;
            else if (value is int i) prop.intValue = i;
            else if (value is float f) prop.floatValue = f;
            else if (value is Vector2 v2) prop.vector2Value = v2;
            else if (value is Vector3 v3) prop.vector3Value = v3;
            else if (value is Object o) prop.objectReferenceValue = o;
            else if (value is GameObject[] arr)
            {
                prop.arraySize = arr.Length;
                for (int idx = 0; idx < arr.Length; idx++)
                    prop.GetArrayElementAtIndex(idx).objectReferenceValue = arr[idx];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
