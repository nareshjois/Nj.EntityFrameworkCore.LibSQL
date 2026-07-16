using Microsoft.EntityFrameworkCore;

namespace MigrationsSample;

public class Blog
{
    public int Id { get; set; }
    public string Url { get; set; } = "";
    public List<Post> Posts { get; set; } = [];
}

public class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public int BlogId { get; set; }
    public Blog Blog { get; set; } = null!;
}

public class SampleDbContext : DbContext
{
    public SampleDbContext(DbContextOptions<SampleDbContext> options)
        : base(options)
    {
    }

    public DbSet<Blog> Blogs => Set<Blog>();

    public DbSet<Post> Posts => Set<Post>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Blog>(b =>
        {
            b.Property(e => e.Url).HasMaxLength(2048).IsRequired();
            b.HasMany(e => e.Posts).WithOne(e => e.Blog).HasForeignKey(e => e.BlogId);
        });

        modelBuilder.Entity<Post>(p =>
        {
            p.Property(e => e.Title).HasMaxLength(512).IsRequired();
        });
    }
}
