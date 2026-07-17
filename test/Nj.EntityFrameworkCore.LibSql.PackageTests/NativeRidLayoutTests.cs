using System.IO.Compression;
using System.Runtime.InteropServices;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.PackageTests;

/// <summary>
/// WP-12: advertised RID native layout (committed runtimes + packed Bindings nupkg when present).
/// </summary>
public sealed class NativeRidLayoutTests
{
    private static readonly (string Rid, string FileName)[] AdvertisedRids =
    [
        ("linux-x64", "libsql.so"),
        ("osx-arm64", "libsql.dylib"),
        ("win-x64", "libsql.dll"),
    ];

    [Fact]
    public void Committed_runtimes_contain_all_advertised_rid_natives()
    {
        var bindingsRoot = FindBindingsProjectRoot();
        foreach (var (rid, fileName) in AdvertisedRids)
        {
            var path = Path.Combine(bindingsRoot, "runtimes", rid, "native", fileName);
            Assert.True(File.Exists(path), $"Missing committed native for {rid}: {path}");
            Assert.True(new FileInfo(path).Length > 0, $"Empty native for {rid}: {path}");
        }

        var versionPin = Path.Combine(bindingsRoot, "runtimes", "LIBSQL_VERSION");
        Assert.True(File.Exists(versionPin), "Missing LIBSQL_VERSION pin file.");
    }

    [Fact]
    public void Runner_rid_native_is_loadable_via_memory_open()
    {
        // Smoke that the RID asset for this process actually works (CI: linux-x64 / win-x64).
        using var connection = new Nj.LibSql.Data.LibSqlConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        Assert.Equal(1L, Convert.ToInt64(command.ExecuteScalar()));
    }

    [Fact]
    public void Packed_bindings_nupkg_contains_advertised_rids_when_artifacts_present()
    {
        var packagesDir = Path.GetFullPath(
            Path.Combine(FindRepoRoot(), "artifacts", "packages"));
        if (!Directory.Exists(packagesDir))
        {
            Assert.Skip("artifacts/packages not present — run pack / verify-package first.");
        }

        var nupkg = Directory.GetFiles(packagesDir, "Nj.LibSql.Bindings.*.nupkg")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
        if (nupkg is null)
        {
            Assert.Skip("Nj.LibSql.Bindings nupkg not found under artifacts/packages.");
        }

        using var zip = ZipFile.OpenRead(nupkg);
        var entries = zip.Entries.Select(e => e.FullName.Replace('\\', '/')).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (rid, fileName) in AdvertisedRids)
        {
            var expected = $"runtimes/{rid}/native/{fileName}";
            Assert.True(
                entries.Contains(expected),
                $"Packed Bindings nupkg missing {expected} (nupkg={Path.GetFileName(nupkg)}).");
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Nj.EntityFrameworkCore.LibSql.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root from " + AppContext.BaseDirectory);
    }

    private static string FindBindingsProjectRoot()
        => Path.Combine(FindRepoRoot(), "src", "Nj.LibSql.Bindings");
}
