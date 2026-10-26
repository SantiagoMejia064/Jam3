using UnityEngine;

/// <summary>Mesa de trabajo del Invernadero donde Siete encuentra la planilla de cultivo N8.</summary>
[RequireComponent(typeof(Collider))]
public sealed class INV_WorkTable : MonoBehaviour
{
    public const string FlagN8 = "INV_N8Leida";

    [Header("Configuración N8")]
    [SerializeField] private string noteTitle = "Planilla de cultivo";
    [TextArea(2, 6)]
    [SerializeField] private string noteVisible = "Ciclo oscuro: nadie. Ahí la guardé.\n\n[PLANILLA DE CULTIVO]\nCalor: 2 (Llama)\nOscuridad: 4 (Luna)\nPulso: 1 (Huellas)\nBruma: 3 (Gota)";
    [TextArea(1, 3)]
    [SerializeField] private string noteTachado = "Vega no sabe que hay semillas. Yo sí.";
    [SerializeField] private GameObject noteVisualSheet;

    private CORE_PlayerController player;
    private bool inRange;

    private void Start()
    {
        GetComponent<Collider>().isTrigger = true;
        if (SAL_Progress.HasFlag(FlagN8) && noteVisualSheet)
            noteVisualSheet.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out CORE_PlayerController pc))
        {
            player = pc;
            inRange = true;
            CORE_InteractionHUD.SetPrompt(this, "E · Leer planilla de cultivo");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<CORE_PlayerController>())
        {
            player = null;
            inRange = false;
            CORE_InteractionHUD.ClearPrompt(this);
        }
    }

    private void OnDisable()
    {
        inRange = false;
        player = null;
        CORE_InteractionHUD.ClearPrompt(this);
    }

    private void Update()
    {
        if (!inRange || player == null) return;
        if (NotebookController.Instance && NotebookController.Instance.IsOpen) return;

        if (player.InteractPressed)
        {
            CollectN8();
        }
    }

    public void CollectN8()
    {
        // Registro en el cuaderno persistente (índice 7 es N8)
        if (NotebookController.Instance)
        {
            NotebookController.Instance.CollectNote(7, noteTitle, noteVisible, noteTachado);
        }

        SAL_Progress.SetFlag(FlagN8);
        if (noteVisualSheet) noteVisualSheet.SetActive(false);

        CORE_InteractionHUD.ShowMessage("N8: \"Ciclo oscuro: nadie. Ahí la guardé.\"\nPlanilla registrada en el Cuaderno (Tab).", 4.5f);
    }
}
