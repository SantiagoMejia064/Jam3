using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Componente de pruebas para la Escena 5 (Invernadero).
/// Inyecta automáticamente el inventario completo, notas del Cuaderno, sellos y banderas de progreso del GDD
/// para poder probar el Core Loop del Invernadero directamente sin jugar las escenas previas.
/// </summary>
public sealed class INV_TestingSetup : MonoBehaviour
{
    [Header("Configuración de Prueba")]
    [Tooltip("Inyecta automáticamente todos los ítems y estados al arrancar la escena")]
    [SerializeField] private bool autoInjectOnStart = true;
    [Tooltip("Asegura que el prefab de inventario CORE_Inventario esté instanciado")]
    [SerializeField] private bool ensureInventoryPrefab = true;

    [Header("Ítems de Escena 5 (GDD)")]
    [SerializeField] private bool injectSemilla = true;
    [SerializeField] private bool injectPiezaCalor = true;
    [SerializeField] private bool injectPiezaOscuridad = true;
    [SerializeField] private bool injectPiezaPulso = true;
    [SerializeField] private bool injectPiezaBruma = true;
    [SerializeField] private bool injectCortador = true;
    [SerializeField] private bool injectPelota = true;
    [SerializeField] private bool injectCable = true;

    [Header("Progreso de Historia (GDD)")]
    [SerializeField] private bool setAct6 = true;
    [SerializeField] private bool setStoryFlags = true;
    [SerializeField] private bool unlockPreviousNotes = true;
    [SerializeField] private bool unlockRoundStamps = true;

    [Header("Atajos Opcionales de Teclado (Desactivados por defecto)")]
    [Tooltip("Sólo si se activa: F2 reinyecta ítems, F3 monta piezas, etc.")]
    [SerializeField] private bool enableKeyShortcuts = false;

    private void Awake()
    {
        if (autoInjectOnStart)
        {
            EnsureInventorySystem();
        }
    }

    private void Start()
    {
        if (autoInjectOnStart)
        {
            StartCoroutine(InitializeTestingRoutine());
        }
    }

    private IEnumerator InitializeTestingRoutine()
    {
        // Esperar un frame a que todos los Singletons (NotebookController, Inventory, etc.) despierten
        yield return null;

        InjectAllForScene5();

        CORE_InteractionHUD.ShowMessage(
            "<color=#38BDF8><b>[ESCENA 5 - INVERNADERO PREPARADA]</b></color>\n" +
            "Tienes la Semilla y las 4 Piezas en tu inventario.\n" +
            "Explora libremente: lee la mesa con [E], examina la maceta y monta la incubadora.",
            5.5f
        );
    }

    /// <summary>Garantiza que el sistema de inventario esté instanciado en la escena si no venimos de Javier.</summary>
    public void EnsureInventorySystem()
    {
        if (ensureInventoryPrefab && CORE_Inventory.Instance == null)
        {
            GameObject prefab = Resources.Load<GameObject>("CORE_Inventario");
            if (prefab != null)
            {
                Instantiate(prefab);
            }
        }
    }

    [ContextMenu("Inyectar Todo para Escena 5")]
    public void InjectAllForScene5()
    {
        EnsureInventorySystem();

        // 1. Inyectar Ítems al inventario
        InjectInventoryItems();

        // 2. Acto y Banderas según GDD
        if (setAct6)
        {
            SAL_Progress.SetAct(6);
        }

        if (setStoryFlags)
        {
            SAL_Progress.SetFlag(SAL_Progress.SeedObtained, true);
            SAL_Progress.SetFlag("InvernaderoAbierto", true);
            SAL_Progress.SetFlag("BodegaAbierta", true);
            SAL_Progress.SetFlag(SAL_Progress.DrawingsFound, true);
            SAL_Progress.SetFlag(SAL_Progress.DiamondSeal, true);
            SAL_Progress.SetFlag(SAL_Progress.TriangleSeal, true);
            SAL_Progress.SetFlag(SAL_Progress.CrossSeal, true);
            SAL_Progress.SetFlag(SAL_Progress.GrilleOpen, true);
            SAL_Progress.SetFlag(SAL_Progress.LabelRead, true);
            SAL_Progress.SetFlag(SAL_Progress.FilterRemoved, true);
            SAL_Progress.SetFlag(SAL_Progress.PanelSolved, true);
            SAL_Progress.SetFlag("BOD_CamaraAbierta", true);
        }

        // 3. Cuaderno: Desbloquear notas previas (N1-N7, N9, N10)
        if (unlockPreviousNotes && NotebookController.Instance != null)
        {
            PopulatePreviousNotes();
        }
    }

    [ContextMenu("Reinyectar Sólo Ítems de Inventario")]
    public void InjectInventoryItems()
    {
        EnsureInventorySystem();

        if (CORE_Inventory.Instance != null)
        {
            // Permitir que los ítems se añadan limpiando su estado de recolectado si ya estaban
            List<string> itemsToGive = new List<string>();
            if (injectSemilla) itemsToGive.Add("Semilla");
            if (injectPiezaCalor) itemsToGive.Add("PiezaCalor");
            if (injectPiezaOscuridad) itemsToGive.Add("PiezaOscuridad");
            if (injectPiezaPulso) itemsToGive.Add("PiezaPulso");
            if (injectPiezaBruma) itemsToGive.Add("PiezaBruma");
            if (injectCortador) itemsToGive.Add("Cortador");
            if (injectPelota) itemsToGive.Add("Pelota");
            if (injectCable) itemsToGive.Add("Cable");

            foreach (string id in itemsToGive)
            {
                if (!CORE_InventoryBridge.HasItem(id))
                {
                    CORE_InventoryBridge.TryAddItem(id);
                }
            }
        }
    }

    private void PopulatePreviousNotes()
    {
        var nb = NotebookController.Instance;
        if (!nb) return;

        // Notas previas del GDD:
        nb.CollectNote(0, "N1 · Registro del reactor", "Bitácora inicial de Vega sobre el reactor y anomalías en la nave.");
        nb.CollectNote(1, "N2 · Ruta de escape", "Esquema del casillero en el Taller indicando rutas de servicio.");
        nb.CollectNote(2, "N3 · Diagrama del motor", "Instrucciones de cableado y prueba de la pulidora.");
        nb.CollectNote(3, "N4 · Placa de la Bodega Fría", "4 bandejas en gradiente de oscuridad: 7294 abre la cámara de Calor.");
        nb.CollectNote(4, "N5 · Combinación de sellos", "Secuencia de consola ▲◆✚ registrada tras verificar los sellos de ronda.");
        nb.CollectNote(5, "N6 · Dibujos infantiles", "Bosquejos de Vega representando a Ella brotando.");
        nb.CollectNote(6, "N7 · Etiqueta del filtro", "Especificaciones del sistema de ventilación y advertencias de condensación.");
        // Nota 7 (N8) queda bloqueada intencionalmente para que Siete la recoja de la mesa en el Invernadero!
        nb.CollectNote(8, "N9 · Secuencia de extracción", "Firma ||· bloque de índice 3 entrega la Semilla.");
        nb.CollectNote(9, "N10 · Registro del cargador", "Vega no sabe que hay semillas. Yo sí.");

        // Sellos de ronda
        if (unlockRoundStamps)
        {
            SAL_Progress.SetFlag(SAL_Progress.TriangleSeal, true);
            SAL_Progress.SetFlag(SAL_Progress.DiamondSeal, true);
            SAL_Progress.SetFlag(SAL_Progress.CrossSeal, true);
        }
    }

    private void Update()
    {
        if (!enableKeyShortcuts) return;

        // [F1] Ayuda en pantalla
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ShowHelpMessage();
        }

        // [F2] Reinyectar ítems
        if (Input.GetKeyDown(KeyCode.F2))
        {
            InjectInventoryItems();
            CORE_InteractionHUD.ShowMessage("<color=#38BDF8>[F2] Ítems reinyectados en el inventario.</color>", 3f);
        }

        // [F3] Montar piezas instantáneamente en la incubadora
        if (Input.GetKeyDown(KeyCode.F3))
        {
            SAL_Progress.SetFlag(INV_IncubatorPuzzle.FlagMontada, true);
            CORE_InteractionHUD.ShowMessage("<color=#38BDF8>[F3] Piezas montadas en la incubadora. Listo para afinar.</color>", 3f);
        }

        // [F4] Abrir panel de afinación
        if (Input.GetKeyDown(KeyCode.F4))
        {
            var puzzle = FindFirstObjectByType<INV_IncubatorPuzzle>();
            if (puzzle)
            {
                SAL_Progress.SetFlag(INV_IncubatorPuzzle.FlagMontada, true);
                var ui = FindFirstObjectByType<INV_IncubatorUI>(FindObjectsInactive.Include);
                if (ui) ui.Open(null);
            }
        }

        // [F5] Floración instantánea de Ella
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SAL_Progress.SetAct(7);
            SAL_Progress.SetFlag(INV_IncubatorPuzzle.FlagFlorecio, true);
            CORE_InteractionHUD.ShowMessage("<color=#C084FC>[F5] Ella ha florecido (Luz violeta activa).</color>", 3f);
        }

        // [F6] Revelación / Alarma de esporas
        if (Input.GetKeyDown(KeyCode.F6))
        {
            SAL_Progress.SetAct(8);
            SAL_Progress.SetFlag(SAL_Progress.Revealed, true);
            INV_RoomController.Instance?.ApplyDesaturation();
            var revUI = FindFirstObjectByType<INV_RevelationUI>(FindObjectsInactive.Include);
            if (revUI) revUI.StartSequence(null);
            CORE_InteractionHUD.ShowMessage("<color=#EF4444>[F6] Alarma activada y tarjetas de revelación mostradas.</color>", 3f);
        }
    }

    private void ShowHelpMessage()
    {
        CORE_InteractionHUD.ShowMessage(
            "<b>ATAJOS DE TESTING (ESCENA 5):</b>\n" +
            "• <b>[F2]</b> Reinyectar Semilla + 4 Piezas + Cortador\n" +
            "• <b>[F3]</b> Montar piezas en la incubadora\n" +
            "• <b>[F4]</b> Abrir UI de afinación de perillas\n" +
            "• <b>[F5]</b> Forzar floración de Ella (Acto 7, Luz violeta)\n" +
            "• <b>[F6]</b> Forzar revelación (Acto 8, Alarma y tarjetas)\n" +
            "• <b>[Tab]</b> Abrir Cuaderno con notas y sellos",
            8f
        );
    }
}
