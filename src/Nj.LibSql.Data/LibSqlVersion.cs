using System.Runtime.InteropServices;
using Nj.LibSql.Bindings;

namespace Nj.LibSql.Data;

/// <summary>Provides version / native load reporting for diagnostics and DriverContract tests.</summary>
public static class LibSqlVersion
{
    private static string? _libSqlVersion;
    private static string? _sqliteVersion;
    private static readonly object Lock = new();

    /// <summary>Gets the version of the libSQL library.</summary>
    public static string LibSqlVersionString
    {
        get
        {
            if (_libSqlVersion != null)
            {
                return _libSqlVersion;
            }

            lock (Lock)
            {
                if (_libSqlVersion != null)
                {
                    return _libSqlVersion;
                }

                LibSqlNative.Initialize();

                try
                {
                    var ptr = LibSqlNative.libsql_libversion();
                    _libSqlVersion = ptr == IntPtr.Zero
                        ? "Unknown"
                        : Marshal.PtrToStringAnsi(ptr) ?? "Unknown";
                }
                catch (EntryPointNotFoundException)
                {
                    _libSqlVersion = $"SQLite {SQLiteVersionString} (libSQL-compatible)";
                }

                return _libSqlVersion;
            }
        }
    }

    /// <summary>Gets the version of the underlying SQLite library.</summary>
    public static string SQLiteVersionString
    {
        get
        {
            if (_sqliteVersion != null)
            {
                return _sqliteVersion;
            }

            lock (Lock)
            {
                if (_sqliteVersion != null)
                {
                    return _sqliteVersion;
                }

                LibSqlNative.Initialize();

                var ptr = LibSqlNative.sqlite3_libversion();
                _sqliteVersion = ptr == IntPtr.Zero ? "Unknown" : Marshal.PtrToStringAnsi(ptr) ?? "Unknown";
                return _sqliteVersion;
            }
        }
    }

    /// <summary>Returns true when a native libsql/SQLite library has been loaded for this process.</summary>
    public static bool IsLibraryLoaded()
    {
        try
        {
            if (!NativeLibraryLoader.IsLoaded && !NativeLibraryLoader.TryLoad())
            {
                return false;
            }

            LibSqlNative.Initialize();
            var ptr = LibSqlNative.sqlite3_libversion();
            return ptr != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Gets detailed version information as a formatted string.</summary>
    public static string GetVersionInfo()
    {
        try
        {
            return IsLibraryLoaded()
                ? $"libSQL {LibSqlVersionString} (SQLite {SQLiteVersionString})"
                : "libsql (native library not loaded)";
        }
        catch (Exception ex)
        {
            return $"Failed to retrieve version information: {ex.Message}";
        }
    }
}
