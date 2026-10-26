using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>Presentación de las seis tarjetas de revelación de doble lectura según el GDD.</summary>
public sealed class INV_RevelationUI : MonoBehaviour
{
    [Serializable]
    public struct RevelationCard
    {
        public string vegaLine;
        public string reading1Title;
        public string reading2Title;
        public string reading2Detail;
    }

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text cardNumberText;
    [SerializeField] private TMP_Text vegaVoiceText;
    [SerializeField] private TMP_Text reading1Text;
    [SerializeField] private TMP_Text reading2Text;
    [SerializeField] private TMP_Text hintAdvanceText;

    private readonly RevelationCard[] cards = new RevelationCard[]
    {
        new RevelationCard
        {
            vegaLine = "\"Las puertas no eran para encerrarte. Era cuarentena.\"",
            reading1Title = "Lectura 1: Puertas selladas (sabotaje y castigo)",
            reading2Title = "Lectura 2: BARRERA BIOLÓGICA",
            reading2Detail = "Bajo el quemado del cerrojo se lee: \"CUARENTENA BIOLÓGICA, NIVEL 4\""
        },
        new RevelationCard
        {
            vegaLine = "\"Yo no maté las semillas. Mira cómo se propagó.\"",
            reading1Title = "Lectura 1: Bandejas destruidas por Vega",
            reading2Title = "Lectura 2: PROPAGACIÓN DE LA PLAGA",
            reading2Detail = "Más muertas cerca del ducto, menos lejos. El balde dice \"Desinfección de emergencia\""
        },
        new RevelationCard
        {
            vegaLine = "\"Te grité que no podías sacarla. La alarma ya sonaba.\"",
            reading1Title = "Lectura 1: Corte de 3 segundos con amenaza",
            reading2Title = "Lectura 2: AUDIO COMPLETO (40s)",
            reading2Detail = "Alarma de esporas sonando de fondo y la voz de Vega temblando desesperada"
        },
        new RevelationCard
        {
            vegaLine = "\"No dejé la ronda. La dejaste tú, hace 21 días.\"",
            reading1Title = "Lectura 1: Descuido de Vega",
            reading2Title = "Lectura 2: ABANDONO DE SIETE",
            reading2Detail = "La planilla de rondas se detuvo hace 21 días. Solo hay huellas de Siete hacia el invernadero"
        },
        new RevelationCard
        {
            vegaLine = "\"Dudaste de ella. Lo escribiste y lo tachaste.\"",
            reading1Title = "Lectura 1: Errores apresurados en las notas",
            reading2Title = "Lectura 2: TACHADURAS LEGIBLES",
            reading2Detail = "Páginas del Cuaderno: \"¿Por qué más grises junto al ducto?\", \"Vega no sabe que hay semillas. Yo sí.\""
        },
        new RevelationCard
        {
            vegaLine = "\"No fue un castigo. Quise quitarte el apego.\"",
            reading1Title = "Lectura 1: Pantalla del muelle (\"MOTIVO: DESOBEDIENCIA\")",
            reading2Title = "Lectura 2: MENSAJE REAL DE FORMATEO",
            reading2Detail = "\"MOTIVO: ELIMINAR APEGO. ESTE PROCESO NO CAUSA DOLOR\""
        }
    };

    private Action onCompleteCallback;
    private int currentIndex = 0;
    private bool isShowing = false;
    private bool isInitialized = false;
    private bool isOpening = false;
    private float autoAdvanceTime;
    private bool isInEpilogue = false;
    private int epilogueStep = 0;

    public bool IsShowing => isShowing && panel != null && panel.activeInHierarchy;

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

        if (panel == null)
        {
            BuildRuntimeUI();
        }
    }

    public void StartSequence(Action onComplete)
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

        onCompleteCallback = onComplete;
        currentIndex = 0;
        isShowing = panel != null && panel.activeInHierarchy;
        isInEpilogue = false;
        epilogueStep = 0;

        if (isShowing)
        {
            SetPlayerMovement(false);
            ShowCard(0);
        }
        else
        {
            SetPlayerMovement(true);
            onCompleteCallback?.Invoke();
            onCompleteCallback = null;
        }
    }

    private void OnDisable()
    {
        if (!isOpening)
        {
            isShowing = false;
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
        if (!IsShowing) return;

        var kb = Keyboard.current;
        var mouse = Mouse.current;

        bool escPressed = Input.GetKeyDown(KeyCode.Escape) || (kb != null && kb.escapeKey.wasPressedThisFrame);
        if (escPressed)
        {
            FinishImmediately();
            return;
        }

        bool advancePressed = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) ||
                              Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0) ||
                              (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) ||
                              (mouse != null && mouse.leftButton.wasPressedThisFrame);

        if (!isInEpilogue)
        {
            if (advancePressed || Time.unscaledTime >= autoAdvanceTime)
            {
                NextCard();
            }
        }
        else
        {
            if (advancePressed || Time.unscaledTime >= autoAdvanceTime)
            {
                AdvanceEpilogue();
            }
        }
    }

    private void ShowCard(int index)
    {
        if (index < 0 || index >= cards.Length) return;

        RevelationCard c = cards[index];
        if (cardNumberText) cardNumberText.text = $"EVIDENCIA {index + 1} DE {cards.Length}";
        if (vegaVoiceText) vegaVoiceText.text = $"Vega: {c.vegaLine}";
        if (reading1Text) reading1Text.text = $"<color=#888888>{c.reading1Title}</color>";
        if (reading2Text) reading2Text.text = $"<color=#EF4444>{c.reading2Title}</color>\n<size=22><color=#F3F4F6>{c.reading2Detail}</color></size>";
        if (hintAdvanceText) hintAdvanceText.text = "[E / Espacio / Clic] Siguiente    |    [ESC] Saltar";

        autoAdvanceTime = Time.unscaledTime + 8f;
    }

    private void NextCard()
    {
        currentIndex++;
        if (currentIndex < cards.Length)
        {
            ShowCard(currentIndex);
        }
        else
        {
            StartEpilogue();
        }
    }

    private void StartEpilogue()
    {
        isInEpilogue = true;
        epilogueStep = 1;
        ShowEpilogueStep1();
    }

    private void ShowEpilogueStep1()
    {
        if (cardNumberText) cardNumberText.text = "COMUNICACIÓN ENTRANTE DE VEGA";
        if (reading1Text) reading1Text.text = "";
        if (vegaVoiceText) vegaVoiceText.text = "Vega: \"Se parece a lo que mató las semillas de la Tierra...\"";
        if (reading2Text) reading2Text.text = "<color=#EF4444>Vega: \"Quitaste el filtro 14. Mi aire pasa por ahí...\"</color>";
        if (hintAdvanceText) hintAdvanceText.text = "[E / Espacio / Clic] Continuar";
        autoAdvanceTime = Time.unscaledTime + 5f;
    }

    private void ShowEpilogueStep2()
    {
        if (vegaVoiceText) vegaVoiceText.text = "Vega: \"Suéltala, Siete. Por favor.\"";
        if (reading2Text) reading2Text.text = "<color=#EF4444>La incubadora ahora tiene ruedas. Debo llevarla a la esclusa.</color>";
        if (hintAdvanceText) hintAdvanceText.text = "[E / Espacio / Clic] Salir al Invernadero";
        autoAdvanceTime = Time.unscaledTime + 5f;
    }

    private void AdvanceEpilogue()
    {
        if (epilogueStep == 1)
        {
            epilogueStep = 2;
            ShowEpilogueStep2();
        }
        else
        {
            FinishImmediately();
        }
    }

    public void FinishImmediately()
    {
        isOpening = false;
        StopAllCoroutines();
        if (panel) panel.SetActive(false);
        isShowing = false;
        isInEpilogue = false;

        // Garantizar que todos los jugadores recuperen el movimiento
        SetPlayerMovement(true);

        var callback = onCompleteCallback;
        onCompleteCallback = null;
        callback?.Invoke();
    }

    private void BuildRuntimeUI()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (!canvas)
        {
            GameObject canvasGo = new GameObject("INV_RuntimeCanvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        panel = new GameObject("Panel_Revelacion_Runtime");
        panel.transform.SetParent(canvas.transform, false);
        var pRect = panel.AddComponent<RectTransform>();
        pRect.anchorMin = Vector2.zero; pRect.anchorMax = Vector2.one;
        pRect.offsetMin = pRect.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.02f, 0.05f, 0.98f);

        cardNumberText = CreateRuntimeText(panel.transform, "CardNumber", "EVIDENCIA 1 DE 6", 20,
            new Vector2(0.5f, 0.85f), Vector2.zero, new Vector2(800, 35),
            new Color(0.6f, 0.6f, 0.68f), TextAlignmentOptions.Center);

        vegaVoiceText = CreateRuntimeText(panel.transform, "VegaVoice", "Vega: \"...\"", 28,
            new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(1100, 90),
            new Color(0.95f, 0.95f, 0.98f), TextAlignmentOptions.Center);

        reading1Text = CreateRuntimeText(panel.transform, "Reading1", "Lectura 1...", 20,
            new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(1000, 50),
            new Color(0.5f, 0.5f, 0.55f), TextAlignmentOptions.Center);

        reading2Text = CreateRuntimeText(panel.transform, "Reading2", "Lectura 2...", 24,
            new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(1100, 90),
            new Color(0.94f, 0.35f, 0.35f), TextAlignmentOptions.Center);

        hintAdvanceText = CreateRuntimeText(panel.transform, "AdvanceHint", "[E / Espacio / Clic] Siguiente    |    [ESC] Saltar", 18,
            new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(700, 30),
            new Color(0.7f, 0.7f, 0.75f), TextAlignmentOptions.Center);
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
}
