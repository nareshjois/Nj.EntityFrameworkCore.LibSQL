namespace Nj.LibSql.Bindings;

/// <summary>
/// Public facade over <see cref="LibSqlNativeLibrary"/> for diagnostics and version reporting.
/// </summary>
public static class NativeLibraryLoader
{
    /// <summary>Returns true when a native libsql library has been loaded for this process.</summary>
    public static bool IsLoaded => LibSqlNativeLibrary.IsLoaded;

    /// <summary>
    /// Attempts to load the platform native library.
    /// </summary>
    public static bool TryLoad()
        => LibSqlNativeLibrary.TryInitialize();
}
