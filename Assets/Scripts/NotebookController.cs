using UnityEngine;
using UnityEngine.UI;

/// <summary>Persistent in-memory registry and UI for the ten collectible notes.</summary>
[DisallowMultipleComponent]
public sealed class NotebookController : MonoBehaviour
{
    public const int NoteCount = 10;
    public static NotebookController Instance { get; private set; }

    [SerializeField] private GameObject[] pages = new GameObject[NoteCount];
    [SerializeField] private Button[] pageButtons = new Button[NoteCount];
    private readonly bool[] unlockedNotes = new bool[NoteCount];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Refresh();
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

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
                pages[i].SetActive(i == index);
        }
    }

    private void Refresh()
    {
        for (int i = 0; i < NoteCount; i++)
        {
            if (pages != null && i < pages.Length && pages[i] != null)
                pages[i].SetActive(false);

            if (pageButtons == null || i >= pageButtons.Length || pageButtons[i] == null)
                continue;

            pageButtons[i].interactable = unlockedNotes[i];
            Text label = pageButtons[i].GetComponentInChildren<Text>();
            if (label != null)
                label.text = unlockedNotes[i] ? $"Nota {i + 1}" : $"Nota {i + 1} (bloqueada)";
        }
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
