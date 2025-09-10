using System;
using Microsoft.EntityFrameworkCore;
using Rail.Data.Models;

namespace Rail.Data.Data
{
    public class RailDbContext : DbContext
    {
        public RailDbContext(DbContextOptions<RailDbContext> options) : base(options)
        {
        }

        public DbSet<Train> Trains { get; set; }
        public DbSet<Wagon> Wagons { get; set; }
        public DbSet<ProcessedEvent> ProcessedEvents { get; set; }
        public DbSet<User> Users { get; set; }

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

            // ProcessedEvent entity configuration
            modelBuilder.Entity<ProcessedEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.EventId).IsUnique();
                entity.HasIndex(e => e.ProcessedAt);
                entity.HasIndex(e => new { e.Source, e.ProcessedAt });

                entity.Property(e => e.EventId).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Source).HasMaxLength(50);
                entity.Property(e => e.WagonNumber).HasMaxLength(50);
                entity.Property(e => e.ProcessedAt).IsRequired();

                entity.HasOne(e => e.Train)
                    .WithMany()
                    .HasForeignKey(e => e.TrainId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // User entity configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();

                entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
                entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Roles).HasMaxLength(100).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            });

            // Seed default users
            SeedDefaultUsers(modelBuilder);
        }

        private void SeedDefaultUsers(ModelBuilder modelBuilder)
        {
            // Note: In production, these should be created through a proper user management system
            // and passwords should be much stronger

            var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
            var readerPasswordHash = BCrypt.Net.BCrypt.HashPassword("reader123");
            var uploaderPasswordHash = BCrypt.Net.BCrypt.HashPassword("uploader123");

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    PasswordHash = adminPasswordHash,
                    Roles = "admin,reader,uploader",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new User
                {
                    Id = 2,
                    Username = "reader",
                    PasswordHash = readerPasswordHash,
                    Roles = "reader",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new User
                {
                    Id = 3,
                    Username = "uploader",
                    PasswordHash = uploaderPasswordHash,
                    Roles = "uploader,reader",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                }
            );
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