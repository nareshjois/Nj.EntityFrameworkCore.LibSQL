using System.Runtime.CompilerServices;
using Xunit;

namespace Nj.LibSql.DriverContractTests;

/// <summary>
/// Marks DriverContract facts that are intentionally skipped until implemented.
/// Prefer <see cref="FactAttribute"/> once the behavior exists.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class PendingDriverFactAttribute : FactAttribute
{
    public PendingDriverFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        Skip = "Nj.LibSql.Data: not implemented yet (see docs/compatibility.md / docs/testing.md).";
    }
}
