using System.Runtime.CompilerServices;
using Xunit;

namespace Nj.LibSql.DriverContractTests;

/// <summary>
/// Marks DriverContract facts that are ported but not yet implemented.
/// Switch back to <see cref="FactAttribute"/> in Phase 1 (local) / Phase 2 (remote).
/// See ADR-0002.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class PendingDriverFactAttribute : FactAttribute
{
    public PendingDriverFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        Skip = "Nj.LibSql.Data Phase 0 stub — implement in Phase 1 (local) / Phase 2 (remote). See ADR-0002.";
    }
}
