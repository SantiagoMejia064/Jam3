using UnityEngine;

/// <summary>Readable SAL HUD. The shared NotebookController owns the notebook in every scene.</summary>
public sealed class SAL_UI : MonoBehaviour
{
    [SerializeField] private SAL_RoomController room;
    [SerializeField] private SAL_InteractionDriver interaction;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle smallStyle;
    private Texture2D background;

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
            float y = room.StandaloneDemo ? 176 : 122;
            Panel(new Rect(Screen.width * .5f - 225, y, 450, 55));
            string prompt = target.holdToClean ? "Mantén E · " + target.prompt : "E · " + target.prompt;
            if (target.holdToClean && interaction.HoldProgress > 0f) prompt += "  " + Mathf.RoundToInt(interaction.HoldProgress * 100f) + "%";
            GUI.Label(new Rect(Screen.width * .5f - 206, y + 8, 420, 44), prompt, bodyStyle);
        }

        if (Time.unscaledTime < room.MessageUntil)
        {
            float y = room.StandaloneDemo ? 238 : 184;
            Panel(new Rect(Screen.width * .5f - 340, y, 680, 56));
            GUI.Label(new Rect(Screen.width * .5f - 326, y + 7, 652, 45), room.Message, bodyStyle);
        }

        SAL_AudioDirector audio = SAL_AudioDirector.Instance;
        if (audio && !string.IsNullOrEmpty(audio.Subtitle) && Time.unscaledTime < audio.SubtitleUntil)
        {
            float y = room.StandaloneDemo ? 304 : 246;
            Panel(new Rect(Screen.width * .5f - 365, y, 730, 60));
            GUI.Label(new Rect(Screen.width * .5f - 347, y + 9, 694, 46), audio.Subtitle, bodyStyle);
        }
    }
}
