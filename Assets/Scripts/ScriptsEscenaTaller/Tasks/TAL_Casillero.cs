using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TAL_Casillero : MonoBehaviour
{
    public GameObject closedDoor;
    public GameObject noteVisual;
    public GameObject interactionArea;

    public AudioSource openingSound;
    public GameObject openedDoor;
    public GameObject notebookRoutePrefab;
    public TMP_Text statusText;

    public string noteTitle = "N2 · Ruta de la ronda";

    [TextArea(3, 6)]
    public string noteContent =
        "Nunca cambies el orden. Él no lo sabe.\n\n▲ → ◆ → ✚";

    private const string OpenFlag = "TAL_CasilleroAbierto";

    void Start()
    {
        Refresh();
        if (SAL_Progress.HasFlag(OpenFlag)) PrepareNotebookRoute();
    }

    public void Interact()
    {
        if (SAL_Progress.HasFlag(OpenFlag))
            return;

        NotebookController notebook = NotebookController.Instance;

        if (notebook != null && notebook.IsOpen)
            return;

        if (!CORE_InventoryBridge.HasItem("Pelota"))
        {
            CORE_InteractionHUD.ShowMessage(
                "No agarra. Necesito algo para tirar.");
            return;
        }

        if (!CORE_InventoryBridge.IsSelected("Pelota"))
        {
            CORE_InteractionHUD.ShowMessage(
                "Selecciona la Pelota en el inventario.");
            return;
        }

        if (notebook == null)
        {
            CORE_InteractionHUD.ShowMessage(
                "El Cuaderno no está disponible.");
            return;
        }

        // Los índices empiezan en cero: N2 corresponde al índice 1.
        notebook.CollectNote(1, noteTitle, noteContent);
        PrepareNotebookRoute();

        SAL_Progress.SetFlag(OpenFlag);

        if (openingSound != null)
            openingSound.Play();

        Refresh();

        CORE_InteractionHUD.ShowMessage(
            "Casillero abierto. N2 guardada en el Cuaderno.");
    }

    void Refresh()
    {
        bool open = SAL_Progress.HasFlag(OpenFlag);

        if (closedDoor != null)
            closedDoor.SetActive(!open);

        if (openedDoor != null)
            openedDoor.SetActive(open);

        if (statusText != null)
            statusText.text = open ? "N2 / GUARDADA" : "ATASCADO / SIN AGARRE";

        if (noteVisual != null)
            noteVisual.SetActive(open);

        if (interactionArea != null)
        {
            CORE_Interactable interaction =
                interactionArea.GetComponent<CORE_Interactable>();

            CORE_InteractionHUD.ClearPrompt(interaction);
            interactionArea.SetActive(!open);
        }
    }

    // Adjunta el dibujo a N2, incluso si el Cuaderno viene de otra escena.
    void PrepareNotebookRoute()
    {
        var notebook = NotebookController.Instance;
        if (notebook == null || notebookRoutePrefab == null) return;

        foreach (Transform child in notebook.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != "Pagina_Nota_02") continue;
            if (child.Find("TAL_RutaN2") != null) return;

            GameObject route = Instantiate(notebookRoutePrefab, child, false);
            route.name = "TAL_RutaN2";

            Transform noteLabel = route.transform.Find("TextoNota");
            if (noteLabel != null)
                noteLabel.GetComponent<TMP_Text>().text = noteContent;

            // Reserva la parte inferior de la página para los iconos.
            Text body = child.GetComponentInChildren<Text>(true);
            if (body != null)
            {
                if (noteLabel != null) body.gameObject.SetActive(false);
                body.rectTransform.anchorMin = new Vector2(.06f, .70f);
                body.rectTransform.anchorMax = new Vector2(.94f, .94f);
                body.rectTransform.offsetMin = body.rectTransform.offsetMax = Vector2.zero;
                body.alignment = TextAnchor.MiddleCenter;
                body.fontSize = 24;
                body.color = new Color(.10f, .15f, .17f);
            }
            return;
        }
    }
}
