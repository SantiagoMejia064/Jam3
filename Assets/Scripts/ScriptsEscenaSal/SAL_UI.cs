using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Readable graybox HUD and temporary notebook view; can hand off to P4's Cuaderno later.</summary>
public sealed class SAL_UI : MonoBehaviour
{
    [SerializeField] private SAL_RoomController room;
    [SerializeField] private SAL_InteractionDriver interaction;
    private bool notebook;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle smallStyle;
    private Texture2D background;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) notebook = !notebook;
    }

    private void OnDestroy() { if (background) Destroy(background); }

    private void EnsureStyles()
    {
        if (background) return;
        background = new Texture2D(1, 1);
        background.SetPixel(0, 0, new Color(0.025f, 0.04f, 0.055f, 0.91f));
        background.Apply();
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold, wordWrap = true };
        titleStyle.normal.textColor = new Color(0.96f, 0.77f, 0.49f);
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
        bodyStyle.normal.textColor = Color.white;
        smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
        smallStyle.normal.textColor = new Color(0.77f, 0.86f, 0.88f);
    }

    private void Panel(Rect rect) { GUI.DrawTexture(rect, background); }

    private void OnGUI()
    {
        if (!room || !interaction) return;
        EnsureStyles();
        float width = Mathf.Min(Screen.width - 32, 730);
        Panel(new Rect(16, 16, width, 94));
        GUI.Label(new Rect(30, 22, width - 30, 31), "SALA DE MÁQUINAS  /  RONDA NOCTURNA", titleStyle);
        GUI.Label(new Rect(30, 56, width - 30, 40), "A / D: moverse     E: interactuar o mantener para limpiar     Tab: Cuaderno", smallStyle);

        if (room.StandaloneDemo)
        {
            Panel(new Rect(16, 116, width, 48));
            GUI.Label(new Rect(28, 121, width - 24, 40), "PRUEBA SAL: Pelota, sello ▲ y sello ✚ disponibles para recorrer esta sala.", smallStyle);
        }

        SAL_Interactable target = interaction.Target;
        if (target)
        {
            Panel(new Rect(Screen.width * .5f - 225, Screen.height - 133, 450, 55));
            string prompt = target.holdToClean ? "Mantén E · " + target.prompt : "E · " + target.prompt;
            if (target.holdToClean && interaction.HoldProgress > 0f) prompt += "  " + Mathf.RoundToInt(interaction.HoldProgress * 100f) + "%";
            GUI.Label(new Rect(Screen.width * .5f - 206, Screen.height - 125, 420, 44), prompt, bodyStyle);
        }

        if (Time.unscaledTime < room.MessageUntil)
        {
            Panel(new Rect(Screen.width * .5f - 340, Screen.height - 76, 680, 56));
            GUI.Label(new Rect(Screen.width * .5f - 326, Screen.height - 69, 652, 45), room.Message, bodyStyle);
        }

        SAL_AudioDirector audio = SAL_AudioDirector.Instance;
        if (audio && !string.IsNullOrEmpty(audio.Subtitle) && Time.unscaledTime < audio.SubtitleUntil)
        {
            Panel(new Rect(Screen.width * .5f - 365, Screen.height - 202, 730, 60));
            GUI.Label(new Rect(Screen.width * .5f - 347, Screen.height - 193, 694, 46), audio.Subtitle, bodyStyle);
        }

        if (!notebook) return;
        Rect book = new Rect(Mathf.Max(16, Screen.width * .5f - 365), 180, Mathf.Min(730, Screen.width - 32), Mathf.Min(390, Screen.height - 205));
        Panel(book);
        GUI.Label(new Rect(book.x + 20, book.y + 14, book.width - 40, 34), "CUADERNO  /  SAL", titleStyle);
        float y = book.y + 54;
        string[] lines =
        {
            "N5  " + (SAL_Progress.HasFlag(SAL_Progress.PanelSolved) ? room.GetNote("N5") : "Pendiente: panel de reciclaje"),
            "N6  " + (SAL_Progress.HasFlag(SAL_Progress.DrawingsFound) ? room.GetNote("N6") : "Pendiente: dibujos"),
            "N7  " + (SAL_Progress.HasFlag(SAL_Progress.LabelRead) ? room.GetNote("N7") : "Pendiente: etiqueta del filtro"),
            "N9  " + (SAL_Progress.HasFlag(SAL_Progress.SeedObtained) ? room.GetNote("N9") : "Pendiente: semilla"),
            "Sellos  ▲ " + Mark(SAL_Progress.TriangleSeal) + "   ◆ " + Mark(SAL_Progress.DiamondSeal) + "   ✚ " + Mark(SAL_Progress.CrossSeal),
            "Objetos  Pelota " + MarkItem("Pelota") + "   Bruma " + MarkItem("PiezaBruma") + "   Semilla " + MarkItem("Semilla")
        };
        foreach (string line in lines) { GUI.Label(new Rect(book.x + 22, y, book.width - 44, 47), line, bodyStyle); y += 50; }
    }

    private static string Mark(string flag) => SAL_Progress.HasFlag(flag) ? "✓" : "·";
    private static string MarkItem(string item) => CORE_InventoryBridge.HasItem(item) ? "✓" : "·";
}
