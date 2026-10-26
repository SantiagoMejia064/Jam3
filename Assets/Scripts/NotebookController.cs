using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
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
    [Header("UI authored in the scene")]
    [SerializeField] private GameObject notebookPanel;
    [SerializeField] private RectTransform pageArea;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Text preview;
    [SerializeField] private Text heading;
    private bool showingStamps;
    private float notificationUntil;
    private readonly bool[] unlockedNotes = new bool[NoteCount];
    private readonly string[] titles = new string[NoteCount];
    private readonly string[] contents = new string[NoteCount];
    private readonly string[] rereadings = new string[NoteCount];
    private int selectedPage = -1;
    private bool reading;
    public bool IsOpen => notebookPanel && notebookPanel.activeSelf;

    private void Awake()
    {
        // La entrada de UI pertenece a esta escena, aunque su Cuaderno sea un duplicado.
        foreach (EventSystem uiInput in GetComponentsInChildren<EventSystem>(true))
            uiInput.transform.SetParent(null, true);

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BindSceneUi();
        SAL_Progress.FlagChanged += OnProgressChanged;
        Refresh();
    }

    private void Update()
    {
        if (stampNotification && stampNotification.activeSelf &&
            (Time.unscaledTime >= notificationUntil || IsOpen))
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
        // Una nota abierta también debe actualizarse al llegar la revelación.
        if (id == SAL_Progress.Revealed) { Refresh(); return; }
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

    public void CloseNotebook()
    {
        if (notebookPanel) notebookPanel.SetActive(false);
        Refresh();
    }

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
                    if (body && body.enabled) body.text = ReadableContent(i);
                    else
                    {
                        var tmpBody = pages[i].GetComponentInChildren<TMP_Text>(true);
                        if (tmpBody) tmpBody.text = ReadableContent(i);
                    }
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
        if (stampsButton) stampsButton.gameObject.SetActive(IsOpen);
        if (stampsPanel) stampsPanel.SetActive(IsOpen && showingStamps);
        if (backToNotesButton) backToNotesButton.gameObject.SetActive(IsOpen && showingStamps);
        string[] ids = { CORE_GameState.SelloTriangulo, CORE_GameState.SelloRombo, CORE_GameState.SelloCruz };
        for (int i = 0; i < stampSlots.Length && i < ids.Length; i++)
            if (stampSlots[i]) stampSlots[i].text = CORE_GameState.HasStamp(ids[i]) ? CORE_GameState.Symbol(ids[i]) : "?";
        if (heading) heading.text = showingStamps ? "CUADERNO · Sellos" : selectedPage < 0 ? "CUADERNO · Notas recogidas" : $"NOTA {selectedPage + 1} · {titles[selectedPage]}";
        if (preview)
        {
            preview.gameObject.SetActive(!showingStamps && (!reading || selectedPage < 0));
            preview.text = selectedPage < 0
                ? "Selecciona una nota del índice para leerla."
                : $"{titles[selectedPage]}\n\nPulsa de nuevo en el índice para leer la nota.";
        }
    }

    private string ReadableContent(int index)
    {
        string body = contents[index] ?? string.Empty;
        return !SAL_Progress.HasFlag(SAL_Progress.Revealed) || string.IsNullOrEmpty(rereadings[index]) ? body
            : body + "\n\nRelectura\n" + rereadings[index];
    }

    // All interface objects are authored in the scene; runtime only wires existing controls.
    private void BindSceneUi()
    {
        var canvas = GetComponent<Canvas>();
        if (!canvas || !notebookPanel || !pageArea || !preview || !heading) return;
        // Mantener los sellos dentro del Cuaderno, también en escenas antiguas.
        if (stampsButton)
        {
            stampsButton.transform.SetParent(notebookPanel.transform, false);
            var rect = stampsButton.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(300, 45);
            rect.anchoredPosition = new Vector2(-375, -270);
            rect.localScale = Vector3.one;
        }
        if (stampsPanel)
        {
            stampsPanel.transform.SetParent(pageArea, false);
            var rect = stampsPanel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
        if (stampNotification)
        {
            stampNotification.transform.SetParent(transform, false);
            var rect = stampNotification.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, -185);
            rect.sizeDelta = new Vector2(580, 110);
            rect.localScale = Vector3.one;
        }
        canvas.sortingOrder = 30;
        var scaler = GetComponent<CanvasScaler>();
        if (scaler)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
        }
        if (stampsButton) stampsButton.onClick.AddListener(ShowStamps);
        if (backToNotesButton) backToNotesButton.onClick.AddListener(ShowNotes);
        if (closeButton) closeButton.onClick.AddListener(CloseNotebook);
        if (openButton) openButton.onClick.AddListener(ToggleNotebook);

        for (int i = 0; i < NoteCount; i++)
        {
            titles[i] = $"Nota {i + 1}";
            if (pages != null && i < pages.Length && pages[i])
            {
                GameObject page = pages[i];
                Text body = page.GetComponentInChildren<Text>(true);
                if (body) contents[i] = body.text;
            }
            if (pageButtons == null || i >= pageButtons.Length || !pageButtons[i]) continue;
            Button button = pageButtons[i];
            button.onClick = new Button.ButtonClickedEvent();
            int index = i;
            button.onClick.AddListener(() => ShowPage(index));
        }
        if (stampNotification) stampNotification.SetActive(false);
        notebookPanel.SetActive(false);
    }
    private static bool IsValidIndex(int index) => index >= 0 && index < NoteCount;

#if UNITY_EDITOR
    public void ConfigureForScene(GameObject[] configuredPages, Button[] configuredButtons)
    {
        pages = configuredPages;
        pageButtons = configuredButtons;
    }

    public void ConfigureSceneUi(GameObject panel, RectTransform area, Button open, Button close, Text previewText, Text headingText)
    {
        notebookPanel = panel;
        pageArea = area;
        openButton = open;
        closeButton = close;
        preview = previewText;
        heading = headingText;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}


