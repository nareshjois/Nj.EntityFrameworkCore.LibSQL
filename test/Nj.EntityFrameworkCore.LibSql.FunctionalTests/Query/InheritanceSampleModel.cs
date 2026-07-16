using Microsoft.EntityFrameworkCore;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;

public abstract class Animal
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class Cat : Animal
{
    public int Lives { get; set; }
}

public sealed class Dog : Animal
{
    public string Breed { get; set; } = "";
}

public sealed class InheritanceQueryDbContext : DbContext
{
    public InheritanceQueryDbContext(DbContextOptions<InheritanceQueryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Animal> Animals => Set<Animal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Animal>(e =>
        {
            e.UseTphMappingStrategy();
            e.HasDiscriminator<string>("Kind")
                .HasValue<Cat>("cat")
                .HasValue<Dog>("dog");
        });
    }
}
