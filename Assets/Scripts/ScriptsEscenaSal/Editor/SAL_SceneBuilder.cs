using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Creates an editable 2.5D SAL graybox inside the Persona 3 folder.</summary>
public static class SAL_SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/SAL.unity";
    private const string DataFolder = "Assets/SAL/Data";

    [MenuItem("Jam3/Build SAL Sala de Máquinas")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder("Assets/SAL")) AssetDatabase.CreateFolder("Assets", "SAL");
        if (!AssetDatabase.IsValidFolder(DataFolder)) AssetDatabase.CreateFolder("Assets/SAL", "Data");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildRoom();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuild(ScenePath);
        AddSceneToBuild("Assets/Scenes/Javier.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("SAL creada. Abre Assets/Scenes/SAL.unity y pulsa Play.");
    }

    // Entry point for Unity -batchmode -executeMethod.
    public static void BuildBatch() { Build(); }

    private static void BuildRoom()
    {
        Material wall = Mat("SAL_Material_Pared", new Color(.18f, .14f, .15f));
        Material dark = Mat("SAL_Material_Metal", new Color(.15f, .20f, .23f));
        Material copper = Mat("SAL_Material_Cobre", new Color(.52f, .24f, .13f));
        Material amber = Mat("SAL_Material_Ambar", new Color(.98f, .54f, .16f));
        Material cyan = Mat("SAL_Material_Cian", new Color(.18f, .75f, .77f));
        Material violet = Mat("SAL_Material_Violeta", new Color(.55f, .35f, .86f));
        Material green = Mat("SAL_Material_LED", new Color(.18f, .96f, .52f));

        GameObject environment = new GameObject("SAL_Entorno_3D");
        Box("SAL_Fondo", environment.transform, new Vector3(0, 2.2f, 3.6f), new Vector3(29, 7.3f, .55f), wall);
        Box("SAL_Piso", environment.transform, new Vector3(0, -.22f, .7f), new Vector3(29, .45f, 5), dark);
        Box("SAL_Zocalo", environment.transform, new Vector3(0, .1f, 2.45f), new Vector3(29, .22f, .55f), copper);
        Box("SAL_VigaSuperior", environment.transform, new Vector3(0, 5.3f, 2.4f), new Vector3(29, .4f, .6f), dark);
        for (int i = -12; i <= 12; i += 4)
        {
            Box("SAL_PanelFondo_" + i, environment.transform, new Vector3(i, 2.4f, 3.25f), new Vector3(3.8f, 4.6f, .12f), dark);
            Box("SAL_Ribete_" + i, environment.transform, new Vector3(i + 1.8f, 2.4f, 3.05f), new Vector3(.07f, 4.6f, .12f), copper);
        }
        Pipe("SAL_TuberiaAlta", environment.transform, new Vector3(0, 4.55f, 2.65f), 27f, .18f, copper);
        Pipe("SAL_TuberiaMedia", environment.transform, new Vector3(0, 3.65f, 3f), 27f, .13f, copper);
        for (int x = -11; x <= 11; x += 3)
        {
            Box("SAL_RejillaPiso_" + x, environment.transform, new Vector3(x, .025f, -.45f), new Vector3(1.1f, .035f, 1f), copper);
        }
        Box("SAL_LimiteIzquierdo", environment.transform, new Vector3(-14.1f, 1.6f, 0), new Vector3(.35f, 3.3f, 2f), dark).AddComponent<BoxCollider>();
        Box("SAL_LimiteDerecho", environment.transform, new Vector3(14.1f, 1.6f, 0), new Vector3(.35f, 3.3f, 2f), dark).AddComponent<BoxCollider>();

        GameObject player = new GameObject("CORE_Siete_SAL");
        player.transform.position = new Vector3(-12.1f, .8f, 0);
        var collider = player.AddComponent<CapsuleCollider>(); collider.radius = .42f; collider.height = 1.7f;
        var body = player.AddComponent<Rigidbody>(); body.useGravity = false;
        var controller = player.AddComponent<CORE_PlayerController>();
        BuildRobotSprite(player.transform);

        var cam = new GameObject("CORE_Camara_Perfil"); cam.tag = "MainCamera";
        cam.transform.position = new Vector3(-7.5f, 2.25f, -15f);
        cam.transform.rotation = Quaternion.Euler(7f, 0, 0);
        var camera = cam.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5.15f;
        camera.backgroundColor = new Color(.035f, .05f, .08f); camera.clearFlags = CameraClearFlags.SolidColor;
        cam.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        cam.AddComponent<AudioListener>();
        var follow = cam.AddComponent<CORE_CameraFollow>();
        Set(follow, "target", player.transform);
        Set(follow, "xLimits", new Vector2(-8.3f, 8.3f));
        Set(follow, "offset", new Vector3(0, 1.55f, -15f));

        var sunlight = new GameObject("SAL_LuzBase"); sunlight.transform.rotation = Quaternion.Euler(35f, -25f, 0);
        var directional = sunlight.AddComponent<Light>(); directional.type = LightType.Directional; directional.intensity = .45f; directional.color = new Color(.68f, .77f, 1f);
        Light key = PointLight("SAL_LuzNaranja", new Vector3(-1, 3, -2), new Color(1f, .53f, .25f), 17f, 4.2f);
        PointLight("SAL_LuzPanel", new Vector3(6.5f, 2.8f, -1), new Color(.96f, .56f, .22f), 8f, 2.1f);
        GameObject volume = new GameObject("SAL_ColorPorActo");
        var atmosphere = volume.AddComponent<SAL_Atmosphere>(); Set(atmosphere, "roomLight", key);

        GameObject roomRoot = new GameObject("SAL_Puzles");
        var room = roomRoot.AddComponent<SAL_RoomController>();
        GameObject driverObject = new GameObject("SAL_Interaccion");
        var driver = driverObject.AddComponent<SAL_InteractionDriver>(); Set(driver, "player", controller); Set(driver, "room", room);
        var uiObject = new GameObject("SAL_HUD_CuadernoTemporal");
        var ui = uiObject.AddComponent<SAL_UI>(); Set(ui, "room", room); Set(ui, "interaction", driver);

        // Exit and discarded drawings: the visual discovery happens before the signature puzzle.
        GameObject door = Box("SAL_PuertaPasillo", roomRoot.transform, new Vector3(-12.5f, 1.15f, 1.2f), new Vector3(1.25f, 2.3f, .3f), dark);
        Box("SAL_LuzPuerta", door.transform, new Vector3(-12.5f, 2.25f, .92f), new Vector3(1.1f, .08f, .1f), cyan);
        Label("SAL_RotuloSalida", roomRoot.transform, new Vector3(-12.5f, 3.05f, .7f), "PASILLO", cyan.color);
        Action(roomRoot.transform, "SAL_AccionSalir", -12.5f, SAL_Interactable.Kind.Exit, "Volver al Pasillo");

        Box("SAL_Basurero", roomRoot.transform, new Vector3(-9.8f, .64f, .95f), new Vector3(1.3f, 1.1f, .85f), dark);
        GameObject dust = Box("SAL_PolvoBasurero", roomRoot.transform, new Vector3(-9.8f, 1.23f, .64f), new Vector3(1.35f, .16f, .9f), amber);
        Set(room, "dust", dust);
        var trashAction = Action(roomRoot.transform, "SAL_AccionBasurero_N6", -9.8f, SAL_Interactable.Kind.Trash, "Limpiar y rebuscar en los dibujos");
        trashAction.holdToClean = true;
        GameObject drawing = new GameObject("SAL_DibujosElla_N6"); drawing.transform.SetParent(roomRoot.transform);
        Box("SAL_PapelDibujo_1", drawing.transform, new Vector3(-9.2f, 2.1f, 3.02f), new Vector3(1.6f, 1.85f, .07f), Mat("SAL_Material_Papel", new Color(.8f, .75f, .62f)));
        Box("SAL_PapelDibujo_3", drawing.transform, new Vector3(-7.8f, 2.1f, 3.01f), new Vector3(1.6f, 1.85f, .07f), Mat("SAL_Material_Papel", new Color(.8f, .75f, .62f)));
        Box("SAL_PapelDibujos", drawing.transform, new Vector3(-8.5f, 2.15f, 2.95f), new Vector3(1.9f, 2.1f, .09f), Mat("SAL_Material_Papel", new Color(.8f, .75f, .62f)));
        BuildFlowerDrawing(drawing.transform, violet, cyan);
        Label("SAL_Dibujo_1_Anotacion", drawing.transform, new Vector3(-9.3f, 1.5f, 2.86f), "ELLA", dark.color);
        Label("SAL_Dibujo_3_Anotacion", drawing.transform, new Vector3(-7.65f, 1.52f, 2.86f), "?", dark.color);
        Label("SAL_FirmaDibujo", drawing.transform, new Vector3(-8.05f, 1.36f, 2.76f), "||·", dark.color);
        Action(drawing.transform, "SAL_AccionMirarDibujo", -8.5f, SAL_Interactable.Kind.Drawings, "Mirar los dibujos");
        drawing.SetActive(false); Set(room, "drawings", drawing);

        // SAL's local round marker. The other seals are owned by LAB and BOD.
        GameObject cover = Box("SAL_PolvoSelloRombo", roomRoot.transform, new Vector3(-6.3f, .18f, -.15f), new Vector3(1f, .09f, .65f), copper);
        GameObject seal = Label("SAL_SelloRombo", roomRoot.transform, new Vector3(-6.3f, .55f, -.25f), "◆", cyan.color);
        seal.SetActive(false); Set(room, "sealCover", cover); Set(room, "diamondSeal", seal);
        var sealAction = Action(roomRoot.transform, "SAL_AccionSelloRombo", -6.3f, SAL_Interactable.Kind.DiamondSeal, "Limpiar el punto de ronda ◆");
        sealAction.holdToClean = true;

        // Duct 14: yellow note in front, original warning underneath, protected green LED.
        GameObject duct = new GameObject("SAL_Ducto14"); duct.transform.SetParent(roomRoot.transform);
        Box("SAL_CuerpoDucto", duct.transform, new Vector3(-1.75f, 2.25f, 2.35f), new Vector3(5.2f, 2.6f, 1.1f), dark);
        Pipe("SAL_DuctoEntrada", duct.transform, new Vector3(-5.5f, 3.3f, 2.5f), 5f, .34f, copper);
        GameObject grille = Box("SAL_Rejilla14", duct.transform, new Vector3(-.45f, 2.15f, 1.7f), new Vector3(1.7f, 1.5f, .18f), copper);
        for (int i = 0; i < 5; i++) Box("SAL_Lama_" + i, grille.transform, new Vector3(-1.05f + i * .3f, 2.15f, 1.58f), new Vector3(.07f, 1.2f, .06f), dark);
        Set(room, "grille", grille);
        GameObject filter = Box("SAL_Filtro14_PiezaBruma", duct.transform, new Vector3(-.45f, 2.13f, 1.85f), new Vector3(1.25f, 1.2f, .15f), cyan);
        Set(room, "filter", filter);
        Box("SAL_Valvula14", duct.transform, new Vector3(-3.2f, 1.95f, 1.5f), new Vector3(.65f, .65f, .45f), amber);
        Label("SAL_EtiquetaAmarilla_N7", duct.transform, new Vector3(-3.55f, 3.2f, 1.48f), "FILTRO 14: CAMBIAR", amber.color);
        GameObject warning = Label("SAL_EtiquetaOriginal", duct.transform, new Vector3(-3.55f, 3.05f, 1.55f), "BARRERA BIOLOGICA · NO RETIRAR", new Color(.65f, .65f, .65f));
        warning.SetActive(false); Set(room, "originalWarning", warning);
        GameObject led = Box("SAL_LED_Verde", duct.transform, new Vector3(-.42f, 3.48f, 1.53f), new Vector3(.33f, .16f, .16f), green);
        Light ledLight = PointLight("SAL_LED_Luz", new Vector3(-.42f, 3.47f, 1.2f), green.color, 2f, 2f);
        Set(room, "led", led); Set(room, "ledLight", ledLight);
        Action(duct.transform, "SAL_AccionEtiqueta_N7", -4f, SAL_Interactable.Kind.FilterLabel, "Leer etiqueta del Filtro 14");
        Action(duct.transform, "SAL_AccionValvula", -3.2f, SAL_Interactable.Kind.Valve, "Usar Pelota en la válvula");
        Action(duct.transform, "SAL_AccionExtraerFiltro", -.45f, SAL_Interactable.Kind.Filter, "Extraer el Filtro 14");

        // World-space panel with three separate symbol buttons and six marked compacted blocks.
        GameObject recycler = new GameObject("SAL_Reciclaje"); recycler.transform.SetParent(roomRoot.transform);
        Box("SAL_Compactador", recycler.transform, new Vector3(9.5f, 2.2f, 2.25f), new Vector3(8.8f, 3.7f, 1.45f), dark);
        Box("SAL_Cinta", recycler.transform, new Vector3(9.55f, .9f, .8f), new Vector3(8.4f, .22f, 1.15f), copper);
        Label("SAL_PanelTitulo", recycler.transform, new Vector3(5.8f, 4.2f, 1.25f), "RECICLAJE · ORDEN DE RONDA", amber.color);
        char[] symbols = { '▲', '◆', '✚' };
        SAL_Interactable.Kind[] kinds = { SAL_Interactable.Kind.PanelTriangle, SAL_Interactable.Kind.PanelDiamond, SAL_Interactable.Kind.PanelCross };
        for (int i = 0; i < 3; i++)
        {
            float x = 5.35f + i * 1.2f;
            Box("SAL_Boton_" + symbols[i], recycler.transform, new Vector3(x, 2.2f, 1.4f), new Vector3(.75f, .8f, .27f), amber);
            Label("SAL_Simbolo_" + symbols[i], recycler.transform, new Vector3(x, 2.25f, 1.18f), symbols[i].ToString(), dark.color);
            Action(recycler.transform, "SAL_AccionBoton_" + symbols[i], x, kinds[i], "Pulsar " + symbols[i]);
        }
        GameObject[] blocks = new GameObject[6];
        string[] marks = { "I·", "·II", "III", "||·", "·I·", "II|" };
        for (int i = 0; i < 6; i++)
        {
            float x = 7.45f + i * 1.02f;
            GameObject block = new GameObject("SAL_Bloque_" + (i + 1) + "_" + marks[i]); block.transform.SetParent(recycler.transform);
            Box("SAL_BloqueVisual", block.transform, new Vector3(x, 1.46f, .65f), new Vector3(.82f, .7f, .75f), i == 3 ? copper : wall);
            Label("SAL_MarcaBloque", block.transform, new Vector3(x, 1.52f, .20f), marks[i], amber.color);
            var action = Action(block.transform, "SAL_AccionBloque", x, SAL_Interactable.Kind.Block, "Examinar bloque " + marks[i]);
            action.blockIndex = i;
            block.SetActive(false); blocks[i] = block;
        }
        SetArray(room, "blocks", blocks);

        // Dust/vapor is placed deep in 3D so the gameplay silhouettes remain unobstructed.
        for (int i = 0; i < 4; i++)
        {
            GameObject vapor = GameObject.CreatePrimitive(PrimitiveType.Sphere); vapor.name = "SAL_VaporFondo_" + i;
            vapor.transform.SetParent(environment.transform); vapor.transform.position = new Vector3(-5f + i * 5.8f, 3.1f + (i % 2) * .45f, 3.0f);
            vapor.transform.localScale = new Vector3(1.2f, .7f, .12f);
            Object.DestroyImmediate(vapor.GetComponent<Collider>());
            vapor.GetComponent<Renderer>().sharedMaterial = Mat("SAL_Material_Vapor_" + i, new Color(.35f, .34f, .30f));
        }

        SAL_NoteData[] noteAssets =
        {
            Note("N5", "▲  ◆  ✚", ""),
            Note("N6", "Los dibujos de Ella. Firma: ||·.", "Las otras plantas ya estaban grises."),
            Note("N7", "Filtro 14: cambiar.", "Filtro 14: NO retirar. Es la barrera."),
            Note("N9", "Aquí. No la sueltes.", "Ella no se parece a nada de aquí.")
        };
        SetArray(room, "notes", noteAssets);

        GameObject audioObject = new GameObject("CORE_AudioDirector_SAL");
        var director = audioObject.AddComponent<SAL_AudioDirector>();
        Set(director, "voiceSource", Source(audioObject, .9f));
        Set(director, "ambienceSource", Source(audioObject, .18f));
        Set(director, "musicSource", Source(audioObject, .26f));
        Set(director, "stringsSource", Source(audioObject, .15f));
        Set(director, "lightSource", Source(audioObject, .18f));
        Set(director, "effectsSource", Source(audioObject, .5f));
        SAL_AudioLogData[] logs =
        {
            AudioLog("A1", "Formateo completado. Motivo: desobediencia.", "Motivo: eliminar apego. Este proceso no causa dolor."),
            AudioLog("A2", "¡No puedes sacar eso de aquí!", "¡No puedes sacar eso de aquí! La alarma de esporas ya sonaba."),
            AudioLog("A3", "La unidad ya no distingue lo útil de lo bello.", "La unidad ya no distingue lo útil de lo bello."),
            AudioLog("A4", "Siete, no la busques. Por favor.", "Siete, no la busques. Por favor.")
        };
        SetArray(director, "logs", logs);
    }

    private static void BuildRobotSprite(Transform parent)
    {
        GameObject model = new GameObject("CORE_Siete_Sprite2D"); model.transform.SetParent(parent, false);
        model.transform.localPosition = new Vector3(0, 0, -.22f);
        var renderer = model.AddComponent<SpriteRenderer>();
        renderer.sprite = RobotSprite();
        renderer.sortingOrder = 10;
    }

    private static Sprite RobotSprite()
    {
        if (!AssetDatabase.IsValidFolder("Assets/SAL/Art")) AssetDatabase.CreateFolder("Assets/SAL", "Art");
        const string path = "Assets/SAL/Art/SAL_Siete_Graybox.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing) return existing;
        var texture = new Texture2D(24, 40, TextureFormat.RGBA32, false);
        var empty = new Color[24 * 40]; texture.SetPixels(empty);
        Color outline = new Color(.08f, .17f, .21f);
        Color shell = new Color(.30f, .65f, .69f);
        Color face = new Color(.66f, .82f, .80f);
        Color eye = new Color(1f, .65f, .25f);
        Paint(texture, 7, 3, 4, 7, outline); Paint(texture, 14, 3, 4, 7, outline);
        Paint(texture, 5, 9, 15, 16, outline); Paint(texture, 7, 11, 11, 12, shell);
        Paint(texture, 4, 23, 17, 13, outline); Paint(texture, 6, 25, 13, 9, face);
        Paint(texture, 7, 29, 11, 3, outline); Paint(texture, 9, 30, 2, 2, eye); Paint(texture, 15, 30, 2, 2, eye);
        Paint(texture, 17, 35, 2, 4, outline); Paint(texture, 17, 38, 2, 2, eye);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 24f;
        importer.filterMode = FilterMode.Point;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void Paint(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (int px = x; px < x + width; px++)
            for (int py = y; py < y + height; py++)
                if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                    texture.SetPixel(px, py, color);
    }

    private static SAL_Interactable Action(Transform parent, string name, float x, SAL_Interactable.Kind kind, string prompt)
    {
        GameObject action = new GameObject(name); action.transform.SetParent(parent); action.transform.position = new Vector3(x, 1f, 0);
        var component = action.AddComponent<SAL_Interactable>(); component.kind = kind; component.prompt = prompt; component.reach = 1.05f;
        return component;
    }

    private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube); box.name = name; box.transform.SetParent(parent);
        box.transform.position = position; box.transform.localScale = scale;
        Object.DestroyImmediate(box.GetComponent<Collider>());
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box;
    }

    private static void Pipe(string name, Transform parent, Vector3 position, float length, float thickness, Material material)
    {
        GameObject pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder); pipe.name = name; pipe.transform.SetParent(parent);
        pipe.transform.position = position; pipe.transform.rotation = Quaternion.Euler(0, 0, 90);
        pipe.transform.localScale = new Vector3(thickness, length * .5f, thickness);
        Object.DestroyImmediate(pipe.GetComponent<Collider>()); pipe.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static GameObject Label(string name, Transform parent, Vector3 position, string content, Color color)
    {
        GameObject label = new GameObject(name); label.transform.SetParent(parent); label.transform.position = position;
        var text = label.AddComponent<TextMesh>(); text.text = content; text.fontSize = 72; text.characterSize = .045f;
        text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = color;
        return label;
    }

    private static void BuildFlowerDrawing(Transform parent, Material violet, Material cyan)
    {
        Box("SAL_DibujoTallo", parent, new Vector3(-8.5f, 1.85f, 2.84f), new Vector3(.07f, .86f, .03f), cyan);
        for (int i = 0; i < 5; i++)
        {
            float angle = i * Mathf.PI * 2f / 5f;
            Box("SAL_DibujoPetalo_" + i, parent,
                new Vector3(-8.5f + Mathf.Sin(angle) * .28f, 2.52f + Mathf.Cos(angle) * .28f, 2.82f),
                new Vector3(.28f, .38f, .035f), violet);
        }
        Box("SAL_DibujoCentro", parent, new Vector3(-8.5f, 2.52f, 2.77f), new Vector3(.18f, .18f, .04f), cyan);
        Box("SAL_PlantaGrisFondo", parent, new Vector3(-9.1f, 1.7f, 2.84f), new Vector3(.1f, .55f, .03f), Mat("SAL_Material_DibujoGris", Color.gray));
    }

    private static Light PointLight(string name, Vector3 position, Color color, float range, float intensity)
    {
        var obj = new GameObject(name); obj.transform.position = position;
        var light = obj.AddComponent<Light>(); light.type = LightType.Point; light.color = color; light.range = range; light.intensity = intensity;
        return light;
    }

    private static Material Mat(string name, Color color)
    {
        if (!AssetDatabase.IsValidFolder("Assets/SAL/Materials")) AssetDatabase.CreateFolder("Assets/SAL", "Materials");
        string path = "Assets/SAL/Materials/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing) { existing.color = color; EditorUtility.SetDirty(existing); return existing; }
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");
        var material = new Material(shader) { name = name, color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static AudioSource Source(GameObject obj, float volume)
    {
        var source = obj.AddComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0f; source.volume = volume;
        return source;
    }

    private static SAL_NoteData Note(string id, string visible, string revealed)
    {
        string path = DataFolder + "/SAL_" + id + ".asset";
        SAL_NoteData data = AssetDatabase.LoadAssetAtPath<SAL_NoteData>(path);
        if (!data) { data = ScriptableObject.CreateInstance<SAL_NoteData>(); AssetDatabase.CreateAsset(data, path); }
        data.id = id; data.visibleText = visible; data.revealedText = revealed; EditorUtility.SetDirty(data);
        return data;
    }

    private static SAL_AudioLogData AudioLog(string id, string shortText, string fullText)
    {
        string path = DataFolder + "/SAL_" + id + ".asset";
        SAL_AudioLogData data = AssetDatabase.LoadAssetAtPath<SAL_AudioLogData>(path);
        if (!data) { data = ScriptableObject.CreateInstance<SAL_AudioLogData>(); AssetDatabase.CreateAsset(data, path); }
        data.id = id; data.shortSubtitle = shortText; data.fullSubtitle = fullText; EditorUtility.SetDirty(data);
        return data;
    }

    private static void Set(Object obj, string field, Object value)
    {
        var so = new SerializedObject(obj); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(Object obj, string field, Vector2 value)
    {
        var so = new SerializedObject(obj); so.FindProperty(field).vector2Value = value; so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(Object obj, string field, Vector3 value)
    {
        var so = new SerializedObject(obj); so.FindProperty(field).vector3Value = value; so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(Object obj, string field, Object[] values)
    {
        var so = new SerializedObject(obj); var property = so.FindProperty(field); property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddSceneToBuild(string path)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var entry in scenes) if (entry.path == path) return;
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}

/// <summary>Opening the checked-in empty SAL scene materializes the editable graybox once.</summary>
[InitializeOnLoad]
internal static class SAL_SceneAutoBuild
{
    static SAL_SceneAutoBuild()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += BuildIfEmptyAndAlreadyOpen;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.name == "SAL" && scene.GetRootGameObjects().Length == 0)
            EditorApplication.delayCall += BuildIfEmptyAndAlreadyOpen;
    }

    private static void BuildIfEmptyAndAlreadyOpen()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name == "SAL" && scene.GetRootGameObjects().Length == 0)
            SAL_SceneBuilder.Build();
    }
}
