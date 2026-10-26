using System.Collections;
using UnityEngine;

/// <summary>Controlador integral de la incubadora: Montaje, Afinación, Floración, Revelación y Extracción al Carrito 3D.</summary>
[RequireComponent(typeof(Collider))]
public sealed class INV_IncubatorPuzzle : MonoBehaviour
{
    public const string FlagMontada = "INV_IncubadoraMontada";
    public const string FlagAfinada = "IncubadoraAfinada";
    public const string FlagFlorecio = "EllaFlorecio";

    [Header("Referencias a UIs")]
    [SerializeField] private INV_IncubatorUI incubatorUI;
    [SerializeField] private INV_RevelationUI revelationUI;

    [Header("Efectos Visuales de Floración")]
    [SerializeField] private GameObject ellaSproutVisual;
    [SerializeField] private Light violetChamberLight;
    [SerializeField] private Light redAlarmLight;
    [SerializeField] private GameObject incubatorCartVisual;

    [Header("Carrito Portátil 3D")]
    [SerializeField] private INV_IncubatorCart portableCart;

    private CORE_PlayerController activePlayer;
    private bool inRange;
    private bool isMounting = false;
    private AudioSource audioSource;
    private bool triggerAlarmNow = false;

    private AudioClip heatHum;
    private AudioClip darkClick;
    private AudioClip stepPulse;
    private AudioClip mistHiss;
    private AudioClip bloomHarmonic;
    private AudioClip alarmSiren;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (!incubatorUI)
        {
            incubatorUI = FindFirstObjectByType<INV_IncubatorUI>(FindObjectsInactive.Include)
                          ?? gameObject.AddComponent<INV_IncubatorUI>();
        }
        if (!revelationUI)
        {
            revelationUI = FindFirstObjectByType<INV_RevelationUI>(FindObjectsInactive.Include)
                           ?? gameObject.AddComponent<INV_RevelationUI>();
        }

        if (!portableCart)
        {
            portableCart = FindFirstObjectByType<INV_IncubatorCart>(FindObjectsInactive.Include);
        }

        // Mantener oculto el cubo placeholder antiguo si existiera en la escena
        if (incubatorCartVisual)
        {
            incubatorCartVisual.SetActive(false);
        }

        heatHum = INV_SoundHelper.CreateHeatHum();
        darkClick = INV_SoundHelper.CreateDarkClick();
        stepPulse = INV_SoundHelper.CreateStepPulse();
        mistHiss = INV_SoundHelper.CreateMistHiss();
        bloomHarmonic = INV_SoundHelper.CreateBloomHarmonic();
        alarmSiren = INV_SoundHelper.CreateAlarmSiren();

        GetComponent<Collider>().isTrigger = true;

        EnsureProceduralVisuals();
    }

    private void Start()
    {
        bool florecio = SAL_Progress.HasFlag(FlagFlorecio);
        bool revelado = SAL_Progress.HasFlag(SAL_Progress.Revealed);
        bool enCarrito = SAL_Progress.HasFlag(INV_IncubatorCart.FlagEllaEnCarrito);

        if (ellaSproutVisual) ellaSproutVisual.SetActive(florecio && !enCarrito);
        if (violetChamberLight) violetChamberLight.enabled = florecio && !enCarrito;
        if (redAlarmLight) redAlarmLight.enabled = revelado;
        if (incubatorCartVisual) incubatorCartVisual.SetActive(false);

        EnsurePortableCartExists();
    }

    private void EnsurePortableCartExists()
    {
        if (portableCart != null) return;

        portableCart = INV_IncubatorCart.ActiveAttachedInstance
                       ?? FindFirstObjectByType<INV_IncubatorCart>(FindObjectsInactive.Include);

        if (portableCart == null)
        {
            GameObject cartGo = new GameObject("INV_Carrito_Incubadora");
            cartGo.transform.position = new Vector3(transform.position.x + 1.33f, 1.45f, 16.48871f);
            portableCart = cartGo.AddComponent<INV_IncubatorCart>();
        }
    }

    private void EnsureProceduralVisuals()
    {
        // 1. Luz de cámara violeta si no fue asignada
        if (!violetChamberLight)
        {
            Transform existingLight = transform.Find("Luz_Violeta_Chamber");
            if (existingLight)
            {
                violetChamberLight = existingLight.GetComponent<Light>();
            }
            else
            {
                GameObject lightGo = new GameObject("Luz_Violeta_Chamber");
                lightGo.transform.SetParent(transform, false);
                lightGo.transform.localPosition = new Vector3(0, 1.2f, 0);
                violetChamberLight = lightGo.AddComponent<Light>();
                violetChamberLight.type = LightType.Point;
                violetChamberLight.color = new Color(0.72f, 0.35f, 0.98f);
                violetChamberLight.range = 5f;
                violetChamberLight.intensity = 4.5f;
                violetChamberLight.enabled = false;
            }
        }

        // 2. Brote de Ella si no fue asignado
        if (!ellaSproutVisual)
        {
            Transform existingSprout = transform.Find("INV_Brote_Ella_Visual");
            if (existingSprout)
            {
                ellaSproutVisual = existingSprout.gameObject;
            }
            else
            {
                GameObject sproutRoot = new GameObject("INV_Brote_Ella_Visual");
                sproutRoot.transform.SetParent(transform, false);
                sproutRoot.transform.localPosition = new Vector3(0, 0.6f, 0);

                // Tallo central
                GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = "Tallo_Ella";
                stem.transform.SetParent(sproutRoot.transform, false);
                stem.transform.localPosition = new Vector3(0, 0.3f, 0);
                stem.transform.localScale = new Vector3(0.06f, 0.3f, 0.06f);
                DestroyImmediate(stem.GetComponent<Collider>());
                SetProceduralColor(stem, new Color(0.35f, 0.75f, 0.55f), true);

                // Flores colgantes tipo farolito
                for (int i = 0; i < 3; i++)
                {
                    float angle = i * 120f * Mathf.Deg2Rad;
                    GameObject flower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    flower.name = "Farolito_" + i;
                    flower.transform.SetParent(sproutRoot.transform, false);
                    flower.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.18f, 0.45f, Mathf.Sin(angle) * 0.18f);
                    flower.transform.localScale = new Vector3(0.12f, 0.16f, 0.12f);
                    DestroyImmediate(flower.GetComponent<Collider>());
                    SetProceduralColor(flower, new Color(0.78f, 0.42f, 1f), true);
                }

                sproutRoot.SetActive(false);
                ellaSproutVisual = sproutRoot;
            }
        }

        // 3. Luz de alarma roja si no fue asignada
        if (!redAlarmLight)
        {
            Transform existingAlarm = transform.Find("Luz_Alarma_Roja");
            if (existingAlarm)
            {
                redAlarmLight = existingAlarm.GetComponent<Light>();
            }
            else
            {
                GameObject alarmGo = new GameObject("Luz_Alarma_Roja");
                alarmGo.transform.SetParent(transform, false);
                alarmGo.transform.localPosition = new Vector3(0, 3f, 0);
                redAlarmLight = alarmGo.AddComponent<Light>();
                redAlarmLight.type = LightType.Point;
                redAlarmLight.color = new Color(1f, 0.15f, 0.15f);
                redAlarmLight.range = 9f;
                redAlarmLight.intensity = 3.5f;
                redAlarmLight.enabled = false;
            }
        }
    }

    private static void SetProceduralColor(GameObject go, Color color, bool emissive)
    {
        var renderer = go.GetComponent<MeshRenderer>();
        if (!renderer) return;
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material m = new Material(s);
        m.color = color;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (emissive)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * 1.4f);
        }
        renderer.sharedMaterial = m;
    }

    private void SetPlayerMovement(bool canMove)
    {
        var controllers = FindObjectsByType<CORE_PlayerController>(FindObjectsSortMode.None);
        foreach (var c in controllers)
        {
            if (c != null)
            {
                c.enabled = canMove;
                if (c.TryGetComponent(out Rigidbody rb) && !rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var pc = other.GetComponentInParent<CORE_PlayerController>();
        if (pc != null)
        {
            activePlayer = pc;
            inRange = true;
            UpdatePrompt();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var pc = other.GetComponentInParent<CORE_PlayerController>();
        if (pc != null)
        {
            inRange = false;
            CORE_InteractionHUD.ClearPrompt(this);
        }
    }

    private void OnDisable()
    {
        inRange = false;
        CORE_InteractionHUD.ClearPrompt(this);
    }

    private void UpdatePrompt()
    {
        if (!inRange) return;

        if (SAL_Progress.HasFlag(INV_IncubatorCart.FlagEllaEnCarrito))
        {
            CORE_InteractionHUD.SetPrompt(this, "Lleva a Ella en el carrito hacia la puerta del Pasillo");
        }
        else if (SAL_Progress.HasFlag(SAL_Progress.Revealed))
        {
            CORE_InteractionHUD.SetPrompt(this, "E · Sacar a Ella en el carrito");
        }
        else if (SAL_Progress.HasFlag(FlagFlorecio))
        {
            CORE_InteractionHUD.SetPrompt(this, "E · Contemplar el brote de Ella");
        }
        else if (SAL_Progress.HasFlag(FlagMontada))
        {
            CORE_InteractionHUD.SetPrompt(this, "E · Afinar incubadora");
        }
        else if (isMounting)
        {
            CORE_InteractionHUD.SetPrompt(this, "Instalando piezas...");
        }
        else
        {
            CORE_InteractionHUD.SetPrompt(this, "E · Montar piezas de la incubadora");
        }
    }

    private void Update()
    {
        if (!inRange || activePlayer == null) return;
        if (isMounting) return;
        if (incubatorUI && incubatorUI.IsOpen) return;
        if (revelationUI && revelationUI.IsShowing) return;
        if (NotebookController.Instance && NotebookController.Instance.IsOpen) return;

        if (activePlayer.InteractPressed)
        {
            Interact();
        }
    }

    public void Interact()
    {
        if (SAL_Progress.HasFlag(INV_IncubatorCart.FlagEllaEnCarrito))
        {
            CORE_InteractionHUD.ShowMessage("Ella ya va contigo en el carrito portátil.\nCamina hacia la puerta del Pasillo para llegar a la esclusa.", 3.5f);
            return;
        }

        if (SAL_Progress.HasFlag(SAL_Progress.Revealed))
        {
            ExtractEllaToCart();
            return;
        }

        if (SAL_Progress.HasFlag(FlagFlorecio))
        {
            triggerAlarmNow = true;
            return;
        }

        if (!SAL_Progress.HasFlag(FlagMontada))
        {
            TryMountPieces();
        }
        else if (!SAL_Progress.HasFlag(FlagAfinada))
        {
            OpenTuningUI();
        }
    }

    private void ExtractEllaToCart()
    {
        EnsurePortableCartExists();

        // Retirar a Ella del marco fijo y colocarla dentro de la cápsula del carrito 3D frente a Siete
        if (ellaSproutVisual) ellaSproutVisual.SetActive(false);
        if (violetChamberLight) violetChamberLight.enabled = false;

        if (portableCart != null)
        {
            portableCart.DeployInFrontOfPlayer(activePlayer, playSound: true);
        }

        UpdatePrompt();

        string doorDirectionHint = GetExitDoorDirectionHint();
        CORE_InteractionHUD.ShowMessage(
            $"<color=#C084FC><b>Ella está en el carrito portátil frente a ti.</b></color>\n" +
            $"Muévete con <b>[A]</b> y <b>[D]</b> para llevarla hacia la puerta del Pasillo ({doorDirectionHint}).",
            5.5f
        );
    }

    private string GetExitDoorDirectionHint()
    {
        GameObject exitDoor = GameObject.Find("INV_Puerta_Pasillo");
        if (exitDoor != null)
        {
            return exitDoor.transform.position.x >= transform.position.x ? "a la derecha" : "a la izquierda";
        }
        return "salida al Pasillo";
    }

    private void TryMountPieces()
    {
        if (isMounting) return;

        bool hasSeed = CORE_InventoryBridge.HasItem("Semilla") || CORE_InventoryBridge.WasCollected("Semilla") || SAL_Progress.HasFlag(SAL_Progress.SeedObtained);
        bool hasCalor = CORE_InventoryBridge.HasItem("PiezaCalor") || CORE_InventoryBridge.WasCollected("PiezaCalor");
        bool hasOscuridad = CORE_InventoryBridge.HasItem("PiezaOscuridad") || CORE_InventoryBridge.WasCollected("PiezaOscuridad");
        bool hasPulso = CORE_InventoryBridge.HasItem("PiezaPulso") || CORE_InventoryBridge.WasCollected("PiezaPulso");
        bool hasBruma = CORE_InventoryBridge.HasItem("PiezaBruma") || CORE_InventoryBridge.WasCollected("PiezaBruma");

        if (!hasSeed || !hasCalor || !hasOscuridad || !hasPulso || !hasBruma)
        {
            CORE_InteractionHUD.ShowMessage("El marco de la incubadora está incompleto.\nNecesita la Semilla y las 4 piezas (Calor, Oscuridad, Pulso y Bruma).", 5f);
            return;
        }

        StartCoroutine(MountSequenceRoutine());
    }

    private IEnumerator MountSequenceRoutine()
    {
        isMounting = true;
        UpdatePrompt();

        CORE_InteractionHUD.ShowMessage("Instalando la Semilla y las 4 piezas en el marco...", 2.2f);

        // Sonidos secuenciales según el GDD (zumbido, clic, tum-tum, siseo) sin congelar a Siete
        if (audioSource && heatHum) audioSource.PlayOneShot(heatHum, 0.7f);
        yield return new WaitForSeconds(0.32f);

        if (audioSource && darkClick) audioSource.PlayOneShot(darkClick, 0.8f);
        yield return new WaitForSeconds(0.32f);

        if (audioSource && stepPulse) audioSource.PlayOneShot(stepPulse, 0.75f);
        yield return new WaitForSeconds(0.32f);

        if (audioSource && mistHiss) audioSource.PlayOneShot(mistHiss, 0.7f);
        yield return new WaitForSeconds(0.32f);

        // Consumir objetos del inventario
        CORE_InventoryBridge.RemoveItem("Semilla");
        CORE_InventoryBridge.RemoveItem("PiezaCalor");
        CORE_InventoryBridge.RemoveItem("PiezaOscuridad");
        CORE_InventoryBridge.RemoveItem("PiezaPulso");
        CORE_InventoryBridge.RemoveItem("PiezaBruma");

        SAL_Progress.SetFlag(FlagMontada);
        isMounting = false;
        UpdatePrompt();

        // Abrir inmediatamente el panel de afinación de las 4 perillas
        OpenTuningUI();
    }

    private void OpenTuningUI()
    {
        if (!incubatorUI)
        {
            incubatorUI = FindFirstObjectByType<INV_IncubatorUI>(FindObjectsInactive.Include)
                          ?? gameObject.AddComponent<INV_IncubatorUI>();
        }

        if (incubatorUI)
        {
            incubatorUI.Open(OnTuningSuccess);
        }
    }

    private void OnTuningSuccess()
    {
        SAL_Progress.SetFlag(FlagAfinada);
        StartCoroutine(FloracionSequenceRoutine());
    }

    private IEnumerator FloracionSequenceRoutine()
    {
        // 1. Floración (Acto 7)
        SAL_Progress.SetAct(7);
        SAL_Progress.SetFlag(FlagFlorecio);

        // Movimiento activo: Siete NUNCA queda congelado
        SetPlayerMovement(true);

        if (audioSource && bloomHarmonic) audioSource.PlayOneShot(bloomHarmonic, 0.95f);

        if (ellaSproutVisual) ellaSproutVisual.SetActive(true);
        if (violetChamberLight)
        {
            violetChamberLight.enabled = true;
            violetChamberLight.color = new Color(0.72f, 0.35f, 0.98f);
            violetChamberLight.intensity = 4.5f;
        }

        CORE_InteractionHUD.ShowMessage("<color=#C084FC><b>¡Ella brota!</b></color>\nVenas azul violeta resplandecen y las flores cuelgan como farolitos.", 6f);
        UpdatePrompt();

        // Permitir que el jugador admire la floración libremente por 6s (o interactúe con Ella)
        float timer = 6.0f;
        while (timer > 0f)
        {
            if (triggerAlarmNow) break;
            timer -= Time.deltaTime;
            yield return null;
        }

        // Transición a la Revelación (Acto 8)
        yield return StartCoroutine(AlarmaAndRevelationRoutine());
    }

    private IEnumerator AlarmaAndRevelationRoutine()
    {
        // 2. Revelación (Acto 8)
        SAL_Progress.SetAct(8);
        SAL_Progress.SetFlag(SAL_Progress.Revealed);

        // Desbloquear la esclusa final en el Pasillo (Javier)
        SAL_Progress.UnlockFinal();

        if (audioSource && alarmSiren)
        {
            audioSource.clip = alarmSiren;
            audioSource.loop = true;
            audioSource.Play();
        }

        if (redAlarmLight) redAlarmLight.enabled = true;

        // Desaturar plantas a gris (GDD pág 10 & 15)
        INV_RoomController.Instance?.ApplyDesaturation();

        EnsurePortableCartExists();

        // SIETE NUNCA SE QUEDA QUIETO: Movimiento 100% activo y fluido
        SetPlayerMovement(true);
        UpdatePrompt();

        string doorDirectionHint = GetExitDoorDirectionHint();

        // Diálogos narrativos de Vega en el HUD mientras juegas libremente
        CORE_InteractionHUD.ShowMessage("<color=#EF4444><b>[¡ALARMA BIOLÓGICA DE ESPORAS!]</b></color>\nVega: \"Siete... ¿qué hiciste? ¡Las puertas selladas eran cuarentena biológica!\"", 5.5f);
        yield return new WaitForSeconds(4.5f);

        CORE_InteractionHUD.ShowMessage("Vega: \"Quitaste el filtro 14. Mi aire pasa por ahí... Se parece a lo que mató las semillas de la Tierra...\"", 5.5f);
        yield return new WaitForSeconds(4.5f);

        CORE_InteractionHUD.ShowMessage("Vega: \"Suéltala, Siete. Por favor... Llévala a la esclusa de carga.\"", 5f);
        yield return new WaitForSeconds(3.5f);

        if (!SAL_Progress.HasFlag(INV_IncubatorCart.FlagEllaEnCarrito))
        {
            CORE_InteractionHUD.ShowMessage($"<color=#38BDF8><b>[OBJETIVO]</b></color> Pulsa <b>[E]</b> en la incubadora para sacar a Ella en el carrito y llévala al Pasillo ({doorDirectionHint}).", 6.5f);
        }
    }
}
