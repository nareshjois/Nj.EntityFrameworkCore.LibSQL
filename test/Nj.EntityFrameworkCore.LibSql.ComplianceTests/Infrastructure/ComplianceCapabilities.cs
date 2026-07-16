using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nj.EntityFrameworkCore.LibSql.ComplianceTests.Infrastructure;

public static class ComplianceCapabilities
{
    public static string ManifestPath
        => Path.GetFullPath(Path.Combine(FindRepoRoot(), "docs", "provider-capabilities.json"));

    public static IReadOnlyCollection<Type> GetIgnoredTestBases(Assembly complianceAssembly)
        => GetUnimplementedTestBases(complianceAssembly);

    public static IReadOnlyList<CapabilityWaiver> LoadWaivers()
    {
        if (!File.Exists(ManifestPath))
        {
            return [];
        }

        var json = File.ReadAllText(ManifestPath);
        var dto = JsonSerializer.Deserialize<CapabilityManifestDto>(json, JsonSerializerOptions)
            ?? new CapabilityManifestDto();
        return dto.Waivers ?? [];
    }

    public static IReadOnlyCollection<Type> GetUnimplementedTestBases(Assembly complianceAssembly)
    {
        var implemented = DiscoverImplementedTestBases(complianceAssembly);
        return GetAllSpecificationTestBases()
            .Where(baseType => !IsComplianceMetaBase(baseType))
            .Where(baseType => !implemented.Any(t => Implements(t, baseType)))
            .ToList();
    }

    public static void ValidateManifestCoversIgnoredSuites(Assembly complianceAssembly)
    {
        var unimplemented = GetUnimplementedTestBases(complianceAssembly);
        var waivers = LoadWaivers();
        var covered = new HashSet<string>(StringComparer.Ordinal);

        foreach (var waiver in waivers)
        {
            if (!string.IsNullOrWhiteSpace(waiver.SuiteType))
            {
                covered.Add(waiver.SuiteType);
            }

            foreach (var pattern in waiver.SuitePatterns ?? [])
            {
                foreach (var baseType in unimplemented)
                {
                    if (MatchesPattern(baseType, pattern))
                    {
                        covered.Add(baseType.FullName!);
                    }
                }
            }
        }

        var missing = unimplemented
            .Select(t => t.FullName!)
            .Where(name => !covered.Contains(name)
                && !waivers.Any(w => MatchesPatternName(name, w.SuiteType)))
            .ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Unimplemented specification suites missing provider-capabilities.json waivers:\n"
                + string.Join("\n", missing));
        }
    }

    internal static HashSet<Type> DiscoverImplementedTestBases(Assembly complianceAssembly)
        => complianceAssembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false } && (t.IsPublic || t.IsNestedPublic))
            .Where(t => t.BaseType != typeof(object))
            .SelectMany(GetImplementedBaseTypes)
            .ToHashSet();

    private static IEnumerable<Type> GetImplementedBaseTypes(Type type)
    {
        var current = type.BaseType;
        while (current is not null && current != typeof(object))
        {
            if (current.Name.Contains("TestBase", StringComparison.Ordinal))
            {
                yield return current.IsGenericType ? current.GetGenericTypeDefinition() : current;
            }

            current = current.BaseType;
        }
    }

    private static IEnumerable<Type> GetAllSpecificationTestBases()
    {
        var spec = typeof(Microsoft.EntityFrameworkCore.ComplianceTestBase).Assembly
            .ExportedTypes
            .Where(t => t.Name.Contains("TestBase", StringComparison.Ordinal));
        var rel = typeof(Microsoft.EntityFrameworkCore.RelationalComplianceTestBase).Assembly
            .ExportedTypes
            .Where(t => t.Name.Contains("TestBase", StringComparison.Ordinal));
        return spec.Concat(rel).Distinct();
    }

    private static bool IsComplianceMetaBase(Type baseType)
        => baseType == typeof(Microsoft.EntityFrameworkCore.ComplianceTestBase)
            || baseType == typeof(Microsoft.EntityFrameworkCore.RelationalComplianceTestBase)
            || baseType.Name is "NonSharedModelTestBase";

    private static bool Implements(Type type, Type interfaceOrBaseType)
        => interfaceOrBaseType.IsGenericTypeDefinition
            ? type.BaseType is not null
                && type.BaseType.IsGenericType
                && type.BaseType.GetGenericTypeDefinition() == interfaceOrBaseType
            : interfaceOrBaseType.IsAssignableFrom(type);

    private static bool MatchesPattern(Type baseType, string pattern)
        => MatchesPatternName(baseType.Name, pattern)
            || MatchesPatternName(baseType.FullName ?? baseType.Name, pattern);

    private static bool MatchesPatternName(string name, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        if (pattern.EndsWith('*'))
        {
            return name.StartsWith(pattern[..^1], StringComparison.Ordinal);
        }

        return string.Equals(name, pattern, StringComparison.Ordinal)
            || name.Contains(pattern, StringComparison.Ordinal);
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

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private sealed class CapabilityManifestDto
    {
        public List<CapabilityWaiver>? Waivers { get; set; }
    }
}

public sealed class CapabilityWaiver
{
    public string Id { get; set; } = "";
    public string SuiteType { get; set; } = "";
    public List<string>? SuitePatterns { get; set; }
    public string Reason { get; set; } = "";
    public List<string> Modes { get; set; } = [];
    public string? Issue { get; set; }
    public string? Owner { get; set; }
}
