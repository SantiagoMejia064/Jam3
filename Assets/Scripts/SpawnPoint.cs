using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Scene-authored destination for the persistent player. Leave Scene Name empty to use its own scene.</summary>
public sealed class SpawnPoint : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [SerializeField] private string entryFromScene;

    public bool Matches(string loadedSceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return gameObject.scene.name == loadedSceneName;

        string resAuthored = CORE_SceneResolver.ResolveSceneName(sceneName);
        string resLoaded = CORE_SceneResolver.ResolveSceneName(loadedSceneName);
        return string.Equals(resAuthored, resLoaded, System.StringComparison.OrdinalIgnoreCase);
    }

    public static SpawnPoint FindForScene(string loadedSceneName, string fromScene = "")
    {
        SpawnPoint fallback = null;
        string resFrom = !string.IsNullOrEmpty(fromScene) ? CORE_SceneResolver.ResolveSceneName(fromScene) : string.Empty;

        foreach (SpawnPoint point in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
        {
            if (point.gameObject.scene == SceneManager.GetActiveScene() && point.Matches(loadedSceneName))
            {
                if (!string.IsNullOrEmpty(fromScene))
                {
                    string resEntry = !string.IsNullOrEmpty(point.entryFromScene)
                        ? CORE_SceneResolver.ResolveSceneName(point.entryFromScene)
                        : string.Empty;

                    if (string.Equals(point.entryFromScene, fromScene, System.StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrEmpty(resEntry) && string.Equals(resEntry, resFrom, System.StringComparison.OrdinalIgnoreCase)))
                    {
                        return point;
                    }
                }
                if (string.IsNullOrEmpty(point.entryFromScene)) fallback = point;
            }
        }
        return fallback;
    }
}
