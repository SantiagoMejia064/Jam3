using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Scene-authored destination for the persistent player. Leave Scene Name empty to use its own scene.</summary>
public sealed class SpawnPoint : MonoBehaviour
{
    [SerializeField] private string sceneName;

    public bool Matches(string loadedSceneName)
    {
        return string.IsNullOrWhiteSpace(sceneName)
            ? gameObject.scene.name == loadedSceneName
            : sceneName == loadedSceneName;
    }

    public static SpawnPoint FindForScene(string loadedSceneName)
    {
        foreach (SpawnPoint point in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
        {
            if (point.gameObject.scene == SceneManager.GetActiveScene() && point.Matches(loadedSceneName))
                return point;
        }

        return null;
    }
}
