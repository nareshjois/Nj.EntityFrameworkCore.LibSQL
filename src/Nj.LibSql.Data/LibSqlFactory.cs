using System.Data.Common;

namespace Nj.LibSql.Data;

/// <summary>DbProviderFactory for Nj.LibSql.Data.</summary>
public sealed class LibSqlFactory : DbProviderFactory
{
    public const string ProviderInvariantName = "Nj.LibSql.Data";

    public static readonly LibSqlFactory Instance = new();

    private LibSqlFactory()
    {
    }

    public override bool CanCreateCommandBuilder => false;

    public override bool CanCreateDataAdapter => false;

    public override bool CanCreateDataSourceEnumerator => false;

    public override DbCommand CreateCommand()
        => new LibSqlCommand();

    public override DbCommandBuilder CreateCommandBuilder()
        => throw new NotSupportedException("CommandBuilder is not implemented for libSQL.");

    public override DbConnection CreateConnection()
        => new LibSqlConnection();

    public override DbConnectionStringBuilder CreateConnectionStringBuilder()
        => new LibSqlConnectionStringBuilder();

    public override DbParameter CreateParameter()
        => new LibSqlParameter();

    public static void RegisterFactory()
        => DbProviderFactories.RegisterFactory(ProviderInvariantName, Instance);

    public static void UnregisterFactory()
        => DbProviderFactories.UnregisterFactory(ProviderInvariantName);
}
