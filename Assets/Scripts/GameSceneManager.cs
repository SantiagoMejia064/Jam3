using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Persistent, scene-name based navigation entry point.</summary>
public sealed class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }

    [Header("Scene names (set these to the exact names in Build Settings)")]
    [SerializeField] private string inicioScene = "Inicio";
    [SerializeField] private string tallerScene;
    [SerializeField] private string salaMaquinasScene;
    [SerializeField] private string bodegaFriaScene;
    [SerializeField] private string invernaderoScene;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadInicio() => LoadScene(inicioScene);
    public void LoadTaller() => LoadScene(tallerScene);
    public void LoadSalaMaquinas() => LoadScene(salaMaquinasScene);
    public void LoadBodegaFria() => LoadScene(bodegaFriaScene);
    public void LoadInvernadero() => LoadScene(invernaderoScene);

    public void LoadSceneByName(string sceneName) => LoadScene(sceneName);

    private static void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("GameSceneManager: configure the destination scene name first.");
            return;
        }

        string resolved = CORE_SceneResolver.ResolveSceneName(sceneName);
        SceneManager.LoadScene(resolved);
    }
}
