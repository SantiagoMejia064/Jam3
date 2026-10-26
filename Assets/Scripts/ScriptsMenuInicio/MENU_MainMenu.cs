using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Unicamente las acciones de Jugar y Salir; no cambia los puzles del juego.</summary>
[DisallowMultipleComponent]
public sealed class MENU_MainMenu : MonoBehaviour
{
    [SerializeField] private string gameScenePath = "Assets/Scenes/Inicio-Laboratorio.unity";
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private TMP_Text statusText;
    private bool loading;

    public void Configure(Button play, Button quit, GameObject panel, TMP_Text loadingLabel, TMP_Text status)
    {
        playButton = play; quitButton = quit; loadingPanel = panel; loadingText = loadingLabel; statusText = status;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        if (loadingPanel) loadingPanel.SetActive(false);
        if (statusText) statusText.text = string.Empty;
        if (EventSystem.current && playButton)
            EventSystem.current.SetSelectedGameObject(playButton.gameObject);
    }

    public void Play()
    {
        if (loading) return;
        string sceneToLoad = CORE_SceneResolver.ResolveSceneName(gameScenePath);
        if (!Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            if (statusText) statusText.text = "No se encuentra la escena inicial del juego.";
            Debug.LogError("MenuInicio: falta " + sceneToLoad + " en las escenas del build.", this);
            return;
        }
        loading = true;
        if (playButton) playButton.interactable = false;
        if (quitButton) quitButton.interactable = false;
        if (loadingPanel) loadingPanel.SetActive(true);
        if (loadingText) loadingText.text = "Cargando…";
        StartCoroutine(LoadGame());
    }

    private IEnumerator LoadGame()
    {
        yield return null;
        Time.timeScale = 1f;
        SAL_Progress.ResetForNewGame();
        SAL_Progress.SetAct(1);
        // El inventario de este checkout ya no se crea automaticamente al arrancar.
        // Se instancia al Jugar para no mostrar su barra encima del menu.
        if (!CORE_Inventory.Instance)
        {
            GameObject inventory = Resources.Load<GameObject>("CORE_Inventario");
            if (inventory) Instantiate(inventory);
        }
        if (CORE_Inventory.Instance) CORE_Inventory.Instance.ResetForNewGame();
        string sceneToLoad = CORE_SceneResolver.ResolveSceneName(gameScenePath);
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Single);
        while (operation != null && !operation.isDone) yield return null;
    }

    public void Quit()
    {
        if (loading) return;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
