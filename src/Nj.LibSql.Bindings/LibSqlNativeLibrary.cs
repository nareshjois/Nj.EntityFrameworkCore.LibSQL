using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Nj.LibSql.Bindings;

/// <summary>
/// Handles platform-specific native library loading for libsql.
/// </summary>
internal static class LibSqlNativeLibrary
{
    /// <summary>
    /// The name of the libsql native library.
    /// </summary>
    internal const string LibraryName = "libsql";

    private static bool _isInitialized;
    private static bool _resolverRegistered;
    private static IntPtr _libraryHandle = IntPtr.Zero;
    private static readonly object Lock = new();

    /// <summary>Returns true when a native libsql library has been loaded for this process.</summary>
    public static bool IsLoaded => _isInitialized;

    /// <summary>
    /// Ensures the native library is loaded and available.
    /// </summary>
    /// <returns>True if the library was successfully loaded or was already loaded.</returns>
    public static bool TryInitialize()
    {
        if (_isInitialized)
        {
            return true;
        }

        lock (Lock)
        {
            if (_isInitialized)
            {
                return true;
            }

            try
            {
                EnsureResolverRegistered();

                var rid = GetRuntimeIdentifier();
                if (rid == null)
                {
                    return false;
                }

                foreach (var path in EnumerateSearchPaths(rid))
                {
                    if (TryLoadFromDirectory(path))
                    {
                        _isInitialized = true;
                        return true;
                    }
                }

                if (TryLoadSystemWide())
                {
                    _isInitialized = true;
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Enumerates the directories searched for the native library, in order of preference.
    /// </summary>
    /// <param name="rid">The runtime identifier for the current platform (e.g. <c>win-x64</c>).</param>
    /// <remarks>
    /// <para>
    /// <see cref="AppContext.BaseDirectory"/> is checked first because it is the API that
    /// works reliably in single-file publishes. In those scenarios <see cref="Assembly.Location"/>
    /// returns an empty string for bundled assemblies, so paths derived from it collapse
    /// and must be treated as non-load-bearing.
    /// </para>
    /// <para>Null and empty entries are never yielded.</para>
    /// </remarks>
    [UnconditionalSuppressMessage(
        "SingleFile",
        "IL3000:Avoid accessing Assembly file path when publishing as a single file",
        Justification = "AppContext.BaseDirectory is probed first; Assembly.Location is only used when it is non-empty for non-bundled layouts.")]
    internal static IEnumerable<string> EnumerateSearchPaths(string rid)
    {
        ArgumentNullException.ThrowIfNull(rid);

        var baseDirectory = AppContext.BaseDirectory;
        if (!string.IsNullOrEmpty(baseDirectory))
        {
            yield return Path.Combine(baseDirectory, "runtimes", rid, "native");
            yield return Path.Combine(baseDirectory, rid);
            yield return baseDirectory;
        }

#pragma warning disable IL3000
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#pragma warning restore IL3000
        if (!string.IsNullOrEmpty(assemblyDirectory))
        {
            yield return Path.Combine(assemblyDirectory, "runtimes", rid, "native");
            yield return Path.Combine(assemblyDirectory, rid);
            yield return assemblyDirectory;

            var parent = Path.GetDirectoryName(assemblyDirectory);
            if (!string.IsNullOrEmpty(parent))
            {
                yield return parent;
            }
        }
    }

    /// <summary>
    /// Gets the runtime identifier for the current platform.
    /// </summary>
    private static string? GetRuntimeIdentifier()
    {
        if (OperatingSystem.IsWindows())
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "win-x64",
                Architecture.X86 => "win-x86",
                Architecture.Arm64 => "win-arm64",
                _ => null
            };
        }

        if (OperatingSystem.IsLinux())
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "linux-x64",
                Architecture.Arm64 => "linux-arm64",
                Architecture.Arm => "linux-arm",
                _ => null
            };
        }

        if (OperatingSystem.IsMacOS())
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "osx-x64",
                Architecture.Arm64 => "osx-arm64",
                _ => "osx"
            };
        }

        return null;
    }

    private static bool TryLoadFromDirectory(string directory)
    {
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return false;
        }

        var libraryNames = GetPlatformSpecificLibraryNames();

        foreach (var libraryName in libraryNames)
        {
            var libraryPath = Path.Combine(directory, libraryName);
            if (File.Exists(libraryPath))
            {
                try
                {
                    if (NativeLibrary.TryLoad(libraryPath, out var handle))
                    {
                        _libraryHandle = handle;
                        return true;
                    }
                }
                catch
                {
                    // Continue trying other names.
                }
            }
        }

        foreach (var libraryName in libraryNames)
        {
            try
            {
                if (NativeLibrary.TryLoad(
                        libraryName,
                        Assembly.GetExecutingAssembly(),
                        DllImportSearchPath.SafeDirectories,
                        out var handle))
                {
                    _libraryHandle = handle;
                    return true;
                }
            }
            catch
            {
                // Continue trying other names.
            }
        }

        return false;
    }

    private static string[] GetPlatformSpecificLibraryNames()
    {
        if (OperatingSystem.IsWindows())
        {
            return ["libsql.dll", "sqlite3.dll"];
        }

        if (OperatingSystem.IsMacOS())
        {
            return ["libsql.dylib", "libsqlite3.dylib", "sqlite3.dylib"];
        }

        return ["libsql.so", "libsqlite3.so", "sqlite3.so"];
    }

    private static bool TryLoadSystemWide()
    {
        var libraryNames = GetPlatformSpecificLibraryNames();

        foreach (var libraryName in libraryNames)
        {
            try
            {
                if (NativeLibrary.TryLoad(libraryName, out var handle))
                {
                    _libraryHandle = handle;
                    return true;
                }
            }
            catch
            {
                // Continue trying other names.
            }
        }

        return false;
    }

    /// <summary>
    /// Registers a <see cref="NativeLibrary.SetDllImportResolver"/> delegate so that
    /// P/Invoke lookups for <c>libsql</c> in this assembly resolve to the handle we
    /// loaded explicitly.
    /// </summary>
    private static void EnsureResolverRegistered()
    {
        if (_resolverRegistered)
        {
            return;
        }

        NativeLibrary.SetDllImportResolver(typeof(LibSqlNativeLibrary).Assembly, ResolveLibrary);
        _resolverRegistered = true;
    }

    private static IntPtr ResolveLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (string.Equals(libraryName, LibraryName, StringComparison.Ordinal) && _libraryHandle != IntPtr.Zero)
        {
            return _libraryHandle;
        }

        return IntPtr.Zero;
    }
}
