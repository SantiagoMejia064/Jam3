using System.Collections;
using UnityEngine;

/// <summary>Wakes the existing scene-authored dock display and types its first message.</summary>
public sealed class CORE_ScreenWake : MonoBehaviour
{
    [SerializeField] private Renderer screenSurface;
    [SerializeField] private TextMesh display;
    [SerializeField] private string audioLogId = "A1";
    [SerializeField] private string onceFlag = "LAB_A1Reproducido";
    [SerializeField, TextArea(3, 6)] private string openingMessage =
        "A1\nFORMATEO COMPLETADO.\nMOTIVO: DESOBEDIENCIA.\n\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588";
    [SerializeField] private Color sleepingColor = new Color(0.015f, 0.025f, 0.035f, 1f);
    [SerializeField] private Color awakeColor = new Color(0.12f, 0.62f, 0.96f, 1f);
    [SerializeField, Min(0f)] private float wakeDelay = 0.45f;
    [SerializeField, Min(0.005f)] private float characterDelay = 0.045f;
    [SerializeField, Min(0f)] private float subtitleSeconds = 4f;

    private MaterialPropertyBlock block;
    private bool wakeStarted;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        SetSurfaceColor(sleepingColor);
        if (display)
        {
            display.text = string.Empty;
            display.color = new Color(0.72f, 0.94f, 1f, 1f);
        }
    }

    private void Start()
    {
        if (!SAL_Progress.HasFlag(onceFlag)) WakeFromInteraction();
        else
        {
            wakeStarted = true;
            SetSurfaceColor(awakeColor);
            if (display) display.text = openingMessage;
        }
    }

    /// <summary>Called by the scene-authored E interaction on the dock screen.</summary>
    public void WakeFromInteraction()
    {
        if (wakeStarted)
        {
            if (SAL_Progress.HasFlag(onceFlag) && SAL_AudioDirector.Instance)
                SAL_AudioDirector.Instance.PlayLog(audioLogId, SAL_Progress.HasFlag(SAL_Progress.Revealed), subtitleSeconds);
            return;
        }
        wakeStarted = true;
        StartCoroutine(WakeSequence());
    }

    private IEnumerator WakeSequence()
    {
        yield return new WaitForSeconds(wakeDelay);
        SetSurfaceColor(awakeColor);
        if ((string.IsNullOrEmpty(onceFlag) || !SAL_Progress.HasFlag(onceFlag)) &&
            !string.IsNullOrEmpty(audioLogId) && SAL_AudioDirector.Instance)
        {
            SAL_AudioDirector.Instance.PlayLog(audioLogId, false, subtitleSeconds);
            if (!string.IsNullOrEmpty(onceFlag)) SAL_Progress.SetFlag(onceFlag);
        }
        if (!display) yield break;

        for (int visible = 1; visible <= openingMessage.Length; visible++)
        {
            display.text = openingMessage.Substring(0, visible);
            yield return new WaitForSeconds(characterDelay);
        }
    }

    private void SetSurfaceColor(Color color)
    {
        if (!screenSurface) return;
        screenSurface.GetPropertyBlock(block);
        Material material = screenSurface.sharedMaterial;
        if (material && material.HasProperty(BaseColorId)) block.SetColor(BaseColorId, color);
        if (material && material.HasProperty(ColorId)) block.SetColor(ColorId, color);
        screenSurface.SetPropertyBlock(block);
    }
}
