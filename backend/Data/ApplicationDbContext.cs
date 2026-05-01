using Microsoft.EntityFrameworkCore;

namespace Storage.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<WarehouseArea> WarehouseAreas => Set<WarehouseArea>();
    public DbSet<WarehouseShelf> WarehouseShelves => Set<WarehouseShelf>();
    public DbSet<WarehouseLocation> WarehouseLocations => Set<WarehouseLocation>();
    public DbSet<ComponentType> ComponentTypes => Set<ComponentType>();
    public DbSet<ComponentCategory> ComponentCategories => Set<ComponentCategory>();
    public DbSet<Component> Components => Set<Component>();
    public DbSet<StockLocation> StockLocations => Set<StockLocation>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WarehouseArea>()
            .HasIndex(x => new { x.Name, x.Code, x.ZoneType, x.FloorLevel })
            .IsUnique();

        modelBuilder.Entity<WarehouseShelf>()
            .HasIndex(x => new { x.AreaId, x.Name, x.Code })
            .IsUnique();

        modelBuilder.Entity<WarehouseLocation>()
            .HasIndex(x => new { x.ShelfId, x.Name, x.Code })
            .IsUnique();

        modelBuilder.Entity<Supplier>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(x => x.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<WarehouseShelf>()
            .HasOne(x => x.Area)
            .WithMany()
            .HasForeignKey(x => x.AreaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WarehouseLocation>()
            .HasOne(x => x.Shelf)
            .WithMany()
            .HasForeignKey(x => x.ShelfId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Component>()
            .HasOne(x => x.ComponentType)
            .WithMany()
            .HasForeignKey(x => x.ComponentTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Component>()
            .HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<StockLocation>()
            .HasOne(x => x.Component)
            .WithMany()
            .HasForeignKey(x => x.ComponentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockLocation>()
            .HasOne(x => x.WarehouseLocation)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AuditLog>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WarehouseArea>()
            .Property(x => x.ZoneType)
            .HasConversion<string>();

        modelBuilder.Entity<ComponentType>()
            .Property(x => x.Type)
            .HasConversion<string>();

        modelBuilder.Entity<User>()
            .Property(x => x.Role)
            .HasConversion<string>();
    }
}