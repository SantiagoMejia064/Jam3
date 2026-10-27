using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>Gestor general de la escena Invernadero (Persona 5). Controla el umbral A4, audio ambiental y de Vega, posición del jugador y la cámara, giro A/D de Siete, preservación de INV_Gameplay y el marchitamiento a negro de las hojas por esporas.</summary>
public sealed class INV_RoomController : MonoBehaviour
{
    public const string FlagUmbralA4 = "INV_UmbralA4Played";

    private static readonly string[] TargetLeafMaterialNames =
    {
        "inv_hoja_clara",
        "inv_hoja_oscura",
        "inv_hoja_salvia"
    };

    public static INV_RoomController Instance { get; private set; }

    [Header("Posición del Jugador")]
    [Tooltip("Detecta automáticamente la posición en la que dejaste a CORE_Siete en la escena")]
    [SerializeField] private bool autoDetectScenePosition = true;
    [SerializeField] private Vector3 fallbackPlayerPosition = new Vector3(-13.5714f, 2.3f, 16.48871f);

    [Header("Preservación de Cámara")]
    [Tooltip("Conserva automáticamente la posición y encuadre en la que dejaste INV_Camara_25D")]
    [SerializeField] private bool autoPreserveCamera = true;

    [Header("Configuración de Entrada (Umbral A4 de Vega)")]
    [SerializeField] private bool autoTriggerA4 = true;
    [SerializeField] private float a4SubtitleDuration = 4.5f;

    [Header("Marchitamiento de Hojas (Alerta de Esporas)")]
    [SerializeField] private GameObject[] greenPlantsToGray;
    [SerializeField] private Material grayPlantMaterial;
    [SerializeField] private float leafWitherDuration = 5.8f;
    [SerializeField] private Color witheredBlackColor = new Color(0.022f, 0.022f, 0.026f, 1f);

    [Header("Audio del Invernadero (AudioSources hijos en Hierarchy)")]
    [SerializeField] private AudioSource ambienteInvernaderoSource;
    [SerializeField] private AudioSource vegaEntradaA4Source;
    [SerializeField] private AudioSource marchitamientoPlantasSource;

    [Header("Clips Opcionales (o arrástralos directo en cada AudioSource)")]
    [SerializeField] private AudioClip clipAmbienteInvernadero;
    [SerializeField] private AudioClip clipVegaEntradaA4;
    [SerializeField] private AudioClip clipMarchitamientoPlantas;

    [Header("Pruebas en Editor")]
    [SerializeField] private bool injectTestItemsIfMissing = false;

    private sealed class LeafMaterialCloneEntry
    {
        public Material runtimeClone;
        public Color initialColor;
        public float initialSmoothness;
    }

    private readonly Dictionary<Material, LeafMaterialCloneEntry> leafMaterialClones = new Dictionary<Material, LeafMaterialCloneEntry>();
    private Coroutine witherRoutine;
    private bool witherPrepared = false;

    private CORE_PlayerController activePlayer;
    private FieldInfo fixedZField;
    private FieldInfo movRobotClipField;
    private FieldInfo movRobotSourceField;
    private Vector3 resolvedPlayerPos;
    private bool hasResolvedPlayerPos = false;

    private CORE_CameraFollow cameraFollow;
    private Vector3 cameraAuthoredPos;
    private Quaternion cameraAuthoredRot;
    private bool hasCameraAuthoredPos = false;
    private FieldInfo camOffsetField;
    private FieldInfo camLimitsField;
    private FieldInfo camTargetField;

    // Diccionario para congelar las posiciones de todos los objetos en INV_Gameplay tal como se dejaron en Scene
    private readonly Dictionary<Transform, Vector3> gameplayAuthoredPositions = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, Quaternion> gameplayAuthoredRotations = new Dictionary<Transform, Quaternion>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        fixedZField = typeof(CORE_PlayerController).GetField("fixedZ", BindingFlags.NonPublic | BindingFlags.Instance);
        movRobotClipField = typeof(CORE_PlayerController).GetField("movimientoRobot", BindingFlags.NonPublic | BindingFlags.Instance);
        movRobotSourceField = typeof(CORE_PlayerController).GetField("movimientoSource", BindingFlags.NonPublic | BindingFlags.Instance);

        EnsureRoomAudioSources();

        // 1. Congelar y preservar absolutamente todo lo que esté dentro de INV_Gameplay
        CaptureGameplayAuthoredPositions();

        // 2. Detectar la posición original de CORE_Siete en la escena de Invernadero
        CaptureSceneAuthoredPositions();

        // 3. Detectar la posición y encuadre original de INV_Camara_25D y garantizar un único AudioListener activo
        CaptureCameraAuthoredPosition();
        EnsureSingleActiveAudioListener();
    }

    private void EnsureRoomAudioSources()
    {
        if (!ambienteInvernaderoSource)
        {
            ambienteInvernaderoSource = INV_SoundHelper.FindOrCreateChildSource(transform, "Audio_Ambiente_Invernadero", 0.32f, loop: true);
        }
        if (!vegaEntradaA4Source)
        {
            vegaEntradaA4Source = INV_SoundHelper.FindOrCreateChildSource(transform, "Audio_Vega_Entrada_A4", 0.90f, loop: false);
        }
        if (!marchitamientoPlantasSource)
        {
            marchitamientoPlantasSource = INV_SoundHelper.FindOrCreateChildSource(transform, "Audio_Marchitamiento_Plantas", 0.80f, loop: false);
        }
    }

    private void EnsureSingleActiveAudioListener()
    {
        Camera targetCam = null;
        if (cameraFollow != null)
        {
            targetCam = cameraFollow.GetComponent<Camera>();
        }
        if (targetCam == null)
        {
            GameObject camGo = GameObject.Find("INV_Camara_25D");
            if (camGo != null) targetCam = camGo.GetComponent<Camera>();
        }
        if (targetCam == null)
        {
            targetCam = Camera.main;
        }

        if (targetCam != null && targetCam.gameObject.activeInHierarchy)
        {
            AudioListener mainListener = targetCam.GetComponent<AudioListener>();
            if (mainListener == null)
            {
                mainListener = targetCam.gameObject.AddComponent<AudioListener>();
            }
            mainListener.enabled = true;

            foreach (AudioListener listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            {
                if (listener != null && listener != mainListener && listener.enabled)
                {
                    listener.enabled = false;
                }
            }
        }
    }

    private void CaptureGameplayAuthoredPositions()
    {
        GameObject gameplayRoot = GameObject.Find("INV_Gameplay");
        if (gameplayRoot != null)
        {
            foreach (Transform t in gameplayRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t != null)
                {
                    if (t.GetComponent<INV_IncubatorCart>() != null || t.GetComponentInParent<INV_IncubatorCart>() != null)
                    {
                        continue;
                    }
                    if (t.GetComponent<INV_EllaGerminationSequence>() != null || t.GetComponentInParent<INV_EllaGerminationSequence>() != null)
                    {
                        continue;
                    }
                    gameplayAuthoredPositions[t] = t.position;
                    gameplayAuthoredRotations[t] = t.rotation;
                }
            }
        }
    }

    private void CaptureSceneAuthoredPositions()
    {
        resolvedPlayerPos = fallbackPlayerPosition;

        if (autoDetectScenePosition)
        {
            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t != null && t.gameObject.scene == gameObject.scene && (t.name == "CORE_Siete" || t.name == "CORE_Siete_SAL"))
                {
                    resolvedPlayerPos = t.position;
                    hasResolvedPlayerPos = true;
                    break;
                }
            }
        }
        else
        {
            hasResolvedPlayerPos = true;
        }
    }

    private void CaptureCameraAuthoredPosition()
    {
        if (!autoPreserveCamera) return;

        var cam = GameObject.Find("INV_Camara_25D");
        if (cam != null)
        {
            cameraFollow = cam.GetComponent<CORE_CameraFollow>();
        }
        if (!cameraFollow)
        {
            cameraFollow = FindFirstObjectByType<CORE_CameraFollow>();
        }

        if (cameraFollow != null)
        {
            cameraAuthoredPos = cameraFollow.transform.position;
            cameraAuthoredRot = cameraFollow.transform.rotation;
            hasCameraAuthoredPos = true;

            camOffsetField = typeof(CORE_CameraFollow).GetField("offset", BindingFlags.NonPublic | BindingFlags.Instance);
            camLimitsField = typeof(CORE_CameraFollow).GetField("xLimits", BindingFlags.NonPublic | BindingFlags.Instance);
            camTargetField = typeof(CORE_CameraFollow).GetField("target", BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }

    private void Start()
    {
        // 1. Asegurar Acto mínimo 6 en Invernadero
        if (SAL_Progress.Act < 6)
        {
            SAL_Progress.SetAct(6);
        }

        // 2. Auto-descubrir plantas si no están asignadas
        if (greenPlantsToGray == null || greenPlantsToGray.Length == 0)
        {
            var list = new List<GameObject>();
            var p1 = GameObject.Find("07_Plantas_Instancias_ROOT");
            var p2 = GameObject.Find("INV_Enredaderas");
            if (p1) list.Add(p1);
            if (p2) list.Add(p2);
            greenPlantsToGray = list.ToArray();
        }

        // 3. Alinear jugador en su posición detectada de la escena y activar giro A/D sin dañar sombras
        LocateAndAlignPlayer();

        // 4. Alinear cámara conservando su encuadre exacto y su AudioListener
        AlignCameraWithPlayer();
        EnsureSingleActiveAudioListener();

        StartCoroutine(SetupPlayerRoutine());

        // 5. Iniciar audio de ambiente del Invernadero
        // Si esta fuente contiene musica, su reproduccion la controla CORE_SceneMusic.
        if (ambienteInvernaderoSource != null && !ambienteInvernaderoSource.GetComponent<CORE_SceneMusic>())
        {
            INV_SoundHelper.PlayLoop(
                ambienteInvernaderoSource,
                clipAmbienteInvernadero,
                INV_SoundHelper.CreateGreenhouseAmbience(),
                0.22f
            );
        }

        // 6. Audio A4 de Vega en el umbral al entrar al Invernadero
        if (autoTriggerA4 && !SAL_Progress.HasFlag(FlagUmbralA4))
        {
            SAL_Progress.SetFlag(FlagUmbralA4);
            StartCoroutine(PlayA4UmbralRoutine());
        }

        // 7. Restaurar estado marchito en negro si ya ocurrió la revelación antes de entrar
        if (SAL_Progress.HasFlag(SAL_Progress.Revealed))
        {
            TriggerSporeWithering(instant: true);
        }
    }

    private void LateUpdate()
    {
        // Asegurar que ningún script secundario ni física mueva jamás los objetos estáticos de INV_Gameplay de donde fueron colocados
        if (gameplayAuthoredPositions.Count > 0)
        {
            foreach (var kvp in gameplayAuthoredPositions)
            {
                if (kvp.Key != null)
                {
                    if (kvp.Key.position != kvp.Value)
                    {
                        kvp.Key.position = kvp.Value;
                        kvp.Key.rotation = gameplayAuthoredRotations[kvp.Key];
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (ambienteInvernaderoSource != null)
        {
            ambienteInvernaderoSource.Stop();
        }

        // Al salir de Invernadero restaurar fixedZ = 0 para Javier / Pasillo
        if (activePlayer != null && fixedZField != null)
        {
            try { fixedZField.SetValue(activePlayer, 0f); }
            catch { }
        }

        // Destruir clones runtime de materiales sin tocar los assets .mat en disco
        foreach (var kvp in leafMaterialClones)
        {
            if (kvp.Value != null && kvp.Value.runtimeClone != null)
            {
                Destroy(kvp.Value.runtimeClone);
            }
        }
        leafMaterialClones.Clear();

        if (Instance == this) Instance = null;
    }

    private IEnumerator SetupPlayerRoutine()
    {
        yield return null;

        LocateAndAlignPlayer();
        AlignCameraWithPlayer();
        EnsureSingleActiveAudioListener();

        if (injectTestItemsIfMissing)
        {
            InjectDebugItems();
        }
    }

    public void LocateAndAlignPlayer()
    {
        CORE_PlayerController foundPlayer = null;
        if (PlayerPersistence.Instance)
        {
            foundPlayer = PlayerPersistence.Instance.GetComponent<CORE_PlayerController>();
        }
        if (!foundPlayer)
        {
            var allActive = FindObjectsByType<CORE_PlayerController>(FindObjectsSortMode.None);
            foreach (var pc in allActive)
            {
                if (pc != null && pc.GetComponent<CapsuleCollider>() != null)
                {
                    foundPlayer = pc;
                    break;
                }
            }
            if (!foundPlayer && allActive.Length > 0)
            {
                foundPlayer = allActive[0];
            }
        }

        // Si probamos Invernadero directamente desde el Editor y CORE_Siete estaba desactivado en la escena, activarlo limpio
        if (!foundPlayer)
        {
            var allIncludingInactive = FindObjectsByType<CORE_PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var pc in allIncludingInactive)
            {
                if (pc != null && pc.GetComponent<CapsuleCollider>() != null)
                {
                    foundPlayer = pc;
                    break;
                }
            }
            if (!foundPlayer && allIncludingInactive.Length > 0)
            {
                foundPlayer = allIncludingInactive[0];
            }

            if (foundPlayer != null && !foundPlayer.gameObject.activeInHierarchy)
            {
                Transform p = foundPlayer.transform;
                while (p != null)
                {
                    if (!p.gameObject.activeSelf)
                    {
                        p.gameObject.SetActive(true);
                    }
                    p = p.parent;
                }
            }
        }

        if (foundPlayer)
        {
            activePlayer = foundPlayer;

            foreach (var mc in activePlayer.GetComponentsInChildren<MeshCollider>(true))
            {
                mc.convex = true;
            }

            SyncPlayerMovementAudio(activePlayer);

            var facing = activePlayer.GetComponent<INV_PlayerFacing>();
            if (facing == null)
            {
                facing = activePlayer.gameObject.AddComponent<INV_PlayerFacing>();
            }
            facing.PrepareRig();

            Vector3 targetPos = hasResolvedPlayerPos ? resolvedPlayerPos : activePlayer.transform.position;
            activePlayer.transform.position = targetPos;

            if (activePlayer.TryGetComponent(out Rigidbody body) && !body.isKinematic)
            {
                body.position = targetPos;
                body.linearVelocity = Vector3.zero;
            }

            if (fixedZField != null)
            {
                fixedZField.SetValue(activePlayer, targetPos.z);
            }
        }
    }

    /// <summary>
    /// Conecta en runtime el AudioSource de CORE_Siete con CORE_PlayerController (sin editar scripts de otras escenas),
    /// permitiendo arrastrar el sonido de pasos tanto en el AudioSource como en Movimiento Robot.
    /// </summary>
    private void SyncPlayerMovementAudio(CORE_PlayerController player)
    {
        if (player == null || movRobotSourceField == null || movRobotClipField == null) return;

        AudioSource existingSource = movRobotSourceField.GetValue(player) as AudioSource;
        AudioSource goSource = player.GetComponent<AudioSource>();

        if (existingSource == null && goSource != null)
        {
            goSource.playOnAwake = false;
            goSource.loop = true;
            goSource.spatialBlend = 0f;
            movRobotSourceField.SetValue(player, goSource);
            existingSource = goSource;
        }

        AudioClip currentMovClip = movRobotClipField.GetValue(player) as AudioClip;
        if (currentMovClip == null && existingSource != null && existingSource.clip != null)
        {
            movRobotClipField.SetValue(player, existingSource.clip);
        }
    }

    public void AlignCameraWithPlayer()
    {
        if (!autoPreserveCamera || cameraFollow == null || !hasCameraAuthoredPos || activePlayer == null) return;

        Vector3 targetPos = activePlayer.transform.position;
        Vector3 exactOffset = cameraAuthoredPos - targetPos;

        camOffsetField?.SetValue(cameraFollow, exactOffset);
        camTargetField?.SetValue(cameraFollow, activePlayer.transform);

        if (camLimitsField != null)
        {
            Vector2 limits = (Vector2)camLimitsField.GetValue(cameraFollow);
            limits.x = Mathf.Min(limits.x, cameraAuthoredPos.x - 35f);
            limits.y = Mathf.Max(limits.y, cameraAuthoredPos.x + 45f);
            camLimitsField.SetValue(cameraFollow, limits);
        }

        cameraFollow.transform.position = cameraAuthoredPos;
        cameraFollow.transform.rotation = cameraAuthoredRot;
    }

    private IEnumerator PlayA4UmbralRoutine()
    {
        yield return new WaitForSeconds(0.6f);

        bool userAssignedLocalA4 = INV_SoundHelper.HasUserAssignedAudio(vegaEntradaA4Source, clipVegaEntradaA4);
        bool directorHasRealClip = SAL_AudioDirector.Instance != null && SAL_AudioDirector.Instance.HasAssignedClip("A4", false);

        // Si asignaron un clip real en el asset SAL_A4 y no en el objeto local del Invernadero, usar el de SAL_AudioDirector
        if (!userAssignedLocalA4 && directorHasRealClip)
        {
            SAL_AudioDirector.Instance.PlayLog("A4", false, a4SubtitleDuration);
            yield break;
        }

        // La voz de Vega se muestra solo en el panel inferior de subtitulos.
        SAL_AudioDirector.EnsureInstance().ShowSubtitle("Vega: \"Siete, no la busques. Por favor.\"", a4SubtitleDuration);

        if (vegaEntradaA4Source != null)
        {
            INV_SoundHelper.PlayEffect(
                vegaEntradaA4Source,
                clipVegaEntradaA4,
                INV_SoundHelper.CreateVegaVoiceTransmission(0),
                0.90f
            );
        }
    }

    /// <summary>Compatibilidad con llamadas existentes: inicia la transición hacia negro de las hojas del invernadero.</summary>
    public void ApplyDesaturation()
    {
        TriggerSporeWithering(instant: false);
    }

    /// <summary>
    /// Clona en runtime los materiales inv_hoja_clara, inv_hoja_oscura e inv_hoja_salvia en toda la escena
    /// (sin modificar los archivos .mat en disco) y los transiciona suavemente hacia negro al soltarse las esporas.
    /// </summary>
    public void TriggerSporeWithering(bool instant = false)
    {
        PrepareLeafMaterialClones();

        // Volver más sombrío el ambiente del invernadero durante la cuarentena
        if (ambienteInvernaderoSource != null)
        {
            ambienteInvernaderoSource.pitch = 0.82f;
        }

        if (!instant && marchitamientoPlantasSource != null)
        {
            INV_SoundHelper.PlayEffect(
                marchitamientoPlantasSource,
                clipMarchitamientoPlantas,
                INV_SoundHelper.CreateSporeWitherSound(),
                0.80f
            );
        }

        if (witherRoutine != null)
        {
            StopCoroutine(witherRoutine);
            witherRoutine = null;
        }

        if (instant || leafWitherDuration <= 0.05f)
        {
            ApplyWitherFactor(1f);
        }
        else
        {
            witherRoutine = StartCoroutine(WitherLeavesToBlackRoutine());
        }
    }

    private void PrepareLeafMaterialClones()
    {
        if (witherPrepared && leafMaterialClones.Count > 0) return;
        witherPrepared = true;

        var allRenderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Renderer r in allRenderers)
        {
            if (r == null) continue;
            if (r.GetComponentInParent<INV_EllaGerminationSequence>() != null) continue;
            if (r.GetComponentInParent<INV_IncubatorCart>() != null) continue;

            Material[] shared = r.sharedMaterials;
            if (shared == null || shared.Length == 0) continue;

            bool changedAny = false;
            for (int i = 0; i < shared.Length; i++)
            {
                Material srcMat = shared[i];
                if (srcMat == null) continue;

                bool alreadyClone = false;
                foreach (var existing in leafMaterialClones.Values)
                {
                    if (existing.runtimeClone == srcMat)
                    {
                        alreadyClone = true;
                        break;
                    }
                }
                if (alreadyClone) continue;

                if (!IsTargetLeafMaterial(srcMat.name)) continue;

                if (!leafMaterialClones.TryGetValue(srcMat, out LeafMaterialCloneEntry entry))
                {
                    Material clone = new Material(srcMat);
                    clone.name = srcMat.name + "_WitherRuntime";

                    Color startCol = clone.HasProperty("_BaseColor")
                        ? clone.GetColor("_BaseColor")
                        : clone.color;
                    float startSmooth = clone.HasProperty("_Smoothness")
                        ? clone.GetFloat("_Smoothness")
                        : 0.28f;

                    entry = new LeafMaterialCloneEntry
                    {
                        runtimeClone = clone,
                        initialColor = startCol,
                        initialSmoothness = startSmooth
                    };
                    leafMaterialClones[srcMat] = entry;
                }

                shared[i] = entry.runtimeClone;
                changedAny = true;
            }

            if (changedAny)
            {
                r.sharedMaterials = shared;
            }
        }
    }

    private static bool IsTargetLeafMaterial(string matName)
    {
        if (string.IsNullOrEmpty(matName)) return false;
        for (int i = 0; i < TargetLeafMaterialNames.Length; i++)
        {
            if (matName.IndexOf(TargetLeafMaterialNames[i], StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }
        return false;
    }

    private IEnumerator WitherLeavesToBlackRoutine()
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.5f, leafWitherDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ApplyWitherFactor(Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        ApplyWitherFactor(1f);
        witherRoutine = null;
    }

    private void ApplyWitherFactor(float t)
    {
        Color necroticMid = new Color(0.11f, 0.09f, 0.06f, 1f);

        foreach (var kvp in leafMaterialClones)
        {
            LeafMaterialCloneEntry entry = kvp.Value;
            if (entry == null || entry.runtimeClone == null) continue;

            Color c;
            if (t < 0.42f)
            {
                c = Color.Lerp(entry.initialColor, necroticMid, t / 0.42f);
            }
            else
            {
                c = Color.Lerp(necroticMid, witheredBlackColor, (t - 0.42f) / 0.58f);
            }
            c.a = entry.initialColor.a;

            entry.runtimeClone.color = c;
            if (entry.runtimeClone.HasProperty("_BaseColor"))
            {
                entry.runtimeClone.SetColor("_BaseColor", c);
            }
            if (entry.runtimeClone.HasProperty("_Color"))
            {
                entry.runtimeClone.SetColor("_Color", c);
            }
            if (entry.runtimeClone.HasProperty("_Smoothness"))
            {
                entry.runtimeClone.SetFloat("_Smoothness", Mathf.Lerp(entry.initialSmoothness, 0.04f, t));
            }
        }
    }

    /// <summary>Permite probar directamente el Invernadero en modo Play del editor.</summary>
    public void InjectDebugItems()
    {
        CORE_InventoryBridge.TryAddItem("Semilla");
        CORE_InventoryBridge.TryAddItem("PiezaCalor");
        CORE_InventoryBridge.TryAddItem("PiezaOscuridad");
        CORE_InventoryBridge.TryAddItem("PiezaPulso");
        CORE_InventoryBridge.TryAddItem("PiezaBruma");
        CORE_InventoryBridge.TryAddItem("Cortador");
    }

    private void Update()
    {
        if (Application.isEditor && Input.GetKeyDown(KeyCode.F5))
        {
            InjectDebugItems();
            Debug.Log("Invernadero: ítems de prueba inyectados (Semilla y 4 Piezas).", this);
        }
    }
}
