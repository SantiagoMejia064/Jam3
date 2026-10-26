using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>Interfaz interactiva para el puzle de afinación de la incubadora (2-4-1-3).</summary>
public sealed class INV_IncubatorUI : MonoBehaviour
{
    [Header("Panel Principal")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button closeButton;

    [Header("Perillas (0 a 4)")]
    [SerializeField] private TMP_Text calorValueText;
    [SerializeField] private TMP_Text oscuridadValueText;
    [SerializeField] private TMP_Text pulsoValueText;
    [SerializeField] private TMP_Text brumaValueText;

    [SerializeField] private Button calorPlusBtn;
    [SerializeField] private Button calorMinusBtn;
    [SerializeField] private Button oscuridadPlusBtn;
    [SerializeField] private Button oscuridadMinusBtn;
    [SerializeField] private Button pulsoPlusBtn;
    [SerializeField] private Button pulsoMinusBtn;
    [SerializeField] private Button brumaPlusBtn;
    [SerializeField] private Button brumaMinusBtn;

    [Header("Anillos / Indicadores de Semilla")]
    [SerializeField] private Image ringCalor;
    [SerializeField] private Image ringOscuridad;
    [SerializeField] private Image ringPulso;
    [SerializeField] private Image ringBruma;

    [Header("Barra de Estabilidad (3 segundos)")]
    [SerializeField] private Slider stabilitySlider;
    [SerializeField] private TMP_Text statusText;

    [Header("Audio de Interfaz de Afinación")]
    [SerializeField] private AudioSource perillaClickSource;
    [SerializeField] private AudioSource afinacionCompletadaSource;
    [SerializeField] private AudioClip clipPerillaClick;
    [SerializeField] private AudioClip clipAfinacionCompletada;

    // Metas según N8 (GDD pág 10)
    public const int TargetCalor = 2;
    public const int TargetOscuridad = 4;
    public const int TargetPulso = 1;
    public const int TargetBruma = 3;

    private int valCalor = 0;
    private int valOscuridad = 0;
    private int valPulso = 0;
    private int valBruma = 0;

    private float stableHoldTimer = 0f;
    private const float RequiredHoldTime = 3.0f;
    private bool isSolved = false;
    private bool isInitialized = false;
    private bool isOpening = false;
    private Action onSolvedCallback;

    public bool IsOpen => panel != null && panel.activeInHierarchy;

    private void Awake()
    {
        EnsureInitialized();

        if (!isOpening && panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void EnsureInitialized()
    {
        if (isInitialized) return;
        isInitialized = true;

        if (!perillaClickSource)
        {
            perillaClickSource = GetComponent<AudioSource>();
            if (!perillaClickSource) perillaClickSource = gameObject.AddComponent<AudioSource>();
        }
        perillaClickSource.playOnAwake = false;
        perillaClickSource.spatialBlend = 0f;

        if (!afinacionCompletadaSource && transform.parent != null)
        {
            afinacionCompletadaSource = INV_SoundHelper.FindOrCreateChildSource(
                transform.parent,
                "Audio_Afinacion_Completada",
                0.85f,
                loop: false
            );
        }

        if (panel == null)
        {
            BuildRuntimeUI();
        }

        if (closeButton)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        if (calorPlusBtn) calorPlusBtn.onClick.AddListener(() => Adjust(ref valCalor, +1));
        if (calorMinusBtn) calorMinusBtn.onClick.AddListener(() => Adjust(ref valCalor, -1));

        if (oscuridadPlusBtn) oscuridadPlusBtn.onClick.AddListener(() => Adjust(ref valOscuridad, +1));
        if (oscuridadMinusBtn) oscuridadMinusBtn.onClick.AddListener(() => Adjust(ref valOscuridad, -1));

        if (pulsoPlusBtn) pulsoPlusBtn.onClick.AddListener(() => Adjust(ref valPulso, +1));
        if (pulsoMinusBtn) pulsoMinusBtn.onClick.AddListener(() => Adjust(ref valPulso, -1));

        if (brumaPlusBtn) brumaPlusBtn.onClick.AddListener(() => Adjust(ref valBruma, +1));
        if (brumaMinusBtn) brumaMinusBtn.onClick.AddListener(() => Adjust(ref valBruma, -1));
    }

    public void Open(Action onSolved)
    {
        isOpening = true;

        EnsureHierarchyActive(transform.parent);
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        EnsureInitialized();

        if (panel != null)
        {
            EnsureHierarchyActive(panel.transform.parent);
            panel.SetActive(true);
        }

        isOpening = false;

        if (panel != null && !panel.activeSelf)
        {
            panel.SetActive(true);
        }

        if (panel != null && panel.TryGetComponent(out CanvasGroup cg))
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        onSolvedCallback = onSolved;
        isSolved = false;
        stableHoldTimer = 0f;
        if (stabilitySlider) stabilitySlider.value = 0f;
        if (statusText) statusText.text = "Ajusta las 4 perillas según la planilla N8 (Teclas 1-4 / Z-V o botones + / -).";

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        RefreshVisuals();

        if (IsOpen)
        {
            SetPlayerMovement(false);
        }
        else
        {
            SetPlayerMovement(true);
        }
    }

    public void Close()
    {
        isOpening = false;
        if (panel != null)
        {
            panel.SetActive(false);
        }
        SetPlayerMovement(true);
    }

    private void OnDisable()
    {
        if (!isOpening)
        {
            SetPlayerMovement(true);
        }
    }

    private static void EnsureHierarchyActive(Transform t)
    {
        while (t != null)
        {
            if (!t.gameObject.activeSelf)
            {
                t.gameObject.SetActive(true);
            }
            t = t.parent;
        }
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

    private void Update()
    {
        if (!IsOpen || isSolved) return;

        var kb = Keyboard.current;
        bool escPressed = Input.GetKeyDown(KeyCode.Escape) || (kb != null && kb.escapeKey.wasPressedThisFrame);
        bool tabPressed = NotebookController.Instance && (Input.GetKeyDown(KeyCode.Tab) || (kb != null && kb.tabKey.wasPressedThisFrame));

        if (escPressed || tabPressed)
        {
            Close();
            return;
        }

        if (WasKeyPressed(KeyCode.Alpha1, KeyCode.Keypad1, kb?.digit1Key, kb?.numpad1Key)) Adjust(ref valCalor, +1);
        if (WasKeyPressed(KeyCode.Z, KeyCode.None, kb?.zKey, null)) Adjust(ref valCalor, -1);

        if (WasKeyPressed(KeyCode.Alpha2, KeyCode.Keypad2, kb?.digit2Key, kb?.numpad2Key)) Adjust(ref valOscuridad, +1);
        if (WasKeyPressed(KeyCode.X, KeyCode.None, kb?.xKey, null)) Adjust(ref valOscuridad, -1);

        if (WasKeyPressed(KeyCode.Alpha3, KeyCode.Keypad3, kb?.digit3Key, kb?.numpad3Key)) Adjust(ref valPulso, +1);
        if (WasKeyPressed(KeyCode.C, KeyCode.None, kb?.cKey, null)) Adjust(ref valPulso, -1);

        if (WasKeyPressed(KeyCode.Alpha4, KeyCode.Keypad4, kb?.digit4Key, kb?.numpad4Key)) Adjust(ref valBruma, +1);
        if (WasKeyPressed(KeyCode.V, KeyCode.None, kb?.vKey, null)) Adjust(ref valBruma, -1);

        CheckStability();
    }

    private static bool WasKeyPressed(
        KeyCode legacyPrimary,
        KeyCode legacySecondary,
        UnityEngine.InputSystem.Controls.KeyControl primaryCtrl,
        UnityEngine.InputSystem.Controls.KeyControl secondaryCtrl)
    {
        if (primaryCtrl != null && primaryCtrl.wasPressedThisFrame) return true;
        if (secondaryCtrl != null && secondaryCtrl.wasPressedThisFrame) return true;
        if (legacyPrimary != KeyCode.None && Input.GetKeyDown(legacyPrimary)) return true;
        if (legacySecondary != KeyCode.None && Input.GetKeyDown(legacySecondary)) return true;
        return false;
    }

    private void Adjust(ref int value, int delta)
    {
        int prev = value;
        value = Mathf.Clamp(value + delta, 0, 4);
        if (value != prev)
        {
            INV_SoundHelper.PlayEffect(
                perillaClickSource,
                clipPerillaClick,
                INV_SoundHelper.CreatePerillaClick(),
                0.65f
            );
            stableHoldTimer = 0f;
            RefreshVisuals();
        }
    }

    private void CheckStability()
    {
        bool allMatch = (valCalor == TargetCalor) &&
                        (valOscuridad == TargetOscuridad) &&
                        (valPulso == TargetPulso) &&
                        (valBruma == TargetBruma);

        if (allMatch)
        {
            stableHoldTimer += Time.deltaTime;
            if (statusText) statusText.text = $"<color=#A855F7>¡AFINACIÓN EXACTA! Estabilizando... ({stableHoldTimer:0.0}/3.0s)</color>";

            if (stableHoldTimer >= RequiredHoldTime)
            {
                isSolved = true;
                if (statusText) statusText.text = "<color=#A855F7>¡ESTABILIZADA! Iniciando floración...</color>";

                // Reproducir sonido en INV_Canvas_Incubadora/Audio_Afinacion_Completada (activo aunque se cierre el panel)
                if (afinacionCompletadaSource != null)
                {
                    INV_SoundHelper.PlayEffect(
                        afinacionCompletadaSource,
                        clipAfinacionCompletada,
                        INV_SoundHelper.CreateTuningCompleteChime(),
                        0.85f
                    );
                }

                var callback = onSolvedCallback;
                onSolvedCallback = null;
                Close();
                callback?.Invoke();
            }
        }
        else
        {
            stableHoldTimer = Mathf.Max(0f, stableHoldTimer - Time.deltaTime * 2f);
            if (statusText) statusText.text = "Ajusta las 4 perillas según la planilla N8.";
        }

        if (stabilitySlider)
        {
            stabilitySlider.value = Mathf.Clamp01(stableHoldTimer / RequiredHoldTime);
        }
    }

    private void RefreshVisuals()
    {
        if (calorValueText) calorValueText.text = valCalor.ToString();
        if (oscuridadValueText) oscuridadValueText.text = valOscuridad.ToString();
        if (pulsoValueText) pulsoValueText.text = valPulso.ToString();
        if (brumaValueText) brumaValueText.text = valBruma.ToString();

        UpdateRingColor(ringCalor, valCalor, TargetCalor);
        UpdateRingColor(ringOscuridad, valOscuridad, TargetOscuridad);
        UpdateRingColor(ringPulso, valPulso, TargetPulso);
        UpdateRingColor(ringBruma, valBruma, TargetBruma);
    }

    private void UpdateRingColor(Image ring, int current, int target)
    {
        if (!ring) return;
        int dist = Mathf.Abs(current - target);
        if (dist == 0)
        {
            ring.color = new Color(0.72f, 0.35f, 0.98f, 1f);
        }
        else if (dist == 1)
        {
            ring.color = new Color(0.55f, 0.45f, 0.65f, 0.75f);
        }
        else
        {
            ring.color = new Color(0.28f, 0.28f, 0.32f, 0.5f);
        }
    }

    private void BuildRuntimeUI()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (!canvas)
        {
            GameObject canvasGo = new GameObject("INV_RuntimeCanvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 25;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        panel = new GameObject("Panel_Afinacion_Runtime");
        panel.transform.SetParent(canvas.transform, false);
        var pRect = panel.AddComponent<RectTransform>();
        pRect.anchorMin = new Vector2(0.12f, 0.08f);
        pRect.anchorMax = new Vector2(0.88f, 0.92f);
        pRect.offsetMin = pRect.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);

        CreateRuntimeText(panel.transform, "Titulo", "INCUBADORA IMPROVISADA · AFINACIÓN DE FRECUENCIA", 26,
            new Vector2(0.5f, 0.92f), new Vector2(0, 0), new Vector2(900, 40),
            new Color(0.75f, 0.52f, 0.98f), TextAlignmentOptions.Center);

        CreateRuntimeText(panel.transform, "Subtitulo", "Ajusta las 4 perillas según la planilla N8 para que coincidan con la semilla", 17,
            new Vector2(0.5f, 0.86f), new Vector2(0, 0), new Vector2(850, 30),
            new Color(0.65f, 0.65f, 0.72f), TextAlignmentOptions.Center);

        string[] names = { "CALOR\n(Llama)", "OSCURIDAD\n(Luna)", "PULSO\n(Huellas)", "BRUMA\n(Gota)" };
        float startX = -360f;
        float spacingX = 240f;

        for (int i = 0; i < 4; i++)
        {
            float posX = startX + i * spacingX;
            GameObject col = new GameObject("Col_" + i);
            col.transform.SetParent(panel.transform, false);
            var colRect = col.AddComponent<RectTransform>();
            colRect.anchorMin = colRect.anchorMax = new Vector2(0.5f, 0.52f);
            colRect.anchoredPosition = new Vector2(posX, 20f);
            colRect.sizeDelta = new Vector2(200, 260);

            GameObject ringGo = new GameObject("Ring_" + i);
            ringGo.transform.SetParent(col.transform, false);
            var ringRect = ringGo.AddComponent<RectTransform>();
            ringRect.anchoredPosition = new Vector2(0, 75f);
            ringRect.sizeDelta = new Vector2(70, 70);
            var ringImg = ringGo.AddComponent<Image>();
            ringImg.color = new Color(0.3f, 0.3f, 0.35f);

            if (i == 0) ringCalor = ringImg;
            else if (i == 1) ringOscuridad = ringImg;
            else if (i == 2) ringPulso = ringImg;
            else if (i == 3) ringBruma = ringImg;

            CreateRuntimeText(col.transform, "Lbl_" + i, names[i], 18,
                new Vector2(0.5f, 0.5f), new Vector2(0, 10f), new Vector2(190, 45),
                new Color(0.85f, 0.85f, 0.9f), TextAlignmentOptions.Center);

            var valTxt = CreateRuntimeText(col.transform, "Val_" + i, "0", 38,
                new Vector2(0.5f, 0.5f), new Vector2(0, -45f), new Vector2(80, 50),
                Color.white, TextAlignmentOptions.Center);

            if (i == 0) calorValueText = valTxt;
            else if (i == 1) oscuridadValueText = valTxt;
            else if (i == 2) pulsoValueText = valTxt;
            else if (i == 3) brumaValueText = valTxt;

            var minusBtn = CreateRuntimeButton(col.transform, "Minus_" + i, "-", new Vector2(-55, -45), new Vector2(42, 42));
            var plusBtn = CreateRuntimeButton(col.transform, "Plus_" + i, "+", new Vector2(55, -45), new Vector2(42, 42));

            if (i == 0) { calorMinusBtn = minusBtn; calorPlusBtn = plusBtn; }
            else if (i == 1) { oscuridadMinusBtn = minusBtn; oscuridadPlusBtn = plusBtn; }
            else if (i == 2) { pulsoMinusBtn = minusBtn; pulsoPlusBtn = plusBtn; }
            else if (i == 3) { brumaMinusBtn = minusBtn; brumaPlusBtn = plusBtn; }
        }

        statusText = CreateRuntimeText(panel.transform, "StatusText", "Ajusta las 4 perillas según la planilla N8.", 18,
            new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(700, 32),
            new Color(0.85f, 0.85f, 0.9f), TextAlignmentOptions.Center);

        GameObject sliderGo = new GameObject("StabilitySlider");
        sliderGo.transform.SetParent(panel.transform, false);
        var sRect = sliderGo.AddComponent<RectTransform>();
        sRect.anchorMin = sRect.anchorMax = new Vector2(0.5f, 0.13f);
        sRect.anchoredPosition = Vector2.zero;
        sRect.sizeDelta = new Vector2(500, 16);
        stabilitySlider = sliderGo.AddComponent<Slider>();

        GameObject bgGo = new GameObject("Background");
        bgGo.transform.SetParent(sliderGo.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
        bgGo.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGo.transform, false);
        var faRect = fillArea.AddComponent<RectTransform>();
        faRect.anchorMin = Vector2.zero; faRect.anchorMax = Vector2.one;
        faRect.offsetMin = faRect.offsetMax = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fRect = fill.AddComponent<RectTransform>();
        fRect.anchorMin = Vector2.zero; fRect.anchorMax = Vector2.one;
        fRect.offsetMin = fRect.offsetMax = Vector2.zero;
        var fImg = fill.AddComponent<Image>();
        fImg.color = new Color(0.72f, 0.35f, 0.98f);
        stabilitySlider.fillRect = fRect;
        stabilitySlider.value = 0f;

        closeButton = CreateRuntimeButton(panel.transform, "CloseBtn", "Cerrar [Esc]", new Vector2(0, -280), new Vector2(160, 42));
    }

    private static TMP_Text CreateRuntimeText(Transform parent, string name, string content, float size,
        Vector2 anchor, Vector2 pos, Vector2 delta, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = pos; rect.sizeDelta = delta;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        return tmp;
    }

    private static Button CreateRuntimeButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos; rect.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.28f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.45f);
        colors.pressedColor = new Color(0.5f, 0.3f, 0.7f);
        btn.colors = colors;

        CreateRuntimeText(go.transform, "BtnText", label, 20, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            Color.white, TextAlignmentOptions.Center);

        return btn;
    }
}
