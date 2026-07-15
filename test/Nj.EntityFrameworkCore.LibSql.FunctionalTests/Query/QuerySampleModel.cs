using Microsoft.EntityFrameworkCore;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;

public sealed class Blog
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public int Rating { get; set; }
    public double Score { get; set; }
    public Guid PublicId { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] Token { get; set; } = [];
    public BlogMeta Meta { get; set; } = new();
    public List<int> Tags { get; set; } = [];
    public List<Post> Posts { get; set; } = [];
}

public sealed class Post
{
    public int Id { get; set; }
    public string Body { get; set; } = "";
    public int BlogId { get; set; }
    public Blog Blog { get; set; } = null!;
}

public sealed class BlogMeta
{
    public string Category { get; set; } = "";
    public int Hits { get; set; }
}

public sealed class QueryDbContext : DbContext
{
    public QueryDbContext(DbContextOptions<QueryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<Post> Posts => Set<Post>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Blog>(e =>
        {
            e.OwnsOne(x => x.Meta, m => m.ToJson());
            e.PrimitiveCollection(x => x.Tags);
        });

        modelBuilder.Entity<Post>(e =>
        {
            e.HasOne(x => x.Blog)
                .WithMany(x => x.Posts)
                .HasForeignKey(x => x.BlogId);
        });
    }
}
