using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Storage.Data
{
    public class StorageDbContext : DbContext
    {
        public DbSet<Area> Areas { get; set; }
        public DbSet<Shelf> Shelves { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<UserAccount> Users { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ComponentParameter> ComponentParameters { get; set; }

        public StorageDbContext(DbContextOptions<StorageDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Area>().HasKey(a => a.Id);
            modelBuilder.Entity<Shelf>().HasKey(s => s.Id);
            modelBuilder.Entity<Location>().HasKey(l => l.Id);
            modelBuilder.Entity<Item>().HasKey(i => i.Id);
            modelBuilder.Entity<UserAccount>().HasKey(u => u.Id);
            modelBuilder.Entity<AuditLog>().HasKey(a => a.Id);
            modelBuilder.Entity<ComponentParameter>().HasKey(c => c.Id);

            // Area - Shelf relationship
            modelBuilder.Entity<Shelf>()
                .HasOne(s => s.Area)
                .WithMany(a => a.Shelves)
                .HasForeignKey(s => s.AreaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Shelf - Location relationship
            modelBuilder.Entity<Location>()
                .HasOne(l => l.Shelf)
                .WithMany(s => s.Locations)
                .HasForeignKey(l => l.ShelfId)
                .OnDelete(DeleteBehavior.Restrict);

            // Location - Item relationship
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Location)
                .WithMany(l => l.Items)
                .HasForeignKey(i => i.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}