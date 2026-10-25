using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class CORE_InventoryUI : MonoBehaviour
{
    public CORE_Inventory inventory;

    public GameObject slotPrefab;
    public Transform slotsParent;
    public CORE_ItemUI[] slots = new CORE_ItemUI[4];

    public TMP_Text pageText;
    public TMP_Text selectedText;

    public Button previousButton;
    public Button nextButton;

    private int page = 0;

    void Awake()
    {
        if ((slots == null || slots.Length == 0) && slotsParent != null)
            slots = slotsParent.GetComponentsInChildren<CORE_ItemUI>(true);

        if (previousButton != null)
        {
            previousButton.onClick.RemoveListener(PreviousPage);
            previousButton.onClick.AddListener(PreviousPage);
        }
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(NextPage);
            nextButton.onClick.AddListener(NextPage);
        }
    }

    public void Refresh()
    {
        if (inventory == null || slots == null || slots.Length == 0) return;
        int slotsPerPage = slots.Length;

        int totalPages = Mathf.Max(1, Mathf.CeilToInt(inventory.items.Count / (float)slotsPerPage));

        page = Mathf.Clamp(page, 0, totalPages - 1);

        for (int i = 0; i < slotsPerPage; i++)
        {
            int itemIndex = page * slotsPerPage + i;

            CORE_ItemData item = null;

            if (itemIndex < inventory.items.Count)
                item = inventory.items[itemIndex];

            bool selected = item != null && item == inventory.selectedItem;

            if (slots[i] != null)
                slots[i].Initialize(item, i + 1, selected);
        }

        pageText.text = (page + 1) + " / " + totalPages;

        previousButton.interactable = page > 0;
        nextButton.interactable = page < totalPages - 1;

        if (inventory.selectedItem != null)
        {
            selectedText.text = "Seleccionado: " + inventory.selectedItem.displayName;
        }
        else
        {
            selectedText.text = "Selecciona un objeto";
        }
    }

    void Update()
    {
        if (NotebookController.Instance != null && NotebookController.Instance.IsOpen)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || slots == null || slots.Length == 0)
            return;

        if (keyboard.digit1Key.wasPressedThisFrame)
            slots[0].Select();

        else if (keyboard.digit2Key.wasPressedThisFrame)
            slots[1].Select();

        else if (keyboard.digit3Key.wasPressedThisFrame)
            slots[2].Select();

        else if (keyboard.digit4Key.wasPressedThisFrame)
            slots[3].Select();
    }

    public void NextPage()
    {
        page++;
        Refresh();
    }

    public void PreviousPage()
    {
        page--;
        Refresh();
    }
}
