using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TAL_CleaningProgressUI : MonoBehaviour
{
    public CORE_Dirt dirt;
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
        bool visible = dirt != null && dirt.IsCleaning;

        panel.SetActive(visible);

        if (!visible)
            return;

        float progress = dirt.Progress01;

        progressImage.fillAmount = progress;
        percentageText.text = Mathf.RoundToInt(progress * 100f) + "%";
    }

    void OnDisable()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}
