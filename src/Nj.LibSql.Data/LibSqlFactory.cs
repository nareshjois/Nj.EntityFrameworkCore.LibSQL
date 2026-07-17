using System.Data.Common;

namespace Nj.LibSql.Data;

/// <summary>DbProviderFactory for Nj.LibSql.Data.</summary>
public sealed class LibSqlFactory : DbProviderFactory
{
    /// <summary>string.</summary>
    public const string ProviderInvariantName = "Nj.LibSql.Data";

    /// <summary>LibSqlFactory.</summary>
    public static readonly LibSqlFactory Instance = new();

    private LibSqlFactory()
    {
    }

    /// <inheritdoc />
    public override bool CanCreateCommandBuilder => false;

    /// <inheritdoc />
    public override bool CanCreateDataAdapter => false;

    /// <inheritdoc />
    public override bool CanCreateDataSourceEnumerator => false;

    /// <inheritdoc />
    public override DbCommand CreateCommand()
        => new LibSqlCommand();

    /// <inheritdoc />
    public override DbCommandBuilder CreateCommandBuilder()
        => throw new NotSupportedException("CommandBuilder is not implemented for libSQL.");

    /// <inheritdoc />
    public override DbConnection CreateConnection()
        => new LibSqlConnection();

    /// <inheritdoc />
    public override DbConnectionStringBuilder CreateConnectionStringBuilder()
        => new LibSqlConnectionStringBuilder();

    /// <inheritdoc />
    public override DbParameter CreateParameter()
        => new LibSqlParameter();

    /// <summary>RegisterFactory().</summary>
    public static void RegisterFactory()
        => DbProviderFactories.RegisterFactory(ProviderInvariantName, Instance);

    /// <summary>UnregisterFactory().</summary>
    public static void UnregisterFactory()
        => DbProviderFactories.UnregisterFactory(ProviderInvariantName);
}
