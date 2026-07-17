using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Nj.LibSql.Data;

namespace Nj.EntityFrameworkCore.LibSql.ComplianceTests.Infrastructure;

public class LibSqlTestHelpers : RelationalTestHelpers
{
    public static LibSqlTestHelpers Instance { get; } = new();

    protected LibSqlTestHelpers()
    {
    }

    public override IServiceCollection AddProviderServices(IServiceCollection services)
        => services.AddEntityFrameworkLibSql();

    public override DbContextOptionsBuilder UseProviderOptions(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseLibSql(new LibSqlConnection("Data Source=:memory:"));
}
