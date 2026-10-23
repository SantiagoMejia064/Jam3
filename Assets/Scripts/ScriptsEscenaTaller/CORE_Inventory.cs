using System.Collections.Generic;
using UnityEngine;

public class CORE_Inventory : MonoBehaviour
{
    public static CORE_Inventory Instance;

    public CORE_InventoryUI ui;

    // Todos los objetos que existen en el juego.
    public List<CORE_ItemData> itemCatalog = new List<CORE_ItemData>();

    // Los objetos que tiene el jugador.
    public List<CORE_ItemData> items = new List<CORE_ItemData>();

    public CORE_ItemData selectedItem;

    // Recordamos los recogidos, aunque después se utilicen.
    private List<string> collectedItems = new List<string>();

    // Permite iniciar desde cualquier sala con el mismo inventario configurado.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CreateInventory()
    {
        if (Instance != null) return;
        GameObject prefab = Resources.Load<GameObject>("CORE_Inventario");
        if (prefab != null) Instantiate(prefab);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (Instance != this) return;
        UpdateUI();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool TryAddItem(string id)
    {
        if (WasCollected(id))
        {
            return false;
        }
        foreach (CORE_ItemData item in itemCatalog)
        {
            if (item != null && item.id == id)
            {
                items.Add(item);
                collectedItems.Add(id);

                UpdateUI();
                return true;
            }
        }

        Debug.LogWarning("No se encontró el objeto: " + id);
        return false;
    }

    public bool HasItem(string id)
    {
        foreach (CORE_ItemData item in items)
        {
            if (item != null && item.id == id)
            {
                return true;
            }
        }
        return false;
    }

    public bool WasCollected(string id)
    {
        return collectedItems.Contains(id);
    }

    public void SelectItem(CORE_ItemData item)
    {
        if (!items.Contains(item))
        {
            return;
        }
        selectedItem = item;
        UpdateUI();
    }

    public bool IsSelected(string id)
    {
        return selectedItem != null && selectedItem.id == id;
    }

    public void RemoveItem(string id)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].id == id)
            {
                if (selectedItem == items[i])
                {
                    selectedItem = null;
                }
                items.RemoveAt(i);
                UpdateUI();
                return;
            }
        }
    }

    public void ResetForNewGame()
    {
        items.Clear();
        collectedItems.Clear();
        selectedItem = null;

        UpdateUI();
    }

    void UpdateUI()
    {
        if (ui != null)
        {
            ui.Refresh();
        }
    }
}
