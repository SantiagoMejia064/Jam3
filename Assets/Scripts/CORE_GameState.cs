/// <summary>Shared stamp API. SAL_Progress remains the single persistent, in-session flag registry.</summary>
public static class CORE_GameState
{
    public const string SelloTriangulo = SAL_Progress.TriangleSeal;
    public const string SelloRombo = SAL_Progress.DiamondSeal;
    public const string SelloCruz = SAL_Progress.CrossSeal;

    public static bool IsStamp(string id) => id == SelloTriangulo || id == SelloRombo || id == SelloCruz;
    public static bool HasStamp(string id) => IsStamp(id) && SAL_Progress.HasFlag(id);

    /// <returns>True only on the first unlock. Safe to call from any future interaction.</returns>
    public static bool RegisterStamp(string id)
    {
        if (!IsStamp(id) || HasStamp(id)) return false;
        SAL_Progress.SetFlag(id);
        return true;
    }

    public static string Symbol(string id) => id == SelloTriangulo ? "▲" : id == SelloRombo ? "◆" : id == SelloCruz ? "✚" : "?";
}
