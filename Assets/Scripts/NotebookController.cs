using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>Persistent in-memory registry and UI for the ten collectible notes.</summary>
[DisallowMultipleComponent]
public sealed class NotebookController : MonoBehaviour
{
    public const int NoteCount = 10;
    public static NotebookController Instance { get; private set; }

    [SerializeField] private GameObject[] pages = new GameObject[NoteCount];
    [SerializeField] private Button[] pageButtons = new Button[NoteCount];
    [Header("Sellos — UI editable en Inicio")]
    [SerializeField] private Button stampsButton;
    [SerializeField] private GameObject stampsPanel;
    [SerializeField] private TMP_Text[] stampSlots = new TMP_Text[3];
    [SerializeField] private Button backToNotesButton;
    [SerializeField] private GameObject stampNotification;
    [SerializeField] private TMP_Text stampNotificationText;
    private bool showingStamps;
    private float notificationUntil;
    private readonly bool[] unlockedNotes = new bool[NoteCount];
    private readonly string[] titles = new string[NoteCount];
    private readonly string[] contents = new string[NoteCount];
    private readonly string[] rereadings = new string[NoteCount];
    private GameObject notebookPanel;
    private Text preview;
    private Text heading;
    private int selectedPage = -1;
    private bool reading;
    public bool IsOpen => notebookPanel && notebookPanel.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildPanel();
        SAL_Progress.FlagChanged += OnProgressChanged;
        Refresh();
    }

    private void Update()
    {
        if (stampNotification && stampNotification.activeSelf && Time.unscaledTime >= notificationUntil)
            stampNotification.SetActive(false);
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.tabKey.wasPressedThisFrame) ToggleNotebook();
        else if (IsOpen && keyboard.escapeKey.wasPressedThisFrame) CloseNotebook();
    }

    private void OnDestroy()
    {
        SAL_Progress.FlagChanged -= OnProgressChanged;
        if (Instance == this) Instance = null;
    }

    private void OnProgressChanged(string id, bool value)
    {
        if (!CORE_GameState.IsStamp(id)) return;
        Refresh();
        if (!value || !stampNotification || !stampNotificationText) return;
        stampNotificationText.text = CORE_GameState.Symbol(id) + " SELLO RECOGIDO\n<size=22>Revísalo en el Cuaderno (Tab)</size>";
        notificationUntil = Time.unscaledTime + 3.5f;
        stampNotification.SetActive(true);
    }

    public void ShowStamps() { showingStamps = true; Refresh(); }
    public void ShowNotes() { showingStamps = false; Refresh(); }

    // Registers content without opening its page or displaying the story in the world HUD.
    public void CollectNote(int index, string title, string content, string rereading = "")
    {
        if (!IsValidIndex(index)) return;
        titles[index] = title;
        contents[index] = content;
        rereadings[index] = rereading;
        UnlockNote(index);
    }

    public void UnlockNote(int index)
    {
        if (!IsValidIndex(index))
            return;

        unlockedNotes[index] = true;
        Refresh();
    }

    public bool IsNoteUnlocked(int index) => IsValidIndex(index) && unlockedNotes[index];

    [ContextMenu("Debug/Unlock note 1")]
    private void DebugUnlockNoteOne() => UnlockNote(0);

    [ContextMenu("Debug/Unlock all notes")]
    private void DebugUnlockAllNotes()
    {
        for (int i = 0; i < NoteCount; i++)
            UnlockNote(i);
    }

    public void ShowPage(int index)
    {
        if (!IsNoteUnlocked(index))
            return;
        showingStamps = false;
        if (selectedPage == index) reading = true;
        else { selectedPage = index; reading = false; }
        if (notebookPanel) notebookPanel.SetActive(true);
        Refresh();
    }

    public void ToggleNotebook()
    {
        if (!notebookPanel) return;
        notebookPanel.SetActive(!notebookPanel.activeSelf);
        Refresh();
    }

    public void CloseNotebook() { if (notebookPanel) notebookPanel.SetActive(false); }

    private void Refresh()
    {
        for (int i = 0; i < NoteCount; i++)
        {
            if (pages != null && i < pages.Length && pages[i] != null)
            {
                bool visible = !showingStamps && selectedPage == i && reading;
                pages[i].SetActive(visible);
                if (visible)
                {
                    var body = pages[i].GetComponentInChildren<Text>(true);
                    if (body) body.text = ReadableContent(i);
                }
            }

            if (pageButtons == null || i >= pageButtons.Length || pageButtons[i] == null)
                continue;

            pageButtons[i].interactable = unlockedNotes[i];
            Text label = pageButtons[i].GetComponentInChildren<Text>();
            if (label != null)
                label.text = unlockedNotes[i]
                    ? $"{(selectedPage == i ? "► " : "")}Nota {i + 1} · {(selectedPage == i && !reading ? "Leer" : "Abrir página")}"
                    : $"Nota {i + 1} · No recogida";
        }
        if (stampsPanel) stampsPanel.SetActive(showingStamps);
        string[] ids = { CORE_GameState.SelloTriangulo, CORE_GameState.SelloRombo, CORE_GameState.SelloCruz };
        for (int i = 0; i < stampSlots.Length && i < ids.Length; i++)
            if (stampSlots[i]) stampSlots[i].text = CORE_GameState.HasStamp(ids[i]) ? CORE_GameState.Symbol(ids[i]) : "?";
        if (heading) heading.text = showingStamps ? "CUADERNO · Sellos" : selectedPage < 0 ? "CUADERNO · Notas recogidas" : $"NOTA {selectedPage + 1} · {titles[selectedPage]}";
        if (preview)
        {
            preview.gameObject.SetActive(!showingStamps && (!reading || selectedPage < 0));
            preview.text = selectedPage < 0
                ? "Selecciona una nota del índice.\n\nSolo puedes consultar las notas que hayas recogido."
                : $"{titles[selectedPage]}\n\nPágina abierta.\nPulsa otra vez su botón del índice para leer la nota.";
        }
    }

    private string ReadableContent(int index)
    {
        string body = contents[index] ?? "Contenido pendiente.";
        return string.IsNullOrEmpty(rereadings[index]) ? body
            : body + "\n\nRelectura\n" + rereadings[index];
    }

    // Reuses the existing Canvas, page objects and index buttons. Only notebook UI is authored here.
    private void BuildPanel()
    {
        var canvas = GetComponent<Canvas>();
        if (!canvas) return;
        canvas.sortingOrder = 30;
        var scaler = GetComponent<CanvasScaler>();
        if (scaler)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
        }
        notebookPanel = new GameObject("Panel_Cuaderno", typeof(RectTransform), typeof(Image));
        notebookPanel.transform.SetParent(transform, false);
        Place((RectTransform)notebookPanel.transform, new Vector2(1120, 720), Vector2.zero);
        notebookPanel.GetComponent<Image>().color = new Color(.035f, .075f, .10f, .99f);
        heading = Label("Titulo", notebookPanel.transform, new Vector2(960, 56), new Vector2(-25, 308), "CUADERNO", 26);
        Label("Ayuda", notebookPanel.transform, new Vector2(1000, 42), new Vector2(0, -320),
            "Primer clic: abrir página · Segundo clic: leer · Tab / Esc: cerrar", 18);
        var close = MakeButton("Cerrar", notebookPanel.transform, new Vector2(84, 40), new Vector2(490, 310), "Cerrar");
        close.onClick.AddListener(CloseNotebook);
        var pageArea = new GameObject("AreaLectura", typeof(RectTransform), typeof(Image));
        pageArea.transform.SetParent(notebookPanel.transform, false);
        Place((RectTransform)pageArea.transform, new Vector2(730, 540), new Vector2(165, -5));
        pageArea.GetComponent<Image>().color = new Color(.90f, .87f, .75f);
        preview = Label("PaginaCerrada", pageArea.transform, new Vector2(650, 460), Vector2.zero, "", 24);
        preview.color = new Color(.08f, .10f, .11f);
        preview.alignment = TextAnchor.MiddleCenter;
        if (stampsPanel)
        {
            stampsPanel.transform.SetParent(pageArea.transform, false);
            Place((RectTransform)stampsPanel.transform, new Vector2(730, 540), Vector2.zero);
        }
        if (stampsButton)
        {
            stampsButton.transform.SetParent(notebookPanel.transform, false);
            Place(stampsButton.GetComponent<RectTransform>(), new Vector2(300, 45), new Vector2(-375, -270));
            stampsButton.onClick.AddListener(ShowStamps);
        }
        if (backToNotesButton) backToNotesButton.onClick.AddListener(ShowNotes);
        if (stampNotification) stampNotification.SetActive(false);
        for (int i = 0; i < NoteCount; i++)
        {
            titles[i] = $"Nota {i + 1}";
            if (pages != null && i < pages.Length && pages[i])
            {
                var page = pages[i];
                var text = page.GetComponentInChildren<Text>(true);
                if (text) contents[i] = text.text;
                page.transform.SetParent(pageArea.transform, false);
                var rect = page.GetComponent<RectTransform>();
                if (rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
                if (text)
                {
                    text.fontSize = 25;
                    text.horizontalOverflow = HorizontalWrapMode.Wrap;
                    text.verticalOverflow = VerticalWrapMode.Truncate;
                    text.resizeTextForBestFit = true;
                    text.resizeTextMinSize = 16;
                    text.resizeTextMaxSize = 25;
                }
            }
            if (pageButtons == null || i >= pageButtons.Length || !pageButtons[i]) continue;
            var button = pageButtons[i];
            button.transform.SetParent(notebookPanel.transform, false);
            Place(button.GetComponent<RectTransform>(), new Vector2(300, 45), new Vector2(-375, 240 - i * 51));
            // Editor-time AddListener calls are not serialized. Bind each page on the live instance.
            button.onClick = new Button.ButtonClickedEvent();
            int index = i;
            button.onClick.AddListener(() => ShowPage(index));
            var label = button.GetComponentInChildren<Text>(true);
            if (label) label.fontSize = 18;
        }
        var open = MakeButton("Abrir_Cuaderno", transform, new Vector2(210, 44), Vector2.zero, "Cuaderno [Tab]");
        var openRect = open.GetComponent<RectTransform>();
        openRect.anchorMin = openRect.anchorMax = openRect.pivot = Vector2.one;
        openRect.anchoredPosition = new Vector2(-24, -24);
        open.onClick.AddListener(ToggleNotebook);
        notebookPanel.SetActive(false);
    }

    private static void Place(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.localScale = Vector3.one;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static Text Label(string name, Transform parent, Vector2 size, Vector2 position, string value, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, size, position);
        var label = go.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.color = Color.white;
        label.text = value;
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
        return label;
    }

    private static Button MakeButton(string name, Transform parent, Vector2 size, Vector2 position, string caption)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, size, position);
        go.GetComponent<Image>().color = new Color(.12f, .27f, .33f);
        var text = Label("Texto", go.transform, size, Vector2.zero, caption, 18);
        text.alignment = TextAnchor.MiddleCenter;
        return go.GetComponent<Button>();
    }

    private static bool IsValidIndex(int index) => index >= 0 && index < NoteCount;

#if UNITY_EDITOR
    public void ConfigureForScene(GameObject[] configuredPages, Button[] configuredButtons)
    {
        pages = configuredPages;
        pageButtons = configuredButtons;
    }
#endif
}
