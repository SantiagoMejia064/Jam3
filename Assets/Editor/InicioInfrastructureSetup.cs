using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Authors the requested, editable infrastructure in Inicio without touching gameplay objects.</summary>
[InitializeOnLoad]
internal static class InicioInfrastructureSetup
{
    private const string InicioPath = "Assets/Scenes/Inicio.unity";
    private const string ManagerName = "GameSceneManager";
    private const string NotebookName = "Canvas_Cuaderno";

    static InicioInfrastructureSetup()
    {
        EditorApplication.delayCall += EnsureSetup;
    }

    [MenuItem("Jam3/Prepare Inicio Infrastructure")]
    private static void PrepareFromMenu() => EnsureSetup();

    private static void EnsureSetup()
    {
        if (Application.isPlaying || !System.IO.File.Exists(InicioPath))
            return;

        Scene inicio = SceneManager.GetSceneByPath(InicioPath);
        bool wasLoaded = inicio.IsValid() && inicio.isLoaded;
        if (!wasLoaded)
            inicio = EditorSceneManager.OpenScene(InicioPath, OpenSceneMode.Additive);

        bool changed = false;
        if (FindRoot(inicio, ManagerName) == null)
        {
            new GameObject(ManagerName).AddComponent<GameSceneManager>();
            changed = true;
        }

        GameObject player = FindPlayer(inicio);
        if (player != null && player.GetComponent<PlayerPersistence>() == null)
        {
            player.AddComponent<PlayerPersistence>();
            changed = true;
        }

        if (FindRoot(inicio, NotebookName) == null)
        {
            BuildNotebook(inicio);
            changed = true;
        }

        if (EnsureSingleEventSystem(inicio, FindRoot(inicio, NotebookName).transform))
        {
            changed = true;
        }

        if (EnsureInicioInBuildSettings())
            changed = true;

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(inicio);
            EditorSceneManager.SaveScene(inicio);
        }

        if (!wasLoaded)
            EditorSceneManager.CloseScene(inicio, true);
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == name)
                return root;
        return null;
    }

    private static bool EnsureSingleEventSystem(Scene scene, Transform notebookRoot)
    {
        EventSystem retained = null;
        bool changed = false;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (EventSystem eventSystem in root.GetComponentsInChildren<EventSystem>(true))
            {
                if (retained == null)
                {
                    retained = eventSystem;
                    continue;
                }

                Object.DestroyImmediate(eventSystem.gameObject);
                changed = true;
            }
        }

        if (retained != null)
            return changed;

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
        eventSystemObject.transform.SetParent(notebookRoot, false);
        return true;
    }

    private static GameObject FindPlayer(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform player = FindChildByName(root.transform, "CORE_Siete");
            if (player != null)
                return player.gameObject;
        }
        return null;
    }

    private static Transform FindChildByName(Transform parent, string objectName)
    {
        if (parent.name == objectName)
            return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindChildByName(child, objectName);
            if (result != null)
                return result;
        }

        return null;
    }

    private static void BuildNotebook(Scene scene)
    {
        GameObject canvasRoot = new GameObject(NotebookName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(NotebookController));
        SceneManager.MoveGameObjectToScene(canvasRoot, scene);
        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject buttonsRoot = CreateUiObject("Botones_Notas", canvasRoot.transform, typeof(RectTransform));
        RectTransform buttonsRect = buttonsRoot.GetComponent<RectTransform>();
        buttonsRect.anchorMin = new Vector2(0f, 1f);
        buttonsRect.anchorMax = new Vector2(0f, 1f);
        buttonsRect.pivot = new Vector2(0f, 1f);
        buttonsRect.anchoredPosition = new Vector2(24f, -24f);

        GameObject pagesRoot = CreateUiObject("Paginas_Notas", canvasRoot.transform, typeof(RectTransform));
        RectTransform pagesRect = pagesRoot.GetComponent<RectTransform>();
        pagesRect.anchorMin = new Vector2(.5f, .5f);
        pagesRect.anchorMax = new Vector2(.5f, .5f);
        pagesRect.sizeDelta = new Vector2(900f, 560f);

        GameObject[] pages = new GameObject[NotebookController.NoteCount];
        Button[] buttons = new Button[NotebookController.NoteCount];
        for (int i = 0; i < NotebookController.NoteCount; i++)
        {
            int index = i;
            buttons[i] = CreateButton(buttonsRoot.transform, i);
            buttons[i].onClick.AddListener(() => NotebookController.Instance?.ShowPage(index));
            pages[i] = CreatePage(pagesRoot.transform, i);
            pages[i].SetActive(false);
        }

        canvasRoot.GetComponent<NotebookController>().ConfigureForScene(pages, buttons);
    }

    private static Button CreateButton(Transform parent, int index)
    {
        GameObject buttonObject = CreateUiObject($"Boton_Nota_{index + 1:00}", parent, typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(0f, -index * 54f);
        rect.sizeDelta = new Vector2(310f, 44f);
        buttonObject.GetComponent<Image>().color = new Color(.10f, .18f, .22f, .96f);

        Text text = CreateText("Etiqueta", buttonObject.transform, 18, TextAnchor.MiddleCenter);
        text.text = $"Nota {index + 1} (bloqueada)";
        Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
        return buttonObject.GetComponent<Button>();
    }

    private static GameObject CreatePage(Transform parent, int index)
    {
        GameObject page = CreateUiObject($"Pagina_Nota_{index + 1:00}", parent, typeof(Image));
        Stretch(page.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        page.GetComponent<Image>().color = new Color(.90f, .87f, .75f, .98f);
        Text text = CreateText("Contenido", page.transform, 32, TextAnchor.UpperLeft);
        text.text = $"NOTA {index + 1}\n\nContenido pendiente.";
        text.color = new Color(.08f, .10f, .11f);
        Stretch(text.rectTransform, new Vector2(35f, 35f), Vector2.zero);
        return page;
    }

    private static Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = CreateUiObject(name, parent, typeof(Text));
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }

    private static GameObject CreateUiObject(string name, Transform parent, params System.Type[] components)
    {
        GameObject result = new GameObject(name, components);
        result.transform.SetParent(parent, false);
        return result;
    }

    private static void Stretch(RectTransform rect, Vector2 inset, Vector2 anchoredPosition)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = inset;
        rect.offsetMax = -inset;
        rect.anchoredPosition = anchoredPosition;
    }

    private static bool EnsureInicioInBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene scene in scenes)
            if (scene.path == InicioPath)
                return false;

        scenes.Add(new EditorBuildSettingsScene(InicioPath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        return true;
    }
}
