using Microsoft.EntityFrameworkCore;
using Rail.Api.Models;

namespace Rail.Api.Data
{
    public class RailDbContext : DbContext
    {
        public RailDbContext(DbContextOptions<RailDbContext> options) : base(options)
        {
        }

        public DbSet<Train> Trains { get; set; }
        public DbSet<Wagon> Wagons { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Train configuration
            modelBuilder.Entity<Train>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.HasIndex(t => t.NormalizedIndex).IsUnique();
                entity.Property(t => t.NormalizedIndex).HasMaxLength(13);
                entity.Property(t => t.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // One-to-many relationship
                entity.HasMany(t => t.Wagons)
                      .WithOne(w => w.Train)
                      .HasForeignKey(w => w.TrainId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Wagon configuration
            modelBuilder.Entity<Wagon>(entity =>
            {
                entity.HasKey(w => w.Id);
                entity.Property(w => w.Number).HasMaxLength(20);
                entity.Property(w => w.WeightKg).HasPrecision(10, 2);

                // Indexes for better query performance
                entity.HasIndex(w => w.TrainId);
                entity.HasIndex(w => w.Date);
                entity.HasIndex(w => w.IsLoaded);
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Fallback configuration - should be configured via DI
                optionsBuilder.UseSqlite("Data Source=rail.db");
            }
        }
    }
}