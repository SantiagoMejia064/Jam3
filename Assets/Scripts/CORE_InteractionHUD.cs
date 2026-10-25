using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Single readable screen-space prompt and feedback panel, authored in the scene hierarchy.</summary>
public sealed class CORE_InteractionHUD : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Text promptText;
    [SerializeField] private Text messageText;

    private static CORE_InteractionHUD instance;
    private readonly List<Object> promptOrder = new List<Object>();
    private readonly Dictionary<Object, string> prompts = new Dictionary<Object, string>();
    private float messageTime;

    private void Awake() { instance = this; Refresh(); }
    private void OnDestroy() { if (instance == this) instance = null; }

    private void Update()
    {
        if (messageTime > 0f) messageTime -= Time.deltaTime;
        if (messageTime <= 0f && messageText) messageText.text = string.Empty;
        Refresh();
    }

    public static void SetPrompt(Object source, string text)
    {
        if (!instance || !source) return;
        instance.prompts[source] = text;
        instance.promptOrder.Remove(source);
        instance.promptOrder.Add(source);
        instance.Refresh();
    }

    public static void ClearPrompt(Object source)
    {
        if (!instance || !source) return;
        instance.prompts.Remove(source);
        instance.promptOrder.Remove(source);
        instance.Refresh();
    }

    public static bool HasPrompt(Object source) => instance && source && instance.prompts.ContainsKey(source);

    public static void ShowMessage(string text, float seconds = 3.5f)
    {
        if (!instance || !instance.messageText) return;
        instance.messageText.text = text;
        instance.messageTime = seconds;
        instance.Refresh();
    }

    private void Refresh()
    {
        if (!promptText || !messageText || !panel) return;
        string activePrompt = string.Empty;
        for (int i = promptOrder.Count - 1; i >= 0; i--)
        {
            Object source = promptOrder[i];
            if (source && prompts.TryGetValue(source, out activePrompt)) break;
            promptOrder.RemoveAt(i);
        }
        promptText.text = activePrompt;
        if (messageTime <= 0f) messageText.text = string.Empty;
        bool notebookOpen = NotebookController.Instance && NotebookController.Instance.IsOpen;
        panel.SetActive(!notebookOpen &&
            (!string.IsNullOrEmpty(activePrompt) || !string.IsNullOrEmpty(messageText.text)));
    }
}
