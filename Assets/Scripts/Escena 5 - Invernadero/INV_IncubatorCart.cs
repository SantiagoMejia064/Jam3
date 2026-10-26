using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Modelo 3D procedural y controlador del Carrito Portátil de Incubación.
/// Al extraer a Ella tras la alarma, se coloca al frente de Siete, gira con él (A = izquierda, D = derecha),
/// rueda girando sus 4 llantas y reproduce los AudioSources de extracción y rodaje.
/// </summary>
[ExecuteAlways]
public sealed class INV_IncubatorCart : MonoBehaviour
{
    public const string FlagEllaEnCarrito = "INV_EllaEnCarrito";

    public static INV_IncubatorCart ActiveAttachedInstance { get; private set; }

    [Header("Construcción 3D")]
    [SerializeField] private bool autoBuild3DModel = true;

    [Header("Seguimiento Frente a Siete")]
    [SerializeField] private float frontOffset = 0.96f;
    [SerializeField] private float followLerpSpeed = 18f;
    [SerializeField] private float sideTurnSpeed = 10f;

    [Header("AudioSources del Carrito (hijos en Hierarchy)")]
    [SerializeField] private AudioSource sacarEllaSource;
    [SerializeField] private AudioSource ruedasCarritoSource;

    [Header("Clips Opcionales (o arrástralos directo en cada AudioSource hijo)")]
    [SerializeField] private AudioClip clipSacarEllaCarrito;
    [SerializeField] private AudioClip clipRuedasCarrito;

    public bool IsAttached => isAttached;

    private bool isAttached = false;
    private CORE_PlayerController targetPlayer;
    private INV_PlayerFacing playerFacing;
    private float currentSide = 1f;
    private float lastCartX;
    private float wheelSpinDegrees = 0f;
    private float pulseTimer = 0f;
    private float stopRollGraceTimer = 0f;

    private GameObject ellaInsideRoot;
    private Light ellaDomeLight;
    private readonly List<Transform> wheelPivots = new List<Transform>();

    private void Awake()
    {
        if (autoBuild3DModel)
        {
            Ensure3DModel();
        }

        if (!Application.isPlaying) return;

        // Evitar duplicados si regresamos al Invernadero trayendo ya el carrito acoplado
        if (ActiveAttachedInstance != null && ActiveAttachedInstance != this)
        {
            Destroy(gameObject);
            return;
        }

        EnsureCartAudioSources();
        lastCartX = transform.position.x;
    }

    private void EnsureCartAudioSources()
    {
        if (!sacarEllaSource)
        {
            sacarEllaSource = INV_SoundHelper.FindOrCreateChildSource(transform, "Audio_Sacar_Ella_Carrito", 0.85f, loop: false);
        }
        if (!ruedasCarritoSource)
        {
            ruedasCarritoSource = INV_SoundHelper.FindOrCreateChildSource(transform, "Audio_Ruedas_Carrito", 0.55f, loop: true);
        }
    }

    private void OnEnable()
    {
        if (autoBuild3DModel)
        {
            Ensure3DModel();
        }
    }

    private void Start()
    {
        if (!Application.isPlaying) return;

        EnsureCartAudioSources();

        bool alreadyExtracted = SAL_Progress.HasFlag(FlagEllaEnCarrito);
        SetEllaVisible(alreadyExtracted);

        if (alreadyExtracted && !isAttached)
        {
            var pc = FindMainPlayerController();
            if (pc != null)
            {
                DeployInFrontOfPlayer(pc, playSound: false);
            }
        }
    }

    /// <summary>Coloca a Ella dentro del domo del carrito y acopla el carrito justo al frente de Siete.</summary>
    public void DeployInFrontOfPlayer(CORE_PlayerController player, bool playSound = true)
    {
        Ensure3DModel();
        EnsureCartAudioSources();

        if (player == null)
        {
            player = FindMainPlayerController();
        }
        if (player == null) return;

        targetPlayer = player;
        playerFacing = targetPlayer.GetComponent<INV_PlayerFacing>();
        if (playerFacing == null)
        {
            playerFacing = targetPlayer.gameObject.AddComponent<INV_PlayerFacing>();
        }

        isAttached = true;
        ActiveAttachedInstance = this;
        SAL_Progress.SetFlag(FlagEllaEnCarrito, true);
        SetEllaVisible(true);

        // Desemparentar para poder seguir libremente a Siete y persistir al cruzar al Pasillo (Javier)
        if (transform.parent != null)
        {
            transform.SetParent(null, true);
        }
        DontDestroyOnLoad(gameObject);

        int facing = playerFacing != null ? playerFacing.FacingSign : 1;
        currentSide = facing;

        Vector3 playerPos = targetPlayer.transform.position;
        float floorY = ResolveFloorY(targetPlayer);

        Vector3 startPos = new Vector3(playerPos.x + currentSide * frontOffset, floorY, playerPos.z);
        transform.position = startPos;
        transform.rotation = Quaternion.Euler(0f, facing >= 0 ? 0f : 180f, 0f);
        lastCartX = startPos.x;

        if (playSound && sacarEllaSource != null)
        {
            INV_SoundHelper.PlayEffect(
                sacarEllaSource,
                clipSacarEllaCarrito,
                INV_SoundHelper.CreateCartDeploySound(),
                0.85f
            );
        }
    }

    public void SetEllaVisible(bool visible)
    {
        Ensure3DModel();
        if (ellaInsideRoot != null)
        {
            ellaInsideRoot.SetActive(visible);
        }
        if (ellaDomeLight != null)
        {
            ellaDomeLight.enabled = visible;
        }
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        // Pulso suave de la luz bioluminiscente de Ella en el carrito
        if (ellaDomeLight != null && ellaDomeLight.enabled)
        {
            ellaDomeLight.intensity = 2.6f + Mathf.Sin(Time.time * 3.2f) * 0.45f;
        }

        if (!isAttached) return;

        // Si se reinició la partida o volvimos al menú principal, limpiar instancia persistente
        string activeScene = SceneManager.GetActiveScene().name;
        if (!SAL_Progress.HasFlag(FlagEllaEnCarrito) || activeScene == "MainMenu" || activeScene == "MenuInicio")
        {
            if (ActiveAttachedInstance == this) ActiveAttachedInstance = null;
            Destroy(gameObject);
            return;
        }

        if (targetPlayer == null)
        {
            targetPlayer = FindMainPlayerController();
            if (targetPlayer != null)
            {
                playerFacing = targetPlayer.GetComponent<INV_PlayerFacing>();
                if (playerFacing == null)
                {
                    playerFacing = targetPlayer.gameObject.AddComponent<INV_PlayerFacing>();
                }
            }
        }

        if (targetPlayer == null) return;

        int facingSign = playerFacing != null ? playerFacing.FacingSign : 1;
        currentSide = Mathf.MoveTowards(currentSide, facingSign, Time.deltaTime * sideTurnSpeed);

        Vector3 playerPos = targetPlayer.transform.position;
        float floorY = ResolveFloorY(targetPlayer);

        Vector3 desiredPos = new Vector3(
            playerPos.x + currentSide * frontOffset,
            floorY,
            playerPos.z
        );

        Vector3 nextPos = new Vector3(
            Mathf.Lerp(transform.position.x, desiredPos.x, 1f - Mathf.Exp(-followLerpSpeed * Time.deltaTime)),
            Mathf.Lerp(transform.position.y, desiredPos.y, 1f - Mathf.Exp(-followLerpSpeed * Time.deltaTime)),
            desiredPos.z
        );

        float dxWorld = nextPos.x - lastCartX;
        lastCartX = nextPos.x;
        transform.position = nextPos;

        float targetYaw = facingSign >= 0 ? 0f : 180f;
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.Euler(0f, targetYaw, 0f),
            Time.deltaTime * 720f
        );

        bool movingCart = !SAL_Progress.HasFlag(SAL_Progress.FinalComplete) && Mathf.Abs(dxWorld) > 0.0004f && Time.timeScale > 0f;

        if (movingCart)
        {
            stopRollGraceTimer = 0.14f;

            float forwardSign = facingSign >= 0 ? 1f : -1f;
            float localForwardDelta = dxWorld * forwardSign;
            const float wheelRadius = 0.13f;
            wheelSpinDegrees -= (localForwardDelta / wheelRadius) * Mathf.Rad2Deg;

            Quaternion spinRot = Quaternion.Euler(0f, 0f, wheelSpinDegrees);
            for (int i = 0; i < wheelPivots.Count; i++)
            {
                if (wheelPivots[i] != null)
                {
                    wheelPivots[i].localRotation = spinRot;
                }
            }

            UpdateRollingAudio(isMoving: true);
        }
        else
        {
            if (stopRollGraceTimer > 0f)
            {
                stopRollGraceTimer -= Time.deltaTime;
            }
            else
            {
                UpdateRollingAudio(isMoving: false);
            }
        }
    }

    private void UpdateRollingAudio(bool isMoving)
    {
        if (ruedasCarritoSource == null) return;

        bool hasUserClip = INV_SoundHelper.HasUserAssignedAudio(ruedasCarritoSource, clipRuedasCarrito);

        // Si el usuario asignó un audio de ruedas en Loop, suena continuo mientras rueda y se detiene al frenar
        if (hasUserClip && ruedasCarritoSource.loop)
        {
            if (isMoving)
            {
                INV_SoundHelper.PlayLoop(ruedasCarritoSource, clipRuedasCarrito, null, 0.55f);
            }
            else if (ruedasCarritoSource.isPlaying)
            {
                ruedasCarritoSource.Stop();
            }
            return;
        }

        // Si no hay clip asignado (o Loop está desmarcado), pulso rítmico "tum... tum..." mientras rueda
        if (isMoving)
        {
            pulseTimer += Time.deltaTime;
            if (pulseTimer >= 0.92f)
            {
                pulseTimer = 0f;
                INV_SoundHelper.PlayEffect(
                    ruedasCarritoSource,
                    clipRuedasCarrito,
                    INV_SoundHelper.CreateStepPulse(),
                    0.48f
                );
            }
        }
    }

    private void OnDestroy()
    {
        if (ActiveAttachedInstance == this)
        {
            ActiveAttachedInstance = null;
        }
    }

    private static CORE_PlayerController FindMainPlayerController()
    {
        if (PlayerPersistence.Instance != null && PlayerPersistence.Instance.TryGetComponent(out CORE_PlayerController persistentPc))
        {
            return persistentPc;
        }

        var all = FindObjectsByType<CORE_PlayerController>(FindObjectsSortMode.None);
        foreach (var pc in all)
        {
            if (pc != null && pc.GetComponent<CapsuleCollider>() != null)
            {
                return pc;
            }
        }
        return all.Length > 0 ? all[0] : null;
    }

    private float ResolveFloorY(CORE_PlayerController player)
    {
        Vector3 origin = player.transform.position + Vector3.up * 0.25f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 3.0f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float bestDist = float.MaxValue;
        float bestY = float.NaN;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null) continue;
            if (col.transform == player.transform || col.transform.IsChildOf(player.transform)) continue;
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

            if (hits[i].distance < bestDist)
            {
                bestDist = hits[i].distance;
                bestY = hits[i].point.y;
            }
        }

        if (!float.IsNaN(bestY))
        {
            return bestY;
        }

        if (player.TryGetComponent(out CapsuleCollider cap))
        {
            return player.transform.position.y + cap.center.y - (cap.height * 0.5f);
        }

        return player.transform.position.y - 0.85f;
    }

    // =========================================================================
    // CONSTRUCCIÓN PROCEDURAL DEL MODELO 3D DEL CARRITO + CÁPSULA DE ELLA
    // =========================================================================
    public void Ensure3DModel()
    {
        Transform existing = transform.Find("INV_Visual_Carrito3D");
        if (existing != null)
        {
            CacheModelReferences(existing);
            return;
        }

        GameObject root = new GameObject("INV_Visual_Carrito3D");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;

        Color gunmetal = new Color(0.18f, 0.21f, 0.25f);
        Color steelLight = new Color(0.52f, 0.56f, 0.62f);
        Color tireRubber = new Color(0.09f, 0.09f, 0.11f);
        Color hazardAmber = new Color(0.86f, 0.56f, 0.14f);
        Color brassRing = new Color(0.36f, 0.31f, 0.24f);
        Color violetGlow = new Color(0.76f, 0.38f, 1.0f);
        Color stemTeal = new Color(0.32f, 0.82f, 0.62f);

        wheelPivots.Clear();
        float[] wheelXs = { -0.32f, 0.32f };
        float[] wheelZs = { -0.24f, 0.24f };
        int wIdx = 0;
        foreach (float wx in wheelXs)
        {
            foreach (float wz in wheelZs)
            {
                CreatePart(root.transform, $"Horquilla_{wIdx}", PrimitiveType.Cube,
                    new Vector3(wx, 0.20f, wz), Vector3.zero, new Vector3(0.07f, 0.11f, 0.07f),
                    steelLight, 0.55f);

                GameObject pivotGo = new GameObject($"WheelPivot_{wIdx}");
                pivotGo.transform.SetParent(root.transform, false);
                pivotGo.transform.localPosition = new Vector3(wx, 0.13f, wz);
                wheelPivots.Add(pivotGo.transform);

                CreatePart(pivotGo.transform, "Llanta", PrimitiveType.Cylinder,
                    Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(0.26f, 0.042f, 0.26f),
                    tireRubber, 0.15f);

                CreatePart(pivotGo.transform, "Rin", PrimitiveType.Cylinder,
                    Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(0.14f, 0.048f, 0.14f),
                    steelLight, 0.65f);

                CreatePart(pivotGo.transform, "Rayo_H", PrimitiveType.Cube,
                    Vector3.zero, Vector3.zero, new Vector3(0.22f, 0.026f, 0.098f),
                    hazardAmber, 0.45f);

                CreatePart(pivotGo.transform, "Rayo_V", PrimitiveType.Cube,
                    Vector3.zero, Vector3.zero, new Vector3(0.026f, 0.22f, 0.098f),
                    steelLight, 0.55f);

                wIdx++;
            }
        }

        CreatePart(root.transform, "Plataforma_Base", PrimitiveType.Cube,
            new Vector3(0f, 0.26f, 0f), Vector3.zero, new Vector3(0.92f, 0.065f, 0.60f),
            gunmetal, 0.45f);

        CreatePart(root.transform, "Parachoques_Frontal", PrimitiveType.Cube,
            new Vector3(0.46f, 0.27f, 0f), Vector3.zero, new Vector3(0.055f, 0.08f, 0.64f),
            hazardAmber, 0.4f);

        CreatePart(root.transform, "Barandal_Izq", PrimitiveType.Cube,
            new Vector3(0.02f, 0.33f, 0.28f), Vector3.zero, new Vector3(0.84f, 0.045f, 0.035f),
            steelLight, 0.55f);

        CreatePart(root.transform, "Barandal_Der", PrimitiveType.Cube,
            new Vector3(0.02f, 0.33f, -0.28f), Vector3.zero, new Vector3(0.84f, 0.045f, 0.035f),
            steelLight, 0.55f);

        CreatePart(root.transform, "Poste_Manubrio_Izq", PrimitiveType.Cylinder,
            new Vector3(-0.46f, 0.46f, 0.22f), new Vector3(0f, 0f, 16f), new Vector3(0.042f, 0.21f, 0.042f),
            steelLight, 0.6f);

        CreatePart(root.transform, "Poste_Manubrio_Der", PrimitiveType.Cylinder,
            new Vector3(-0.46f, 0.46f, -0.22f), new Vector3(0f, 0f, 16f), new Vector3(0.042f, 0.21f, 0.042f),
            steelLight, 0.6f);

        CreatePart(root.transform, "Barra_Agarre", PrimitiveType.Cylinder,
            new Vector3(-0.52f, 0.64f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.05f, 0.26f, 0.05f),
            tireRubber, 0.25f);

        CreatePart(root.transform, "Modulo_Soporte", PrimitiveType.Cube,
            new Vector3(-0.28f, 0.40f, 0f), Vector3.zero, new Vector3(0.18f, 0.22f, 0.42f),
            new Color(0.13f, 0.15f, 0.19f), 0.35f);

        Color[] ledColors =
        {
            new Color(1.0f, 0.45f, 0.15f),
            new Color(0.55f, 0.38f, 0.95f),
            new Color(0.25f, 0.85f, 0.95f),
            new Color(0.35f, 0.92f, 0.65f)
        };
        for (int i = 0; i < 4; i++)
        {
            float lz = -0.135f + i * 0.09f;
            GameObject led = CreatePart(root.transform, $"LED_Soporte_{i}", PrimitiveType.Sphere,
                new Vector3(-0.28f, 0.52f, lz), Vector3.zero, new Vector3(0.045f, 0.045f, 0.045f),
                ledColors[i], 0.8f);
            SetEmissive(led, ledColors[i], 1.6f);
        }

        CreatePart(root.transform, "Capsula_Anillo_Base", PrimitiveType.Cylinder,
            new Vector3(0.10f, 0.33f, 0f), Vector3.zero, new Vector3(0.48f, 0.045f, 0.48f),
            brassRing, 0.5f);

        GameObject glassDome = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        glassDome.name = "Domo_Cristal";
        glassDome.transform.SetParent(root.transform, false);
        glassDome.transform.localPosition = new Vector3(0.10f, 0.57f, 0f);
        glassDome.transform.localScale = new Vector3(0.42f, 0.22f, 0.42f);
        RemoveCollider(glassDome);
        SetTransparentGlassMaterial(glassDome, new Color(0.62f, 0.84f, 0.98f, 0.26f));

        CreatePart(root.transform, "Capsula_Tapa_Superior", PrimitiveType.Cylinder,
            new Vector3(0.10f, 0.80f, 0f), Vector3.zero, new Vector3(0.45f, 0.035f, 0.45f),
            brassRing, 0.5f);

        CreatePart(root.transform, "Respiradero_Superior", PrimitiveType.Cylinder,
            new Vector3(0.10f, 0.85f, 0f), Vector3.zero, new Vector3(0.18f, 0.025f, 0.18f),
            steelLight, 0.6f);

        ellaInsideRoot = new GameObject("Ella_En_Carrito");
        ellaInsideRoot.transform.SetParent(root.transform, false);
        ellaInsideRoot.transform.localPosition = new Vector3(0.10f, 0.36f, 0f);

        CreatePart(ellaInsideRoot.transform, "Sustrato_Capsula", PrimitiveType.Cylinder,
            new Vector3(0f, 0.02f, 0f), Vector3.zero, new Vector3(0.36f, 0.02f, 0.36f),
            new Color(0.08f, 0.07f, 0.10f), 0.1f);

        GameObject stem = CreatePart(ellaInsideRoot.transform, "Tallo_Ella", PrimitiveType.Cylinder,
            new Vector3(0f, 0.18f, 0f), Vector3.zero, new Vector3(0.045f, 0.16f, 0.045f),
            stemTeal, 0.4f);
        SetEmissive(stem, stemTeal, 1.1f);

        GameObject leafL = CreatePart(ellaInsideRoot.transform, "Hoja_Izq", PrimitiveType.Sphere,
            new Vector3(-0.06f, 0.16f, 0.03f), new Vector3(0f, 20f, 35f), new Vector3(0.10f, 0.025f, 0.05f),
            stemTeal, 0.4f);
        SetEmissive(leafL, stemTeal, 0.9f);

        GameObject leafR = CreatePart(ellaInsideRoot.transform, "Hoja_Der", PrimitiveType.Sphere,
            new Vector3(0.06f, 0.20f, -0.03f), new Vector3(0f, -25f, -35f), new Vector3(0.10f, 0.025f, 0.05f),
            stemTeal, 0.4f);
        SetEmissive(leafR, stemTeal, 0.9f);

        for (int i = 0; i < 3; i++)
        {
            float ang = i * 120f * Mathf.Deg2Rad;
            GameObject lantern = CreatePart(ellaInsideRoot.transform, $"Farolito_{i}", PrimitiveType.Sphere,
                new Vector3(Mathf.Cos(ang) * 0.10f, 0.31f, Mathf.Sin(ang) * 0.10f),
                Vector3.zero,
                new Vector3(0.085f, 0.115f, 0.085f),
                violetGlow, 0.7f);
            SetEmissive(lantern, violetGlow, 2.0f);
        }

        GameObject domeLightGo = new GameObject("Luz_Ella_Carrito");
        domeLightGo.transform.SetParent(ellaInsideRoot.transform, false);
        domeLightGo.transform.localPosition = new Vector3(0f, 0.26f, 0f);
        ellaDomeLight = domeLightGo.AddComponent<Light>();
        ellaDomeLight.type = LightType.Point;
        ellaDomeLight.color = violetGlow;
        ellaDomeLight.range = 3.2f;
        ellaDomeLight.intensity = 2.6f;
        ellaDomeLight.shadows = LightShadows.None;
    }

    private void CacheModelReferences(Transform visualRoot)
    {
        wheelPivots.Clear();
        for (int i = 0; i < 4; i++)
        {
            Transform wp = visualRoot.Find($"WheelPivot_{i}");
            if (wp != null) wheelPivots.Add(wp);
        }

        Transform ellaTr = visualRoot.Find("Ella_En_Carrito");
        if (ellaTr != null)
        {
            ellaInsideRoot = ellaTr.gameObject;
            ellaDomeLight = ellaTr.GetComponentInChildren<Light>(true);
        }
    }

    private static GameObject CreatePart(
        Transform parent,
        string partName,
        PrimitiveType type,
        Vector3 localPos,
        Vector3 localEuler,
        Vector3 localScale,
        Color color,
        float smoothness)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = partName;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(localEuler);
        go.transform.localScale = localScale;
        RemoveCollider(go);

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            mr.sharedMaterial = mat;
        }
        return go;
    }

    private static void SetEmissive(GameObject go, Color glowColor, float intensity)
    {
        var mr = go.GetComponent<MeshRenderer>();
        if (mr == null || mr.sharedMaterial == null) return;
        Material mat = mr.sharedMaterial;
        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", glowColor * intensity);
        }
    }

    private static void SetTransparentGlassMaterial(GameObject go, Color tint)
    {
        var mr = go.GetComponent<MeshRenderer>();
        if (mr == null) return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = tint;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.92f);

        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        mr.sharedMaterial = mat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
    }

    private static void RemoveCollider(GameObject go)
    {
        Collider col = go.GetComponent<Collider>();
        if (col != null)
        {
            DestroyImmediate(col);
        }
    }
}
