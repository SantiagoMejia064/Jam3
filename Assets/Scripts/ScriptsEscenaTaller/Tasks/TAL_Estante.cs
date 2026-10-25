using UnityEngine;
using UnityEngine.UI;

public class TAL_Estante : MonoBehaviour
{
    public GameObject notebookDiagramPrefab;
    public string noteTitle = "N3 · Por reparar";

    private const string NoteFound = "TAL_N3Encontrada";

    void Start()
    {
        if (SAL_Progress.HasFlag(NoteFound))
            AddDiagramToNotebook();
    }

    public void Interact()
    {
        NotebookController notebook = NotebookController.Instance;

        if (notebook == null)
        {
            CORE_InteractionHUD.ShowMessage(
                "El Cuaderno no está disponible.");
            return;
        }

        if (notebook.IsOpen)
            return;

        if (notebook.IsNoteUnlocked(2))
        {
            AddDiagramToNotebook();
            CORE_InteractionHUD.ShowMessage(
                "La tablilla está guardada en N3.");
            return;
        }

        if (!AddDiagramToNotebook())
        {
            CORE_InteractionHUD.ShowMessage(
                "Falta configurar el dibujo de N3.");
            return;
        }

        // N3 corresponde al índice 2. Su contenido es el dibujo.
        notebook.CollectNote(2, noteTitle, "");

        SAL_Progress.SetFlag(NoteFound);

        CORE_InteractionHUD.ShowMessage(
            "N3 guardada en el Cuaderno.");
    }

    bool AddDiagramToNotebook()
    {
        NotebookController notebook = NotebookController.Instance;

        if (notebook == null || notebookDiagramPrefab == null)
            return false;

        foreach (Transform child in
                 notebook.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != "Pagina_Nota_03")
                continue;

            if (child.Find("TAL_DiagramaN3") != null)
                return true;

            GameObject diagram =
                Instantiate(notebookDiagramPrefab, child, false);

            diagram.name = "TAL_DiagramaN3";

            // Ocultamos el texto anterior: esta nota es visual.
            Text body = child.GetComponentInChildren<Text>(true);

            if (body != null)
                body.gameObject.SetActive(false);

            return true;
        }

        return false;
    }
}
