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

        modelBuilder.Entity<ComponentType>()
            .HasIndex(x => x.Name)
            .IsUnique();

        modelBuilder.Entity<ComponentType>()
            .HasIndex(x => new { x.CategoryId, x.Name })
            .IsUnique();

        modelBuilder.Entity<StockLocation>()
            .HasIndex(x => new { x.ComponentId, x.LocationId, x.BatchCode });

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(x => x.Token)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(x => new { x.UserId, x.IsRevoked });

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

        modelBuilder.Entity<ComponentType>()
            .HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

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

        // Global soft-delete filters keep inactive records out of normal queries.
        modelBuilder.Entity<WarehouseArea>().HasQueryFilter(x => x.IsActive);
        modelBuilder.Entity<WarehouseShelf>().HasQueryFilter(x => x.IsActive);
        modelBuilder.Entity<WarehouseLocation>().HasQueryFilter(x => x.IsActive);
        modelBuilder.Entity<ComponentCategory>().HasQueryFilter(x => x.IsActive);
        modelBuilder.Entity<ComponentType>().HasQueryFilter(x => x.IsActive);
        modelBuilder.Entity<Component>().HasQueryFilter(x => x.IsActive);
        modelBuilder.Entity<Supplier>().HasQueryFilter(x => x.IsActive);
        modelBuilder.Entity<StockLocation>().HasQueryFilter(x => x.IsActive);
        modelBuilder.Entity<User>().HasQueryFilter(x => x.IsActive);
    }
}