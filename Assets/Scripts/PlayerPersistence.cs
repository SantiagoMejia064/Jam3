using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Keeps the player instance and places it at the loaded scene's SpawnPoint.</summary>
[DisallowMultipleComponent]
public sealed class PlayerPersistence : MonoBehaviour
{
    public static PlayerPersistence Instance { get; private set; }

    [SerializeField] private string playerObjectName = "CORE_Siete";

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

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // A scene may contain a mistakenly authored player. Keep the original instance only.
        foreach (PlayerPersistence player in FindObjectsByType<PlayerPersistence>(FindObjectsSortMode.None))
        {
            if (player.gameObject != gameObject)
                Destroy(player.gameObject);
        }

        foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (candidate != transform && candidate.name == playerObjectName)
                Destroy(candidate.gameObject);
        }

        SpawnPoint spawnPoint = SpawnPoint.FindForScene(scene.name);
        if (spawnPoint != null)
        {
            transform.SetPositionAndRotation(spawnPoint.transform.position, spawnPoint.transform.rotation);
        }

        // Cameras belong to their current scene, so acquire them again instead of retaining one.
        foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            camera.SendMessage("SetTarget", transform, SendMessageOptions.DontRequireReceiver);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
