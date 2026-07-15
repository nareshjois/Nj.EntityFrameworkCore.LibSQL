using Microsoft.EntityFrameworkCore;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Updates;

public sealed class Account
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public int Balance { get; set; }
}

public sealed class VersionedItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Version { get; set; }
}

public sealed class UpdateDbContext : DbContext
{
    public UpdateDbContext(DbContextOptions<UpdateDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<VersionedItem> VersionedItems => Set<VersionedItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<VersionedItem>(e =>
        {
            e.Property(x => x.Version).IsConcurrencyToken();
        });
    }
}
