using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Scene-authored destination for the persistent player. Leave Scene Name empty to use its own scene.</summary>
public sealed class SpawnPoint : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [SerializeField] private string entryFromScene;

    public bool Matches(string loadedSceneName)
    {
        return string.IsNullOrWhiteSpace(sceneName)
            ? gameObject.scene.name == loadedSceneName
            : sceneName == loadedSceneName;
    }

    public static SpawnPoint FindForScene(string loadedSceneName, string fromScene = "")
    {
        SpawnPoint fallback = null;
        foreach (SpawnPoint point in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
        {
            if (point.gameObject.scene == SceneManager.GetActiveScene() && point.Matches(loadedSceneName))
            {
                if (!string.IsNullOrEmpty(fromScene) && point.entryFromScene == fromScene) return point;
                if (string.IsNullOrEmpty(point.entryFromScene)) fallback = point;
            }
        }
        return fallback;
    }
}
