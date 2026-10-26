using UnityEngine;

/// <summary>Maceta quemada con los restos de Ella en el Invernadero. Incluye modelo 3D con ceniza y brillo violeta tenue.</summary>
[ExecuteAlways]
[RequireComponent(typeof(Collider))]
public sealed class INV_RestosElla : MonoBehaviour
{
    [Header("Interacción")]
    [SerializeField] private string inspectPrompt = "E · Mirar maceta quemada";
    [SerializeField] private string inspectMessage = "Ceniza con un brillo violeta apagado.";

    [Header("Efectos Visuales 3D")]
    [SerializeField] private bool autoBuild3DModel = true;
    [SerializeField] private Color violetGlowColor = new Color(0.75f, 0.35f, 0.98f);
    [SerializeField] private float lightIntensityBase = 1.8f;
    [SerializeField] private float lightIntensityPulse = 0.4f;

    private CORE_PlayerController player;
    private bool inRange;
    private Light violetLight;

    private void Awake()
    {
        if (autoBuild3DModel)
        {
            Ensure3DModel();
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
        GetComponent<Collider>().isTrigger = true;
    }

    private void Ensure3DModel()
    {
        Transform existing = transform.Find("INV_Visual_RestosElla");
        if (existing != null)
        {
            violetLight = existing.GetComponentInChildren<Light>();
            return;
        }

        GameObject root = new GameObject("INV_Visual_RestosElla");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;

        // 1. Cuerpo de la Maceta Quemada (Cilindro cerámico oscuro)
        GameObject potGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        potGo.name = "Maceta_Quemada_Cuerpo";
        potGo.transform.SetParent(root.transform, false);
        potGo.transform.localPosition = new Vector3(0, 0.22f, 0);
        potGo.transform.localScale = new Vector3(0.52f, 0.22f, 0.52f);
        DestroyImmediate(potGo.GetComponent<Collider>());
        SetMaterialColor(potGo, new Color(0.11f, 0.10f, 0.12f), 0.2f);

        // 2. Borde superior de la maceta
        GameObject rimGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rimGo.name = "Maceta_Quemada_Borde";
        rimGo.transform.SetParent(root.transform, false);
        rimGo.transform.localPosition = new Vector3(0, 0.44f, 0);
        rimGo.transform.localScale = new Vector3(0.58f, 0.04f, 0.58f);
        DestroyImmediate(rimGo.GetComponent<Collider>());
        SetMaterialColor(rimGo, new Color(0.14f, 0.13f, 0.15f), 0.25f);

        // 3. Ceniza / Sustrato negro carbón
        GameObject ashGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ashGo.name = "Ceniza_Sustrato";
        ashGo.transform.SetParent(root.transform, false);
        ashGo.transform.localPosition = new Vector3(0, 0.43f, 0);
        ashGo.transform.localScale = new Vector3(0.48f, 0.035f, 0.48f);
        DestroyImmediate(ashGo.GetComponent<Collider>());
        SetMaterialColor(ashGo, new Color(0.06f, 0.06f, 0.08f), 0.1f);

        // 4. Restos carbonizados de tallos de Ella
        CreateCharredSprout(root.transform, "TalloQuemado_1", new Vector3(0.02f, 0.52f, 0.02f), new Vector3(0.045f, 0.16f, 0.045f), new Vector3(14f, 30f, -8f));
        CreateCharredSprout(root.transform, "TalloQuemado_2", new Vector3(-0.06f, 0.50f, -0.03f), new Vector3(0.04f, 0.12f, 0.04f), new Vector3(-18f, -15f, 12f));
        CreateCharredSprout(root.transform, "TalloQuemado_3", new Vector3(0.05f, 0.48f, -0.05f), new Vector3(0.035f, 0.10f, 0.035f), new Vector3(8f, 110f, -14f));

        // 5. Brasa violeta apagada (núcleo resplandeciente en la ceniza)
        GameObject emberGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        emberGo.name = "Brasa_Violeta_Nucleo";
        emberGo.transform.SetParent(root.transform, false);
        emberGo.transform.localPosition = new Vector3(0, 0.46f, 0);
        emberGo.transform.localScale = new Vector3(0.12f, 0.06f, 0.12f);
        DestroyImmediate(emberGo.GetComponent<Collider>());
        SetMaterialEmission(emberGo, violetGlowColor);

        // 6. Luz misteriosa violeta ténue
        GameObject lightGo = new GameObject("LuzVioletaApagada");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.localPosition = new Vector3(0, 0.55f, 0);
        violetLight = lightGo.AddComponent<Light>();
        violetLight.type = LightType.Point;
        violetLight.color = violetGlowColor;
        violetLight.range = 2.4f;
        violetLight.intensity = lightIntensityBase;
        violetLight.shadows = LightShadows.None;
    }

    private void CreateCharredSprout(Transform parent, string name, Vector3 pos, Vector3 scale, Vector3 rot)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(rot);
        go.transform.localScale = scale;
        DestroyImmediate(go.GetComponent<Collider>());
        SetMaterialColor(go, new Color(0.05f, 0.04f, 0.06f), 0.05f);
    }

    private void SetMaterialColor(GameObject go, Color color, float smoothness)
    {
        var renderer = go.GetComponent<MeshRenderer>();
        if (!renderer) return;

        // Intentar usar un material base del proyecto o crear una instancia Standard
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = color;
        mat.SetFloat("_Smoothness", smoothness);
        renderer.sharedMaterial = mat;
    }

    private void SetMaterialEmission(GameObject go, Color emissionColor)
    {
        var renderer = go.GetComponent<MeshRenderer>();
        if (!renderer) return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = emissionColor * 0.4f;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emissionColor * 1.5f);
        renderer.sharedMaterial = mat;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out CORE_PlayerController pc))
        {
            player = pc;
            inRange = true;
            CORE_InteractionHUD.SetPrompt(this, inspectPrompt);
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
        // Respiración / pulsación sutil del brillo violeta de la ceniza
        if (violetLight != null)
        {
            float pulse = Mathf.Sin(Time.time * 2.2f) * lightIntensityPulse;
            violetLight.intensity = lightIntensityBase + pulse;
        }

        if (!inRange || player == null) return;
        if (NotebookController.Instance && NotebookController.Instance.IsOpen) return;

        if (player.InteractPressed)
        {
            CORE_InteractionHUD.ShowMessage(inspectMessage, 3.5f);
        }
    }
}
