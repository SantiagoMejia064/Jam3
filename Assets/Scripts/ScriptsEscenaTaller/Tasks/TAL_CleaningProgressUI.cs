using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TAL_CleaningProgressUI : MonoBehaviour
{
    public CORE_Dirt dirt;
    public BOD_Tray tray;
    public SAL_InteractionDriver salInteraction;
    public SAL_Interactable salTarget;
    public GameObject panel;
    public Image progressImage;
    public TMP_Text percentageText;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    void LateUpdate()
    {
        if (panel == null || progressImage == null || percentageText == null) return;
        bool visible = false;
        float progress = 0f;

        if (dirt != null)
        {
            visible = dirt.isActiveAndEnabled && dirt.IsCleaning;
            progress = dirt.Progress01;
        }
        else if (tray != null)
        {
            visible = tray.isActiveAndEnabled && tray.IsCleaning;
            progress = tray.Progress01;
        }
        else if (salInteraction != null && salTarget != null)
        {
            visible = salInteraction.isActiveAndEnabled &&
                      salTarget.isActiveAndEnabled &&
                      salInteraction.Target == salTarget &&
                      salTarget.holdToClean && salInteraction.HoldProgress > 0f;
            progress = salInteraction.HoldProgress;
        }

        panel.SetActive(visible);

        if (!visible)
            return;

        progressImage.fillAmount = Mathf.Clamp01(progress);
        percentageText.text = Mathf.RoundToInt(progress * 100f) + "%";
    }

    void OnDisable()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}
