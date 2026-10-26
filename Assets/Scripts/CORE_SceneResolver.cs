using UnityEngine;

/// <summary>
/// Resuelve de forma robusta los nombres de escena activos en el proyecto,
/// gestionando automáticamente las transiciones y renombrados históricos
/// (ej. 'Javier'/'Inicio' -> 'Inicio-Laboratorio', 'SAL' -> 'SalaMaquinas').
/// </summary>
public static class CORE_SceneResolver
{
    public static string ResolveSceneName(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return string.Empty;

        // 1. Si la escena solicitada ya se encuentra registrada y cargable tal cual
        if (Application.CanStreamedLevelBeLoaded(target))
            return target;

        // 2. Extraer nombre base si viene como ruta de archivo (ej. "Assets/Scenes/Javier.unity" -> "Javier")
        string baseName = target;
        if (baseName.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
        {
            int slash = baseName.LastIndexOf('/');
            if (slash < 0) slash = baseName.LastIndexOf('\\');
            if (slash >= 0) baseName = baseName.Substring(slash + 1);
            baseName = baseName.Substring(0, baseName.Length - 6);
        }

        // Si el nombre base simple se puede cargar directamente
        if (Application.CanStreamedLevelBeLoaded(baseName))
            return baseName;

        // 3. Aliases para la escena principal de Inicio / Laboratorio / Pasillo
        if (baseName.Equals("Javier", System.StringComparison.OrdinalIgnoreCase) ||
            baseName.Equals("Inicio", System.StringComparison.OrdinalIgnoreCase))
        {
            if (Application.CanStreamedLevelBeLoaded("Inicio-Laboratorio"))
                return "Inicio-Laboratorio";
            if (Application.CanStreamedLevelBeLoaded("Assets/Scenes/Inicio-Laboratorio.unity"))
                return "Assets/Scenes/Inicio-Laboratorio.unity";
        }
        else if (baseName.Equals("Inicio-Laboratorio", System.StringComparison.OrdinalIgnoreCase))
        {
            if (Application.CanStreamedLevelBeLoaded("Inicio"))
                return "Inicio";
            if (Application.CanStreamedLevelBeLoaded("Javier"))
                return "Javier";
        }

        // 4. Aliases para la Sala de Máquinas (antiguamente SAL)
        if (baseName.Equals("SAL", System.StringComparison.OrdinalIgnoreCase))
        {
            if (Application.CanStreamedLevelBeLoaded("SalaMaquinas"))
                return "SalaMaquinas";
            if (Application.CanStreamedLevelBeLoaded("Assets/Scenes/SalaMaquinas.unity"))
                return "Assets/Scenes/SalaMaquinas.unity";
        }
        else if (baseName.Equals("SalaMaquinas", System.StringComparison.OrdinalIgnoreCase))
        {
            if (Application.CanStreamedLevelBeLoaded("SAL"))
                return "SAL";
        }

        return target;
    }
}
