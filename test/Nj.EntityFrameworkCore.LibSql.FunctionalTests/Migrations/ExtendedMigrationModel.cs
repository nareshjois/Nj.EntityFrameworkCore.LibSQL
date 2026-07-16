using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Migrations;

public sealed class Gizmo
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
}

public sealed class Gadget
{
    public int Id { get; set; }
    public int GizmoId { get; set; }
    public string Name { get; set; } = "";
    public int Priority { get; set; }
    public Gizmo Gizmo { get; set; } = null!;
}

public sealed class ExtendedMigrationDbContext : DbContext
{
    public ExtendedMigrationDbContext(DbContextOptions<ExtendedMigrationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Gizmo> Gizmos => Set<Gizmo>();
    public DbSet<Gadget> Gadgets => Set<Gadget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Gizmo>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Gadget>(e =>
        {
            e.HasOne(x => x.Gizmo)
                .WithMany()
                .HasForeignKey(x => x.GizmoId);
            e.HasIndex(x => x.GizmoId);
        });
    }
}

[DbContext(typeof(ExtendedMigrationDbContext))]
[Migration("20260716000001_AddGizmos")]
public class AddGizmos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Gizmos",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("LibSql:Autoincrement", true),
                Code = table.Column<string>(type: "TEXT", nullable: false),
                Label = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Gizmos", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Gizmos_Code",
            table: "Gizmos",
            column: "Code",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable(name: "Gizmos");
}

[DbContext(typeof(ExtendedMigrationDbContext))]
[Migration("20260716000002_AddGadgets")]
public class AddGadgets : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Gadgets",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("LibSql:Autoincrement", true),
                GizmoId = table.Column<int>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Gadgets", x => x.Id);
                table.ForeignKey(
                    name: "FK_Gadgets_Gizmos_GizmoId",
                    column: x => x.GizmoId,
                    principalTable: "Gizmos",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Gadgets_GizmoId",
            table: "Gadgets",
            column: "GizmoId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable(name: "Gadgets");
}

[DbContext(typeof(ExtendedMigrationDbContext))]
[Migration("20260716000003_AddGadgetPriority")]
public class AddGadgetPriority : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Priority",
            table: "Gadgets",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropColumn(
            name: "Priority",
            table: "Gadgets");
}
