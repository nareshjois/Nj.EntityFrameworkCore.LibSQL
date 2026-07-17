using Microsoft.EntityFrameworkCore;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.ConnectionModes;

internal sealed class ModeDbContext(DbContextOptions<ModeDbContext> options) : DbContext(options)
{
    public DbSet<ModeItem> Items => Set<ModeItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<ModeItem>(e =>
        {
            e.ToTable("ModeItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
        });
}

internal sealed class ModeItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
