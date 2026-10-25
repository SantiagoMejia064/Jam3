using UnityEngine;
using TMPro;

public class CORE_SubtitlesUI : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text subtitleText;

    void Update()
    {
        var audio = SAL_AudioDirector.Instance;
        bool notebookOpen = NotebookController.Instance && NotebookController.Instance.IsOpen;
        bool visible = audio && !notebookOpen && !string.IsNullOrEmpty(audio.Subtitle) &&
                       Time.unscaledTime < audio.SubtitleUntil;
        if (panel) panel.SetActive(visible);
        if (visible && subtitleText && subtitleText.text != audio.Subtitle)
            subtitleText.text = audio.Subtitle;
    }
}
