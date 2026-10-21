using System;
using System.Collections.Generic;

/// <summary>Runtime progress for SAL. Other zones can use SetFlag/SetAct until CORE_GameState exists.</summary>
public static class SAL_Progress
{
    public const string DrawingsFound = "SAL_DibujosEncontrados";
    public const string DiamondSeal = "SelloRombo";
    public const string TriangleSeal = "SelloTriangulo";
    public const string CrossSeal = "SelloCruz";
    public const string GrilleOpen = "SAL_RejillaAbierta";
    public const string LabelRead = "SAL_EtiquetaFiltroLeida";
    public const string WarningInspected = "SAL_AdvertenciaFiltroInspeccionada";
    public const string FilterRemoved = "FiltroRetirado";
    public const string PanelSolved = "SAL_PanelResuelto";
    public const string SeedObtained = "SemillaObtenida";
    public const string Revealed = "Revelado";

    private static readonly HashSet<string> Flags = new HashSet<string>();
    public static event Action<string, bool> FlagChanged;
    public static event Action<int> ActChanged;
    public static int Act { get; private set; } = 3;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticsOnPlay()
    {
        Flags.Clear();
        Act = 3;
        FlagChanged = null;
        ActChanged = null;
    }

    public static bool HasFlag(string id) => !string.IsNullOrEmpty(id) && Flags.Contains(id);

    public static void SetFlag(string id, bool value = true)
    {
        if (string.IsNullOrEmpty(id)) return;
        bool changed = value ? Flags.Add(id) : Flags.Remove(id);
        if (changed) FlagChanged?.Invoke(id, value);
    }

    public static void SetAct(int act)
    {
        act = Math.Max(1, Math.Min(9, act));
        if (act == Act) return;
        Act = act;
        ActChanged?.Invoke(act);
    }

    public static void ResetForNewGame()
    {
        Flags.Clear();
        Act = 3;
        ActChanged?.Invoke(Act);
    }
}
