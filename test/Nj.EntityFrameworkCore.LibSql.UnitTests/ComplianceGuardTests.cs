using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.UnitTests;

public sealed class ComplianceGuardTests
{
    [Fact]
    public void Repository_has_no_raw_xunit_skip_attributes()
    {
        var root = FindRepoRoot();
        var violations = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}external{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            if (Regex.IsMatch(text, @"\[(Fact|Theory)\s*\(\s*Skip\s*="))
            {
                violations.Add(Path.GetRelativePath(root, file));
            }
        }

        Assert.Empty(violations);
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

        return Directory.GetCurrentDirectory();
    }
}
