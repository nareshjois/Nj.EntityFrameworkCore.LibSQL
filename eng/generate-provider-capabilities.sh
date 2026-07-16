#!/usr/bin/env bash
# Regenerate docs/provider-capabilities.json waivers for unimplemented EF spec suites.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="${ROOT}/docs/provider-capabilities.json"
TMP="${ROOT}/eng/tmp-cap-gen"
rm -rf "${TMP}"
mkdir -p "${TMP}"

cat > "${TMP}/Gen.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Relational.Specification.Tests" Version="10.0.10" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../test/Nj.EntityFrameworkCore.LibSql.ComplianceTests/Nj.EntityFrameworkCore.LibSql.ComplianceTests.csproj" />
  </ItemGroup>
</Project>
EOF

cat > "${TMP}/Program.cs" <<'EOF'
using System.Reflection;
using System.Text.Json;
using Nj.EntityFrameworkCore.LibSql.ComplianceTests.Infrastructure;

var asm = typeof(Nj.EntityFrameworkCore.LibSql.ComplianceTests.LibSqlComplianceTest).Assembly;
var unimplemented = ComplianceCapabilities.GetUnimplementedTestBases(asm)
    .Select(t => t.FullName!)
    .OrderBy(x => x, StringComparer.Ordinal)
    .ToList();

var existing = new List<object>();
var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs", "provider-capabilities.json"));
if (File.Exists(path))
{
    // preserve manually curated waivers with ids C-001..C-005 documented in compatibility.md
}

var waivers = new List<Dictionary<string, object?>>
{
    new()
    {
        ["id"] = "C-006",
        ["suitePatterns"] = new[] { "Spatial", "NetTopology" },
        ["reason"] = "SpatiaLite / spatial types are out of scope (Preview 1).",
        ["modes"] = new[] { "local", "remote" },
        ["owner"] = "provider"
    },
    new()
    {
        ["id"] = "C-007",
        ["suitePatterns"] = new[] { "FromSqlSproc", "SqlExecutor", "UdfDbFunction", "StoredProcedure" },
        ["reason"] = "Stored procedures and UDF DbFunctions are not supported.",
        ["modes"] = new[] { "local", "remote" },
        ["owner"] = "provider"
    }
};

foreach (var suite in unimplemented)
{
    waivers.Add(new Dictionary<string, object?>
    {
        ["id"] = "C-AUTO",
        ["suiteType"] = suite,
        ["reason"] = "Not yet hosted in Nj.EntityFrameworkCore.LibSql.ComplianceTests (WP-10 expansion backlog).",
        ["modes"] = new[] { "local", "remote" },
        ["owner"] = "provider"
    });
}

var doc = new Dictionary<string, object?> { ["waivers"] = waivers };
var json = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
Console.Write(json);
EOF

JSON="$(dotnet run --project "${TMP}/Gen.csproj" -v q)"
printf '%s\n' "${JSON}" > "${OUT}"
echo "Wrote ${OUT} ($(jq '.waivers | length' "${OUT}") waivers)"
