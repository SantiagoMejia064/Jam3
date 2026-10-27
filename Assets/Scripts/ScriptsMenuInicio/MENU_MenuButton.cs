using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Mismo estilo con raton, teclado y mando; el texto sigue siendo editable.</summary>
[AddComponentMenu("Jam3/Menu/Boton de inicio")]
public sealed class MENU_MenuButton : Button
{
    [SerializeField] private MENU_ButtonFrame frame;
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text pointer;
    [SerializeField] private Image background;
    [SerializeField] private Color normalText = new Color(.88f, .85f, .75f, 1f);
    [SerializeField] private Color accent = new Color(1f, .65f, .23f, 1f);

    public void Configure(MENU_ButtonFrame border, TMP_Text text, TMP_Text cursor, Image fill)
    {
        frame = border; label = text; pointer = cursor; background = fill;
        transition = Transition.None;
        DoStateTransition(SelectionState.Normal, true);
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        if (!gameObject.activeInHierarchy) return;
        bool active = state == SelectionState.Highlighted || state == SelectionState.Selected || state == SelectionState.Pressed;
        float alpha = state == SelectionState.Disabled ? .35f : 1f;
        if (frame) frame.color = active ? accent : new Color(.62f, .48f, .29f, .44f * alpha);
        if (label) label.color = (active ? accent : normalText) * new Color(1f, 1f, 1f, alpha);
        if (pointer) pointer.color = active ? accent : Color.clear;
        if (background) background.color = background.sprite
            ? new Color(active ? 1.1f : 1f, active ? 1.05f : 1f, active ? 1.15f : 1f, alpha)
            : new Color(.018f, .028f, .045f, active ? .43f : .16f);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        if (IsActive() && IsInteractable() && EventSystem.current)
            EventSystem.current.SetSelectedGameObject(gameObject, eventData);
    }
}
