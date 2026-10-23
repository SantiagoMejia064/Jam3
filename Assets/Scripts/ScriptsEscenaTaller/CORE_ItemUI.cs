using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CORE_ItemUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text keyText;
    public GameObject selectionBorder;

    private CORE_ItemData item;

    void Awake()
    {
        Button button = GetComponent<Button>();
        button.onClick.RemoveListener(Select);
        button.onClick.AddListener(Select);
    }

    public void Initialize(CORE_ItemData newItem, int key, bool selected)
    {
        item = newItem;

        keyText.text = key.ToString();

        if (item == null)
        {
            nameText.text = "Vacío";
            icon.enabled = false;
            selectionBorder.SetActive(false);
            return;
        }

        nameText.text = item.displayName;

        icon.sprite = item.icon;
        icon.color = Color.white;
        icon.preserveAspect = true;
        icon.enabled = item.icon != null;

        selectionBorder.SetActive(selected);
    }

    public void Select()
    {
        if (NotebookController.Instance != null && NotebookController.Instance.IsOpen) return;
        if (item != null)
        {
            CORE_Inventory.Instance.SelectItem(item);
        }
    }
}
