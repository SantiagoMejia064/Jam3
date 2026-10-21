using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

/// <summary>Incremental authoring only: does not rebuild Bodega or touch any note content.</summary>
public static class StampSetup
{
    private const string Folder = "Assets/UI_Sellos";

    [MenuItem("Jam3/Conectar sellos")]
    public static void Build()
    {
        if (!Shader.Find("TextMeshPro/Distance Field"))
        {
            var package = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories).First();
            AssetDatabase.ImportPackage(package, false);
            AssetDatabase.Refresh();
        }
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "UI_Sellos");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder + "/SellosFont.asset");
        if (!font)
        {
            // Bake only the required characters. No OS font file is shipped with the game.
            font = TMP_FontAsset.CreateFontAsset("Segoe UI Symbol", "Regular", 64);
            if (!font.TryAddCharacters("?▲◆✚SELLO RECOGIDOVolvernatsLaboratorioTallerBodegaSellosRevísalo en el Cuaderno (Tab)áéíóúñ0123456789", out string missing))
                throw new Exception("Missing stamp font characters: " + missing);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            font.name = "SellosFont";
            AssetDatabase.CreateAsset(font, Folder + "/SellosFont.asset");
            font.material.name = "SellosFont Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var texture in font.atlasTextures) { texture.name = "SellosFont Atlas"; AssetDatabase.AddObjectToAsset(texture, font); }
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
        }
        var root = UI("UI_Sellos", null, new Vector2(1920, 1080), Vector2.zero);
        var panel = UI("Panel_Sellos", root.transform, new Vector2(730, 540), Vector2.zero);
        string[] names = { "Laboratorio", "Taller", "Bodega" };
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 225;
            Label(names[i], panel.transform, new Vector2(215, 60), new Vector2(x, 100), names[i], 26, font);
            Label("Espacio_" + names[i], panel.transform, new Vector2(200, 160), new Vector2(x, -15), "?", 90, font);
        }
        MakeButton("Volver_Notas", panel.transform, new Vector2(300, 50), new Vector2(0, -190), "Volver a notas", font);
        MakeButton("Sellos", root.transform, new Vector2(300, 45), new Vector2(-375, -270), "Sellos", font);
        var toast = UI("Notificacion_Sello", root.transform, new Vector2(580, 110), new Vector2(0, 125));
        var toastRect = (RectTransform)toast.transform;
        toastRect.anchorMin = toastRect.anchorMax = new Vector2(.5f, 0);
        toast.AddComponent<Image>().color = new Color(.035f, .075f, .10f, .96f);
        toast.GetComponent<Image>().raycastTarget = false;
        Label("Mensaje", toast.transform, new Vector2(550, 100), Vector2.zero,
            "✚ SELLO RECOGIDO\n<size=22>Revísalo en el Cuaderno (Tab)</size>", 30, font).color = Color.white;
        toast.SetActive(false);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, Folder + "/UI_Sellos.prefab");
        UnityEngine.Object.DestroyImmediate(root);

        foreach (string path in new[] { "Assets/Scenes/Inicio.unity", "Assets/Scenes/Bodega.unity" })
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var notebook = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<NotebookController>(true)).Single();
            var old = notebook.transform.Find("UI_Sellos");
            if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var ui = (GameObject)PrefabUtility.InstantiatePrefab(prefab, notebook.transform);
            var rect = (RectTransform)ui.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero; rect.anchoredPosition = Vector2.zero;
            var stamps = ui.transform.Find("Panel_Sellos");
            var so = new SerializedObject(notebook);
            so.FindProperty("stampsButton").objectReferenceValue = ui.transform.Find("Sellos").GetComponent<Button>();
            so.FindProperty("stampsPanel").objectReferenceValue = stamps.gameObject;
            so.FindProperty("backToNotesButton").objectReferenceValue = stamps.Find("Volver_Notas").GetComponent<Button>();
            var slots = so.FindProperty("stampSlots"); slots.arraySize = 3;
            for (int i = 0; i < 3; i++) slots.GetArrayElementAtIndex(i).objectReferenceValue = stamps.Find("Espacio_" + names[i]).GetComponent<TMP_Text>();
            so.FindProperty("stampNotification").objectReferenceValue = ui.transform.Find("Notificacion_Sello").gameObject;
            so.FindProperty("stampNotificationText").objectReferenceValue = ui.transform.Find("Notificacion_Sello/Mensaje").GetComponent<TMP_Text>();
            so.ApplyModifiedPropertiesWithoutUndo();
            if (path.EndsWith("Bodega.unity")) ConfigureBodega();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("STAMP_SETUP_OK: UI conectada en Inicio/Bodega y punto de ronda guardado.");
    }

    public static void ConfigureBodega()
    {
        var point = GameObject.Find("BOD_PuntoRonda_Cruz");
        if (!point) throw new Exception("Missing Bodega stamp point");
        var visible = point.transform.Find("Visual");
        if (visible) UnityEngine.Object.DestroyImmediate(visible.gameObject);
        var reward = point.transform.Find("SelloCruz").gameObject;
        var collectible = reward.GetComponent<CORE_Collectible>();
        if (collectible) UnityEngine.Object.DestroyImmediate(collectible);
        var collider = reward.GetComponent<Collider>();
        if (collider) UnityEngine.Object.DestroyImmediate(collider);
        var text = reward.GetComponentInChildren<TextMesh>(true);
        if (text) UnityEngine.Object.DestroyImmediate(text.gameObject);
        var previousGlyph = reward.transform.Find("Simbolo_Cruz");
        if (previousGlyph) UnityEngine.Object.DestroyImmediate(previousGlyph.gameObject);
        var glyph = new GameObject("Simbolo_Cruz", typeof(TextMeshPro));
        glyph.transform.SetParent(reward.transform, false);
        var tmp = glyph.GetComponent<TextMeshPro>();
        tmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder + "/SellosFont.asset");
        tmp.text = "✚"; tmp.fontSize = 5; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(.05f, .85f, 1f); tmp.rectTransform.sizeDelta = new Vector2(.65f, .65f);
        reward.SetActive(false);
        var cover = point.transform.Find("Suciedad");
        cover.localPosition = new Vector3(0, .3f, -.25f);
        cover.localScale = new Vector3(.9f, .9f, .05f);
        var dirt = new SerializedObject(point.GetComponent<CORE_Dirt>());
        dirt.FindProperty("stampId").stringValue = CORE_GameState.SelloCruz;
        dirt.FindProperty("prompt").stringValue = "Mantén E para limpiar";
        dirt.FindProperty("cleanSeconds").floatValue = 1f;
        dirt.FindProperty("dustVisual").objectReferenceValue = cover.gameObject;
        dirt.FindProperty("revealOnClean").objectReferenceValue = reward;
        dirt.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject UI(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.sizeDelta = size; rect.anchoredPosition = position;
        return go;
    }
    private static TMP_Text Label(string name, Transform parent, Vector2 size, Vector2 pos, string value, int sizeInPoints, TMP_FontAsset font)
    {
        var go = UI(name, parent, size, pos);
        var label = go.AddComponent<TextMeshProUGUI>(); label.font = font; label.text = value;
        label.fontSize = sizeInPoints; label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(.08f, .10f, .11f); label.raycastTarget = false;
        return label;
    }
    private static void MakeButton(string name, Transform parent, Vector2 size, Vector2 pos, string value, TMP_FontAsset font)
    {
        var go = UI(name, parent, size, pos); go.AddComponent<Image>().color = new Color(.12f, .27f, .33f);
        go.AddComponent<Button>(); Label("Texto", go.transform, size, Vector2.zero, value, 24, font).color = Color.white;
    }
}
