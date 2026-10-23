using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class CORE_InventoryUI : MonoBehaviour
{
    public CORE_Inventory inventory;

    public GameObject slotPrefab;
    public Transform slotsParent;

    public TMP_Text pageText;
    public TMP_Text selectedText;

    public Button previousButton;
    public Button nextButton;

    private List<CORE_ItemUI> slots = new List<CORE_ItemUI>();

    private int page = 0;
    private int slotsPerPage = 4;

    void Awake()
    {
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
        if (inventory == null || slotPrefab == null || slotsParent == null) return;
        // Creamos las cuatro casillas una sola vez.
        if (slots.Count == 0)
        {
            for (int i = 0; i < slotsPerPage; i++)
            {
                GameObject slotObject =
                    Instantiate(slotPrefab, slotsParent, false);

                CORE_ItemUI slot =
                    slotObject.GetComponent<CORE_ItemUI>();

                slots.Add(slot);
            }
        }

        int totalPages = Mathf.Max(1, Mathf.CeilToInt(inventory.items.Count / (float)slotsPerPage));

        page = Mathf.Clamp(page, 0, totalPages - 1);

        for (int i = 0; i < slotsPerPage; i++)
        {
            int itemIndex = page * slotsPerPage + i;

            CORE_ItemData item = null;

            if (itemIndex < inventory.items.Count)
                item = inventory.items[itemIndex];

            bool selected = item != null && item == inventory.selectedItem;

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

        if (keyboard == null || slots.Count == 0)
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
