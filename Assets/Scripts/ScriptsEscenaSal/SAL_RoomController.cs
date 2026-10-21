using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Owns only SAL puzzle rules. Shared player, inventory and doors stay outside this class.</summary>
public sealed class SAL_RoomController : MonoBehaviour
{
    [Header("Standalone graybox test; disable when all zones are integrated")]
    [SerializeField] private bool standaloneDemo = true;
    [SerializeField] private string exitScene = "Javier";
    [Header("Scene references")]
    [SerializeField] private GameObject dust;
    [SerializeField] private GameObject drawings;
    [SerializeField] private GameObject sealCover;
    [SerializeField] private GameObject diamondSeal;
    [SerializeField] private GameObject grille;
    [SerializeField] private GameObject filter;
    [SerializeField] private GameObject originalWarning;
    [SerializeField] private GameObject led;
    [SerializeField] private Light ledLight;
    [SerializeField] private GameObject[] blocks;
    [SerializeField] private SAL_NoteData[] notes;

    public string Message { get; private set; }
    public float MessageUntil { get; private set; }
    public bool PanelCoolingDown { get; private set; }
    public string PanelInput => panelInput;
    public bool StandaloneDemo => standaloneDemo;
    private string panelInput = string.Empty;
    private Coroutine panelRoutine;

    private void Awake()
    {
        if (standaloneDemo)
        {
            CORE_InventoryBridge.AddItem("Pelota");
            SAL_Progress.SetFlag(SAL_Progress.TriangleSeal);
            SAL_Progress.SetFlag(SAL_Progress.CrossSeal);
        }
        Refresh();
    }

    private void OnEnable() { SAL_Progress.FlagChanged += OnFlagChanged; }
    private void OnDisable() { SAL_Progress.FlagChanged -= OnFlagChanged; }
    private void OnFlagChanged(string id, bool value) { Refresh(); }

    public bool IsAvailable(SAL_Interactable target)
    {
        if (!target || !target.gameObject.activeInHierarchy) return false;
        switch (target.kind)
        {
            case SAL_Interactable.Kind.Trash: return !SAL_Progress.HasFlag(SAL_Progress.DrawingsFound);
            case SAL_Interactable.Kind.DiamondSeal: return !SAL_Progress.HasFlag(SAL_Progress.DiamondSeal);
            case SAL_Interactable.Kind.Valve: return !SAL_Progress.HasFlag(SAL_Progress.GrilleOpen);
            case SAL_Interactable.Kind.Filter: return SAL_Progress.HasFlag(SAL_Progress.GrilleOpen) && !SAL_Progress.HasFlag(SAL_Progress.FilterRemoved);
            case SAL_Interactable.Kind.PanelTriangle:
            case SAL_Interactable.Kind.PanelDiamond:
            case SAL_Interactable.Kind.PanelCross: return !SAL_Progress.HasFlag(SAL_Progress.PanelSolved);
            case SAL_Interactable.Kind.Block: return SAL_Progress.HasFlag(SAL_Progress.PanelSolved) && !SAL_Progress.HasFlag(SAL_Progress.SeedObtained);
            default: return true;
        }
    }

    public void Use(SAL_Interactable target)
    {
        if (!IsAvailable(target)) return;
        switch (target.kind)
        {
            case SAL_Interactable.Kind.Exit:
                if (Application.CanStreamedLevelBeLoaded(exitScene)) SceneManager.LoadScene(exitScene);
                else Tell("El Pasillo aún no está en Build Settings. Puedes seguir probando SAL.");
                break;
            case SAL_Interactable.Kind.Trash:
                SAL_Progress.SetFlag(SAL_Progress.DrawingsFound);
                Tell("N6 · Ella es una planta. Sus dibujos quedaron aquí.", 5f);
                break;
            case SAL_Interactable.Kind.Drawings:
                Tell("Pétalos de vitral. Venas violetas. Firma: ||·. Otras plantas, grises.", 6f);
                break;
            case SAL_Interactable.Kind.DiamondSeal:
                SAL_Progress.SetFlag(SAL_Progress.DiamondSeal);
                Tell("Sello ◆ descubierto. Quedó en el Cuaderno.");
                break;
            case SAL_Interactable.Kind.FilterLabel:
                if (SAL_Progress.HasFlag(SAL_Progress.Revealed) || SAL_Progress.HasFlag(SAL_Progress.WarningInspected))
                    Tell("Bajo la etiqueta: BARRERA BIOLÓGICA. NO RETIRAR.", 5f);
                else
                {
                    SAL_Progress.SetFlag(SAL_Progress.LabelRead);
                    SAL_Progress.SetFlag(SAL_Progress.WarningInspected);
                    Tell("N7 · Filtro 14: cambiar. Hay letras bajo la etiqueta.", 5f);
                }
                break;
            case SAL_Interactable.Kind.Valve:
                if (!CORE_InventoryBridge.HasItem("Pelota")) { Tell("No agarra. Necesito la Pelota."); break; }
                SAL_Progress.SetFlag(SAL_Progress.GrilleOpen);
                Tell("La Pelota hace de agarradero. Rejilla abierta.");
                break;
            case SAL_Interactable.Kind.Filter:
                SAL_Progress.SetFlag(SAL_Progress.FilterRemoved);
                CORE_InventoryBridge.AddItem("PiezaBruma");
                SAL_AudioDirector.Instance?.OnFilterRemoved();
                Tell("Pieza Bruma obtenida.");
                break;
            case SAL_Interactable.Kind.PanelTriangle: EnterSymbol('▲'); break;
            case SAL_Interactable.Kind.PanelDiamond: EnterSymbol('◆'); break;
            case SAL_Interactable.Kind.PanelCross: EnterSymbol('✚'); break;
            case SAL_Interactable.Kind.Block: ChooseBlock(target.blockIndex); break;
        }
    }

    private void EnterSymbol(char symbol)
    {
        if (PanelCoolingDown) return;
        if (!SAL_Progress.HasFlag(SAL_Progress.TriangleSeal) ||
            !SAL_Progress.HasFlag(SAL_Progress.DiamondSeal) ||
            !SAL_Progress.HasFlag(SAL_Progress.CrossSeal))
        {
            Tell("Faltan sellos de la ronda: ▲ Laboratorio, ◆ Sala, ✚ Bodega.");
            return;
        }
        panelInput += symbol;
        if (panelInput.Length < 3) { Tell("Panel: " + panelInput); return; }
        if (panelInput == "▲◆✚")
        {
            SAL_Progress.SetFlag(SAL_Progress.PanelSolved);
            Tell("N5 · El orden de la ronda abrió el compactador.");
        }
        else
        {
            PanelCoolingDown = true;
            Tell("Orden incorrecto. El panel se reinicia en 3 segundos.");
            if (panelRoutine != null) StopCoroutine(panelRoutine);
            panelRoutine = StartCoroutine(ResetPanel());
        }
    }

    private IEnumerator ResetPanel()
    {
        yield return new WaitForSeconds(3f);
        panelInput = string.Empty;
        PanelCoolingDown = false;
        panelRoutine = null;
        Tell("Panel listo. Recuerda el orden de la ronda.");
    }

    private void ChooseBlock(int index)
    {
        if (index != 3) { Tell("El bloque no lleva mi firma. Debo mirar los otros."); return; }
        SAL_Progress.SetFlag(SAL_Progress.SeedObtained);
        CORE_InventoryBridge.AddItem("Semilla");
        if (SAL_Progress.Act < 5) SAL_Progress.SetAct(5);
        Tell("Semilla obtenida. N9 · Aquí. No la sueltes.", 6f);
    }

    public void Tell(string message, float seconds = 4f)
    {
        Message = message;
        MessageUntil = Time.unscaledTime + seconds;
    }

    public string GetNote(string id)
    {
        if (notes == null) return string.Empty;
        foreach (SAL_NoteData note in notes)
        {
            if (!note || note.id != id) continue;
            return SAL_Progress.HasFlag(SAL_Progress.Revealed) && !string.IsNullOrEmpty(note.revealedText)
                ? note.visibleText + "  /  Tachado: " + note.revealedText
                : note.visibleText + (string.IsNullOrEmpty(note.revealedText) ? string.Empty : "  [tachado ilegible]");
        }
        return string.Empty;
    }

    private void Refresh()
    {
        bool found = SAL_Progress.HasFlag(SAL_Progress.DrawingsFound);
        if (dust) dust.SetActive(!found);
        if (drawings) drawings.SetActive(found);
        bool seal = SAL_Progress.HasFlag(SAL_Progress.DiamondSeal);
        if (sealCover) sealCover.SetActive(!seal);
        if (diamondSeal) diamondSeal.SetActive(seal);
        bool open = SAL_Progress.HasFlag(SAL_Progress.GrilleOpen);
        if (grille) grille.SetActive(!open);
        bool removed = SAL_Progress.HasFlag(SAL_Progress.FilterRemoved);
        if (filter) filter.SetActive(!removed);
        if (originalWarning) originalWarning.SetActive(SAL_Progress.HasFlag(SAL_Progress.WarningInspected) || SAL_Progress.HasFlag(SAL_Progress.Revealed));
        if (led) led.SetActive(!removed);
        if (ledLight) ledLight.enabled = !removed;
        bool solved = SAL_Progress.HasFlag(SAL_Progress.PanelSolved);
        if (blocks != null) foreach (GameObject block in blocks) if (block) block.SetActive(solved && !SAL_Progress.HasFlag(SAL_Progress.SeedObtained));
    }
}
