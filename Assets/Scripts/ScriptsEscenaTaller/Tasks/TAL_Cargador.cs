using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TAL_Cargador : MonoBehaviour
{
    public TAL_ObjetivoUI objectivePrefab;
    public GameObject notebookNotePrefab;
    public GameObject noteCleaningArea;
    public GameObject dustVisual;
    public GameObject noteVisual;
    public TMP_Text statusText;
    public AudioSource restSound;

    public const string NoteFound = "TAL_N10Encontrada";

    private TAL_ObjetivoUI objectiveUI;

    void Start()
    {
        PrepareNotebook();
        RefreshNote();
    }

    void Update()
    {
        // El Cuaderno puede venir de otra escena; usamos la instancia que sobrevivió.
        if (objectiveUI == null)
            PrepareNotebook();

        RefreshNote();
    }

    public void Rest()
    {
        if (NotebookController.Instance != null && NotebookController.Instance.IsOpen)
            return;

        if (objectiveUI == null)
            PrepareNotebook();

        if (objectiveUI == null)
        {
            CORE_InteractionHUD.ShowMessage("El Cuaderno no está disponible.");
            return;
        }

        objectiveUI.RefreshObjective();
        CORE_InteractionHUD.ShowMessage("Siete: " + objectiveUI.CurrentHint, 7f);

        if (restSound != null)
            restSound.Play();
    }

    void RefreshNote()
    {
        bool found = SAL_Progress.HasFlag(NoteFound);
        bool seed = TAL_ObjetivoUI.SeedObtained();

        if (noteCleaningArea != null)
            noteCleaningArea.SetActive(seed && !found && NotebookController.Instance != null);

        if (dustVisual != null)
            dustVisual.SetActive(!found);

        if (noteVisual != null)
            noteVisual.SetActive(found);

        string status = found ? "REGISTRO / EN EL CUADERNO" :
            seed ? "REGISTRO / LIMPIAR DETRÁS" : "CARGADOR / DESCANSAR";

        if (statusText != null && statusText.text != status)
            statusText.text = status;
    }

    void PrepareNotebook()
    {
        NotebookController notebook = NotebookController.Instance;
        if (notebook == null) return;

        // La ayuda queda dentro del Cuaderno persistente y continúa entre escenas.
        objectiveUI = notebook.GetComponentInChildren<TAL_ObjetivoUI>(true);
        if (objectiveUI == null && objectivePrefab != null)
        {
            objectiveUI = Instantiate(objectivePrefab, notebook.transform, false);
            objectiveUI.name = "TAL_ObjetivoUI";
        }

        // También dejamos espacio si el Cuaderno viene del Laboratorio.
        if (objectiveUI != null)
        {
            foreach (Button button in notebook.GetComponentsInChildren<Button>(true))
            {
                if (button.name == "Abrir_Cuaderno")
                    button.GetComponent<RectTransform>().anchoredPosition = new Vector2(-130, -212);
            }
        }

        if (notebookNotePrefab == null) return;

        foreach (Transform child in notebook.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != "Pagina_Nota_10") continue;
            if (child.Find("TAL_NotaN10") != null) return;

            GameObject note = Instantiate(notebookNotePrefab, child, false);
            note.name = "TAL_NotaN10";

            // N10 utiliza TextMeshPro. Las páginas antiguas pueden seguir usando Text.
            Text oldText = child.GetComponentInChildren<Text>(true);
            if (oldText != null) oldText.enabled = false;
            return;
        }
    }
}
