using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>
/// Controla el giro visual izquierda/derecha (A / D) de Siete sin deformar ni mover sus generadores de sombra.
/// </summary>
[DisallowMultipleComponent]
public sealed class INV_PlayerFacing : MonoBehaviour
{
    [Tooltip("+1 = Derecha (D), -1 = Izquierda (A)")]
    [SerializeField] private int initialFacingSign = 1;

    /// <summary>+1 cuando Siete mira a la derecha (+X), -1 cuando mira a la izquierda (-X).</summary>
    public int FacingSign { get; private set; } = 1;

    private CORE_PlayerController playerController;
    private Rigidbody playerBody;
    private Transform sieteVisual;
    private Vector3 baseVisualScale = Vector3.one;
    private Vector3 baseVisualLocalPos = Vector3.zero;
    private float absVisualScaleX = 1f;
    private float absVisualPosX = 0f;

    private SpriteRenderer fallbackSprite;
    private readonly List<Material> mirroredShadowMaterials = new List<Material>();
    private bool rigPrepared = false;

    private void Awake()
    {
        FacingSign = initialFacingSign >= 0 ? 1 : -1;
        PrepareRig();
        ApplyFacingVisuals();
    }

    private void Start()
    {
        PrepareRig();
        ApplyFacingVisuals();
    }

    public void PrepareRig()
    {
        if (rigPrepared && sieteVisual != null) return;

        playerController = GetComponent<CORE_PlayerController>();
        playerBody = GetComponent<Rigidbody>();

        // 1. Localizar el rig visual hijo (SietePlayer)
        if (sieteVisual == null)
        {
            Transform found = transform.Find("SietePlayer");
            if (found == null)
            {
                var anim = GetComponentInChildren<Animator>(true);
                if (anim != null && anim.transform != transform)
                {
                    found = anim.transform;
                }
            }
            sieteVisual = found;
        }

        if (sieteVisual != null)
        {
            baseVisualScale = sieteVisual.localScale;
            baseVisualLocalPos = sieteVisual.localPosition;
            absVisualScaleX = Mathf.Abs(baseVisualScale.x) > 0.0001f ? Mathf.Abs(baseVisualScale.x) : 1f;
            absVisualPosX = Mathf.Abs(baseVisualLocalPos.x);

            // 2. Desacoplar los generadores de sombra hacia CORE_Siete ANTES de invertir escala en SietePlayer
            //    Así mantienen su posición exacta, su ángulo respecto a la luz direccional y escala positiva.
            DetachAndProtectShadows();

            // 3. Limpiar controlador y Rigidbody duplicados en el hijo visual SietePlayer
            CleanupDuplicateChildPhysics(sieteVisual.gameObject);

            rigPrepared = true;
        }
        else
        {
            fallbackSprite = GetComponentInChildren<SpriteRenderer>(true);
            rigPrepared = fallbackSprite != null;
        }
    }

    private void DetachAndProtectShadows()
    {
        var renderers = sieteVisual.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var mr in renderers)
        {
            if (mr == null) continue;

            bool isShadowQuad = mr.shadowCastingMode == ShadowCastingMode.ShadowsOnly ||
                                mr.name.IndexOf("sombra", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isShadowQuad) continue;

            if (mr.TryGetComponent(out MeshCollider mc))
            {
                mc.convex = true;
                mc.enabled = false;
            }

            // Re-emparentar directamente a CORE_Siete conservando posición, rotación y escala mundial exacta
            if (mr.transform.parent != transform)
            {
                mr.transform.SetParent(transform, true);
            }

            // Si el generador de sombra usa la silueta de Siete con textura, registrar instancia para espejar UV en sitio
            if (Application.isPlaying && mr.sharedMaterial != null)
            {
                bool hasTex = (mr.sharedMaterial.HasProperty("_BaseMap") && mr.sharedMaterial.GetTexture("_BaseMap") != null) ||
                              (mr.sharedMaterial.HasProperty("_MainTex") && mr.sharedMaterial.GetTexture("_MainTex") != null);
                if (hasTex)
                {
                    Material matInstance = mr.material;
                    if (!mirroredShadowMaterials.Contains(matInstance))
                    {
                        mirroredShadowMaterials.Add(matInstance);
                    }
                }
            }
        }
    }

    private void CleanupDuplicateChildPhysics(GameObject visualChild)
    {
        if (visualChild == null || visualChild == gameObject) return;

        var dupController = visualChild.GetComponent<CORE_PlayerController>();
        var dupRb = visualChild.GetComponent<Rigidbody>();

        if (dupController != null)
        {
            dupController.enabled = false;
            Destroy(dupController);
        }

        if (dupRb != null)
        {
            dupRb.isKinematic = true;
            dupRb.detectCollisions = false;
            StartCoroutine(DestroyChildRigidbodyNextFrame(dupRb));
        }
    }

    private static IEnumerator DestroyChildRigidbodyNextFrame(Rigidbody rb)
    {
        yield return null;
        if (rb != null)
        {
            Destroy(rb);
        }
    }

    private void Update()
    {
        if (!rigPrepared) PrepareRig();

        // Respetar pausas de movimiento (UI de afinación, cuaderno, etc.)
        if (playerController != null && !playerController.enabled) return;
        if (NotebookController.Instance != null && NotebookController.Instance.IsOpen) return;

        var kb = Keyboard.current;

        bool leftJustPressed = (kb != null && (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)) ||
                               Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
        bool rightJustPressed = (kb != null && (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)) ||
                                Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);

        if (leftJustPressed && !rightJustPressed)
        {
            SetFacing(-1);
            return;
        }
        if (rightJustPressed && !leftJustPressed)
        {
            SetFacing(1);
            return;
        }

        bool leftHeld = (kb != null && (kb.aKey.isPressed || kb.leftArrowKey.isPressed)) ||
                        Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool rightHeld = (kb != null && (kb.dKey.isPressed || kb.rightArrowKey.isPressed)) ||
                         Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);

        if (leftHeld && !rightHeld)
        {
            SetFacing(-1);
        }
        else if (rightHeld && !leftHeld)
        {
            SetFacing(1);
        }
        else if (!leftHeld && !rightHeld && playerBody != null)
        {
            float vx = playerBody.linearVelocity.x;
            if (vx < -0.2f) SetFacing(-1);
            else if (vx > 0.2f) SetFacing(1);
        }
    }

    private void LateUpdate()
    {
        ApplyFacingVisuals();
    }

    public void SetFacing(int sign)
    {
        int normalized = sign < 0 ? -1 : 1;
        if (FacingSign == normalized) return;

        FacingSign = normalized;
        ApplyFacingVisuals();
    }

    private void ApplyFacingVisuals()
    {
        if (sieteVisual != null)
        {
            Vector3 s = sieteVisual.localScale;
            s.x = absVisualScaleX * FacingSign;
            s.y = baseVisualScale.y;
            s.z = baseVisualScale.z;
            sieteVisual.localScale = s;

            Vector3 p = sieteVisual.localPosition;
            p.x = absVisualPosX * FacingSign;
            sieteVisual.localPosition = p;
        }
        else if (fallbackSprite != null)
        {
            fallbackSprite.flipX = FacingSign < 0;
        }

        // Espejar únicamente el UV horizontal de la silueta sin alterar la geometría ni el ángulo de sombra
        Vector2 uvScale = new Vector2(FacingSign, 1f);
        Vector2 uvOffset = new Vector2(FacingSign < 0 ? 1f : 0f, 0f);
        for (int i = 0; i < mirroredShadowMaterials.Count; i++)
        {
            Material mat = mirroredShadowMaterials[i];
            if (mat == null) continue;
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTextureScale("_BaseMap", uvScale);
                mat.SetTextureOffset("_BaseMap", uvOffset);
            }
            if (mat.HasProperty("_MainTex"))
            {
                mat.SetTextureScale("_MainTex", uvScale);
                mat.SetTextureOffset("_MainTex", uvOffset);
            }
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < mirroredShadowMaterials.Count; i++)
        {
            if (mirroredShadowMaterials[i] != null)
            {
                Destroy(mirroredShadowMaterials[i]);
            }
        }
        mirroredShadowMaterials.Clear();
    }
}
