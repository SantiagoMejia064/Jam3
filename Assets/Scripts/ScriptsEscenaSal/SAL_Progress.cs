using System;
using System.Collections.Generic;

/// <summary>Shared in-session story flags and progress used by SAL and the other authored zones.</summary>
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
    public const string FinalAvailable = "FinalDisponible";
    public const string FinalComplete = "FinalCompleto";

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

    /// <summary>Call when the final sequence's requirements are fulfilled to enable Javier's airlock lever.</summary>
    public static void UnlockFinal() => SetFlag(FinalAvailable);

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
