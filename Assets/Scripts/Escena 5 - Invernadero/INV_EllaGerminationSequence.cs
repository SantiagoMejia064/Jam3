using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Secuencia 3D animada dentro de la Cámara de Cultivo:
/// Muestra la semilla germinando, abriéndose en dos cáscaras, brotando el tallo con hojas bioluminiscentes
/// y floreciendo en los 3 farolitos violetas de Ella (idéntica a la Ella que luego va en el carrito).
/// </summary>
public sealed class INV_EllaGerminationSequence : MonoBehaviour
{
    private Transform modelRoot;
    private Transform seedRoot;
    private Transform huskLeft;
    private Transform huskRight;
    private Transform seedCore;

    private Transform stemPivot;
    private Transform crownRoot;
    private readonly List<Transform> leaves = new List<Transform>();
    private readonly List<Vector3> leafTargetRotations = new List<Vector3>();
    private readonly List<Vector3> leafTargetScales = new List<Vector3>();

    private readonly List<Transform> pedicels = new List<Transform>();
    private readonly List<Vector3> pedicelTargetScales = new List<Vector3>();
    private readonly List<Transform> lanterns = new List<Transform>();
    private readonly List<Vector3> lanternTargetScales = new List<Vector3>();

    private readonly List<Transform> sporeMotes = new List<Transform>();
    private readonly List<Vector3> moteBasePositions = new List<Vector3>();

    private readonly List<Material> createdMaterials = new List<Material>();
    private Material seedCoreMat;
    private Light chamberVioletLight;

    private Coroutine animRoutine;
    private bool isFullyBloomed = false;
    private bool isExtracted = false;

    public void InitializeInsideChamber(Light existingVioletLight)
    {
        chamberVioletLight = existingVioletLight;
        PositionAtCultivationChamber(out float interiorHeight);
        BuildModel(interiorHeight);
    }

    private void PositionAtCultivationChamber(out float interiorHeight)
    {
        interiorHeight = 1.05f;
        GameObject baseGo = GameObject.Find("Camara_Cultivo_Base");
        GameObject topGo = GameObject.Find("Camara_Cultivo_Cabezal");

        if (baseGo != null && baseGo.TryGetComponent(out Renderer baseRenderer))
        {
            Bounds b = baseRenderer.bounds;
            Vector3 chamberFloorCenter = new Vector3(b.center.x, b.max.y - 0.01f, b.center.z);
            transform.position = chamberFloorCenter;
            transform.rotation = Quaternion.identity;

            if (topGo != null && topGo.TryGetComponent(out Renderer topRenderer))
            {
                float h = topRenderer.bounds.min.y - b.max.y;
                if (h > 0.45f) interiorHeight = h;
            }
        }

        if (chamberVioletLight != null)
        {
            chamberVioletLight.transform.position = transform.position + new Vector3(0f, interiorHeight * 0.55f, -0.15f);
            chamberVioletLight.color = new Color(0.76f, 0.38f, 1.0f);
            chamberVioletLight.range = 6.5f;
        }
    }

    private void BuildModel(float interiorHeight)
    {
        if (modelRoot != null) return;

        modelRoot = new GameObject("INV_Ella_Germinacion_3D").transform;
        modelRoot.SetParent(transform, false);
        modelRoot.localPosition = Vector3.zero;
        modelRoot.localRotation = Quaternion.identity;

        // Ajustar escala uniforme para ocupar con elegancia el interior de Camara_Cultivo sin tocar el cabezal
        float scaleFactor = Mathf.Clamp((interiorHeight * 0.80f) / 0.76f, 0.85f, 1.38f);
        modelRoot.localScale = Vector3.one * scaleFactor;

        Color soilColor = new Color(0.08f, 0.07f, 0.10f);
        Color trayRimColor = new Color(0.24f, 0.27f, 0.32f);
        Color huskColor = new Color(0.24f, 0.17f, 0.30f);
        Color seedGlowColor = new Color(0.85f, 0.55f, 1.0f);
        Color stemTeal = new Color(0.32f, 0.82f, 0.62f);
        Color violetGlow = new Color(0.76f, 0.38f, 1.0f);
        Color stamenWhiteViolet = new Color(0.94f, 0.82f, 1.0f);

        // 1. Plato y sustrato oscuro en la base de la cámara
        CreatePrimitivePart(modelRoot, "Bandeja_Cultivo", PrimitiveType.Cylinder,
            new Vector3(0f, 0.015f, 0f), Vector3.zero, new Vector3(0.50f, 0.015f, 0.50f),
            trayRimColor, 0.55f, 0f);

        CreatePrimitivePart(modelRoot, "Sustrato_Tierra", PrimitiveType.Cylinder,
            new Vector3(0f, 0.028f, 0f), Vector3.zero, new Vector3(0.44f, 0.018f, 0.44f),
            soilColor, 0.10f, 0f);

        // 2. Semilla germinable (2 mitades de cáscara + núcleo bioluminiscente)
        seedRoot = new GameObject("Semilla_Germinable").transform;
        seedRoot.SetParent(modelRoot, false);
        seedRoot.localPosition = new Vector3(0f, 0.075f, 0f);

        GameObject coreGo = CreatePrimitivePart(seedRoot, "Semilla_Nucleo", PrimitiveType.Sphere,
            Vector3.zero, Vector3.zero, new Vector3(0.085f, 0.115f, 0.085f),
            seedGlowColor, 0.75f, 2.2f);
        seedCore = coreGo.transform;
        seedCoreMat = coreGo.GetComponent<MeshRenderer>().sharedMaterial;

        huskLeft = CreatePrimitivePart(seedRoot, "Cascara_Izq", PrimitiveType.Sphere,
            new Vector3(-0.018f, 0f, 0f), Vector3.zero, new Vector3(0.075f, 0.13f, 0.095f),
            huskColor, 0.35f, 0.25f).transform;

        huskRight = CreatePrimitivePart(seedRoot, "Cascara_Der", PrimitiveType.Sphere,
            new Vector3(0.018f, 0f, 0f), Vector3.zero, new Vector3(0.075f, 0.13f, 0.095f),
            huskColor, 0.35f, 0.25f).transform;

        // 3. Pivote del tallo desde la base (permite crecer hacia arriba desde y = 0.04)
        stemPivot = new GameObject("Tallo_Pivot").transform;
        stemPivot.SetParent(modelRoot, false);
        stemPivot.localPosition = new Vector3(0f, 0.04f, 0f);

        CreatePrimitivePart(stemPivot, "Tallo_Principal", PrimitiveType.Cylinder,
            new Vector3(0f, 0.26f, 0f), Vector3.zero, new Vector3(0.052f, 0.26f, 0.052f),
            stemTeal, 0.45f, 1.15f);

        // 4. Hojas bioluminiscentes escalonadas (misma estética que Ella_En_Carrito)
        leaves.Clear();
        leafTargetRotations.Clear();
        leafTargetScales.Clear();

        AddLeaf(stemPivot, "Hoja_Baja_Izq", new Vector3(-0.065f, 0.17f, 0.02f), new Vector3(0f, 18f, 36f), new Vector3(0.14f, 0.03f, 0.065f), stemTeal);
        AddLeaf(stemPivot, "Hoja_Baja_Der", new Vector3(0.065f, 0.22f, -0.02f), new Vector3(0f, -20f, -36f), new Vector3(0.14f, 0.03f, 0.065f), stemTeal);
        AddLeaf(stemPivot, "Hoja_Alta_Izq", new Vector3(-0.075f, 0.34f, -0.02f), new Vector3(0f, -15f, 32f), new Vector3(0.16f, 0.032f, 0.07f), stemTeal);
        AddLeaf(stemPivot, "Hoja_Alta_Der", new Vector3(0.075f, 0.39f, 0.02f), new Vector3(0f, 15f, -32f), new Vector3(0.16f, 0.032f, 0.07f), stemTeal);

        // 5. Corona superior con los 3 farolitos colgantes violetas de Ella
        crownRoot = new GameObject("Corona_Farolitos").transform;
        crownRoot.SetParent(stemPivot, false);
        crownRoot.localPosition = new Vector3(0f, 0.52f, 0f);

        pedicels.Clear();
        pedicelTargetScales.Clear();
        lanterns.Clear();
        lanternTargetScales.Clear();

        // Tres posiciones bien legibles hacia la cámara 2.5D (izquierda, derecha y centro elevado)
        Vector3[] lanternOffsets =
        {
            new Vector3(-0.145f, 0.03f, -0.04f),
            new Vector3( 0.145f, 0.05f, -0.04f),
            new Vector3( 0.00f,  0.11f,  0.05f)
        };
        Vector3[] pedicelRots =
        {
            new Vector3(0f, 0f, 52f),
            new Vector3(0f, 0f, -52f),
            new Vector3(-22f, 0f, 0f)
        };

        for (int i = 0; i < 3; i++)
        {
            Vector3 mid = lanternOffsets[i] * 0.55f + new Vector3(0f, 0.04f, 0f);
            Vector3 pedScale = new Vector3(0.024f, 0.09f, 0.024f);
            Transform ped = CreatePrimitivePart(crownRoot, $"Rama_Farol_{i}", PrimitiveType.Cylinder,
                mid, pedicelRots[i], pedScale, stemTeal, 0.4f, 1.0f).transform;
            pedicels.Add(ped);
            pedicelTargetScales.Add(pedScale);

            GameObject lanternPivot = new GameObject($"Farolito_Pivot_{i}");
            lanternPivot.transform.SetParent(crownRoot, false);
            lanternPivot.transform.localPosition = lanternOffsets[i];

            // Cáliz verde-azulado superior
            CreatePrimitivePart(lanternPivot.transform, "Caliz", PrimitiveType.Cylinder,
                new Vector3(0f, 0.075f, 0f), Vector3.zero, new Vector3(0.065f, 0.015f, 0.065f),
                stemTeal, 0.45f, 1.1f);

            // Campana luminosa azul-violeta (farolito)
            CreatePrimitivePart(lanternPivot.transform, $"Farolito_{i}", PrimitiveType.Sphere,
                Vector3.zero, Vector3.zero, new Vector3(0.125f, 0.17f, 0.125f),
                violetGlow, 0.72f, 2.25f);

            // Gota estambre brillante inferior
            CreatePrimitivePart(lanternPivot.transform, "Estambre_Luz", PrimitiveType.Sphere,
                new Vector3(0f, -0.085f, 0f), Vector3.zero, new Vector3(0.042f, 0.05f, 0.042f),
                stamenWhiteViolet, 0.85f, 3.0f);

            lanterns.Add(lanternPivot.transform);
            lanternTargetScales.Add(Vector3.one);
        }

        // 6. Motas de luz bioluminiscente flotando alrededor durante la floración
        sporeMotes.Clear();
        moteBasePositions.Clear();
        for (int i = 0; i < 6; i++)
        {
            float ang = i * 60f * Mathf.Deg2Rad;
            Vector3 basePos = new Vector3(Mathf.Cos(ang) * 0.20f, 0.22f + (i % 3) * 0.14f, Mathf.Sin(ang) * 0.16f);
            Transform mote = CreatePrimitivePart(modelRoot, $"Mota_Luz_{i}", PrimitiveType.Sphere,
                basePos, Vector3.zero, Vector3.one * 0.026f,
                stamenWhiteViolet, 0.8f, 2.8f).transform;
            mote.gameObject.SetActive(false);
            sporeMotes.Add(mote);
            moteBasePositions.Add(basePos);
        }
    }

    private void AddLeaf(Transform parent, string leafName, Vector3 pos, Vector3 eulerRot, Vector3 scale, Color color)
    {
        Transform leaf = CreatePrimitivePart(parent, leafName, PrimitiveType.Sphere,
            pos, eulerRot, scale, color, 0.42f, 0.95f).transform;
        leaves.Add(leaf);
        leafTargetRotations.Add(eulerRot);
        leafTargetScales.Add(scale);
    }

    /// <summary>Oculta todo antes de montar la semilla.</summary>
    public void HideAll()
    {
        StopRunningAnimation();
        isFullyBloomed = false;
        isExtracted = false;
        if (modelRoot != null) modelRoot.gameObject.SetActive(false);
        if (chamberVioletLight != null) chamberVioletLight.enabled = false;
    }

    /// <summary>Muestra únicamente la semilla cerrada reposando en la cámara al montar las piezas.</summary>
    public void ShowDormantSeed()
    {
        StopRunningAnimation();
        isFullyBloomed = false;
        isExtracted = false;

        if (modelRoot == null) return;
        modelRoot.gameObject.SetActive(true);

        seedRoot.gameObject.SetActive(true);
        seedRoot.localScale = Vector3.one;
        seedRoot.localPosition = new Vector3(0f, 0.075f, 0f);
        huskLeft.localPosition = new Vector3(-0.018f, 0f, 0f);
        huskLeft.localRotation = Quaternion.identity;
        huskRight.localPosition = new Vector3(0.018f, 0f, 0f);
        huskRight.localRotation = Quaternion.identity;
        seedCore.localScale = new Vector3(0.075f, 0.10f, 0.075f);

        stemPivot.gameObject.SetActive(false);
        for (int i = 0; i < sporeMotes.Count; i++)
        {
            sporeMotes[i].gameObject.SetActive(false);
        }

        if (chamberVioletLight != null)
        {
            chamberVioletLight.enabled = true;
            chamberVioletLight.intensity = 0.8f;
        }
    }

    /// <summary>Reproduce la animación completa de germinación de la semilla hasta convertirse en Ella.</summary>
    public Coroutine PlayGerminationAnimation(float duration = 4.6f)
    {
        StopRunningAnimation();
        ShowDormantSeed();
        animRoutine = StartCoroutine(GerminationRoutine(Mathf.Max(2.0f, duration)));
        return animRoutine;
    }

    /// <summary>Deja a Ella completamente florecida (para restaurar estado si ya había florecido).</summary>
    public void ShowFullBloomImmediate()
    {
        StopRunningAnimation();
        if (modelRoot == null) return;

        isExtracted = false;
        isFullyBloomed = true;
        modelRoot.gameObject.SetActive(true);

        seedRoot.gameObject.SetActive(true);
        seedRoot.localPosition = new Vector3(0f, 0.065f, 0f);
        seedRoot.localScale = Vector3.one;
        huskLeft.localPosition = new Vector3(-0.055f, -0.01f, 0f);
        huskLeft.localRotation = Quaternion.Euler(0f, 0f, 44f);
        huskRight.localPosition = new Vector3(0.055f, -0.01f, 0f);
        huskRight.localRotation = Quaternion.Euler(0f, 0f, -44f);
        seedCore.localScale = new Vector3(0.05f, 0.05f, 0.05f);

        stemPivot.gameObject.SetActive(true);
        stemPivot.localScale = Vector3.one;
        stemPivot.localRotation = Quaternion.identity;

        for (int i = 0; i < leaves.Count; i++)
        {
            leaves[i].localScale = leafTargetScales[i];
            leaves[i].localRotation = Quaternion.Euler(leafTargetRotations[i]);
        }

        for (int i = 0; i < pedicels.Count; i++)
        {
            pedicels[i].localScale = pedicelTargetScales[i];
        }

        for (int i = 0; i < lanterns.Count; i++)
        {
            lanterns[i].localScale = lanternTargetScales[i];
        }

        for (int i = 0; i < sporeMotes.Count; i++)
        {
            sporeMotes[i].gameObject.SetActive(true);
        }

        if (chamberVioletLight != null)
        {
            chamberVioletLight.enabled = true;
            chamberVioletLight.intensity = 4.8f;
        }
    }

    /// <summary>Cuando Siete saca a Ella al carrito, oculta la planta y deja solo la bandeja con las cáscaras abiertas.</summary>
    public void HidePlantKeepEmptyTray()
    {
        StopRunningAnimation();
        isFullyBloomed = false;
        isExtracted = true;

        if (stemPivot != null) stemPivot.gameObject.SetActive(false);
        if (seedCore != null) seedCore.gameObject.SetActive(false);
        for (int i = 0; i < sporeMotes.Count; i++)
        {
            if (sporeMotes[i] != null) sporeMotes[i].gameObject.SetActive(false);
        }
        if (chamberVioletLight != null)
        {
            chamberVioletLight.enabled = false;
        }
    }

    private IEnumerator GerminationRoutine(float totalDuration)
    {
        stemPivot.gameObject.SetActive(true);
        stemPivot.localScale = new Vector3(0.25f, 0.005f, 0.25f);

        for (int i = 0; i < leaves.Count; i++) leaves[i].localScale = Vector3.zero;
        for (int i = 0; i < pedicels.Count; i++) pedicels[i].localScale = Vector3.zero;
        for (int i = 0; i < lanterns.Count; i++) lanterns[i].localScale = Vector3.zero;
        for (int i = 0; i < sporeMotes.Count; i++)
        {
            sporeMotes[i].gameObject.SetActive(true);
            sporeMotes[i].localScale = Vector3.zero;
        }

        float elapsed = 0f;
        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / totalDuration);

            // FASE 1 (0.00 -> 0.28): La semilla late, se hincha y rompe sus cáscaras
            float seedPhase = Mathf.Clamp01(u / 0.28f);
            float crackT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - 0.10f) / 0.22f));

            float tremble = (1f - crackT) * Mathf.Sin(elapsed * 42f) * 0.010f;
            float swell = 1f + Mathf.Sin(seedPhase * Mathf.PI) * 0.26f;
            seedRoot.localPosition = new Vector3(tremble, 0.075f - crackT * 0.01f, 0f);
            seedRoot.localScale = Vector3.one * swell;

            huskLeft.localPosition = new Vector3(Mathf.Lerp(-0.018f, -0.056f, crackT), Mathf.Lerp(0f, -0.01f, crackT), 0f);
            huskLeft.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 44f, crackT));

            huskRight.localPosition = new Vector3(Mathf.Lerp(0.018f, 0.056f, crackT), Mathf.Lerp(0f, -0.01f, crackT), 0f);
            huskRight.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -44f, crackT));

            if (seedCoreMat != null && seedCoreMat.HasProperty("_EmissionColor"))
            {
                float flash = Mathf.Lerp(1.6f, 3.8f, Mathf.Sin(seedPhase * Mathf.PI));
                seedCoreMat.SetColor("_EmissionColor", new Color(0.85f, 0.55f, 1.0f) * flash);
            }

            // FASE 2 (0.18 -> 0.68): El tallo emerge verticalmente y las hojas se despliegan
            float stemT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - 0.18f) / 0.50f));
            float stemWidth = Mathf.Clamp01(stemT * 1.4f);
            stemPivot.localScale = new Vector3(
                Mathf.Lerp(0.28f, 1f, stemWidth),
                Mathf.Max(0.005f, stemT),
                Mathf.Lerp(0.28f, 1f, stemWidth)
            );
            float growthSway = Mathf.Sin(elapsed * 3.2f) * 3.0f * stemT;
            stemPivot.localRotation = Quaternion.Euler(0f, 0f, growthSway);

            for (int i = 0; i < leaves.Count; i++)
            {
                float leafStart = 0.28f + i * 0.08f;
                float leafT = EaseOutBack(Mathf.Clamp01((u - leafStart) / 0.24f));
                leaves[i].localScale = leafTargetScales[i] * leafT;

                Vector3 targetEuler = leafTargetRotations[i];
                Vector3 foldedEuler = new Vector3(targetEuler.x, targetEuler.y, targetEuler.z * 0.2f);
                leaves[i].localRotation = Quaternion.Euler(Vector3.LerpUnclamped(foldedEuler, targetEuler, leafT));
            }

            // FASE 3 (0.52 -> 1.00): Ramas superiores y floración de los 3 farolitos violetas de Ella
            for (int i = 0; i < pedicels.Count; i++)
            {
                float pedStart = 0.50f + i * 0.06f;
                float pedT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - pedStart) / 0.22f));
                pedicels[i].localScale = pedicelTargetScales[i] * pedT;

                float lanStart = 0.60f + i * 0.08f;
                float lanT = EaseOutBack(Mathf.Clamp01((u - lanStart) / 0.28f));
                lanterns[i].localScale = lanternTargetScales[i] * lanT;
            }

            // Motas luminosas ascendentes
            float moteFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - 0.45f) / 0.40f));
            for (int i = 0; i < sporeMotes.Count; i++)
            {
                sporeMotes[i].localScale = Vector3.one * (0.026f * moteFade);
                Vector3 bp = moteBasePositions[i];
                float floatY = Mathf.Repeat(elapsed * 0.14f + i * 0.11f, 0.36f);
                sporeMotes[i].localPosition = bp + new Vector3(Mathf.Sin(elapsed * 2f + i) * 0.03f, floatY, 0f);
            }

            if (chamberVioletLight != null)
            {
                chamberVioletLight.enabled = true;
                chamberVioletLight.intensity = Mathf.Lerp(0.8f, 4.8f, Mathf.SmoothStep(0f, 1f, u));
            }

            yield return null;
        }

        animRoutine = null;
        ShowFullBloomImmediate();
    }

    private void Update()
    {
        if (isExtracted || modelRoot == null || !modelRoot.gameObject.activeInHierarchy) return;

        // Si sólo está la semilla dormida antes de afinar, hacerla latir suavemente
        if (!isFullyBloomed && animRoutine == null && seedCore != null && seedRoot.gameObject.activeSelf)
        {
            float beat = 1f + Mathf.Sin(Time.time * 3.0f) * 0.06f;
            seedRoot.localScale = Vector3.one * beat;
            return;
        }

        if (!isFullyBloomed) return;

        // Respiración orgánica suave de Ella ya florecida dentro de la cámara
        float sway = Mathf.Sin(Time.time * 1.6f) * 2.2f;
        if (stemPivot != null)
        {
            stemPivot.localRotation = Quaternion.Euler(0f, 0f, sway);
        }

        for (int i = 0; i < lanterns.Count; i++)
        {
            if (lanterns[i] != null)
            {
                float pend = Mathf.Sin(Time.time * 2.1f + i * 1.4f) * 4.0f;
                lanterns[i].localRotation = Quaternion.Euler(0f, 0f, pend);
            }
        }

        for (int i = 0; i < sporeMotes.Count; i++)
        {
            if (sporeMotes[i] != null && sporeMotes[i].gameObject.activeSelf)
            {
                Vector3 bp = moteBasePositions[i];
                float floatY = Mathf.Repeat(Time.time * 0.12f + i * 0.13f, 0.36f);
                sporeMotes[i].localPosition = bp + new Vector3(Mathf.Sin(Time.time * 1.8f + i) * 0.035f, floatY, 0f);
            }
        }

        if (chamberVioletLight != null && chamberVioletLight.enabled)
        {
            chamberVioletLight.intensity = 4.8f + Mathf.Sin(Time.time * 2.6f) * 0.45f;
        }
    }

    private void StopRunningAnimation()
    {
        if (animRoutine != null)
        {
            StopCoroutine(animRoutine);
            animRoutine = null;
        }
    }

    private static float EaseOutBack(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float p = t - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }

    private GameObject CreatePrimitivePart(
        Transform parent,
        string partName,
        PrimitiveType type,
        Vector3 localPos,
        Vector3 localEuler,
        Vector3 localScale,
        Color color,
        float smoothness,
        float emissionMultiplier)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = partName;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(localEuler);
        go.transform.localScale = localScale;

        Collider col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (emissionMultiplier > 0.001f)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", color * emissionMultiplier);
                }
            }
            mr.sharedMaterial = mat;
            createdMaterials.Add(mat);
        }
        return go;
    }

    private void OnDestroy()
    {
        for (int i = 0; i < createdMaterials.Count; i++)
        {
            if (createdMaterials[i] != null)
            {
                Destroy(createdMaterials[i]);
            }
        }
        createdMaterials.Clear();
    }
}
