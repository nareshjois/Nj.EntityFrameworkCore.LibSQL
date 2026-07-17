namespace Nj.EntityFrameworkCore.LibSql;

/// <summary>
/// Package identity helpers used by pack/install smoke tests.
/// </summary>
public static class LibSqlProviderInfo
{
    /// <summary>NuGet package id / assembly simple name.</summary>
    public const string PackageId = "Nj.EntityFrameworkCore.LibSql";

    /// <summary>Human-readable product label.</summary>
    public const string DisplayName = "Nj Entity Framework Core LibSql Provider";

    /// <summary>
    /// Returns a short status message for pack/install smoke tests.
    /// </summary>
    public static string GetScaffoldStatus()
        => $"{PackageId} {typeof(LibSqlProviderInfo).Assembly.GetName().Version} — UseLibSql provider.";
}
