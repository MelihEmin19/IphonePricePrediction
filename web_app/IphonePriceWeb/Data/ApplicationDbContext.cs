using Microsoft.EntityFrameworkCore;
using IphonePriceWeb.Data.Entities;

namespace IphonePriceWeb.Data
{
    /// <summary>
    /// Entity Framework Core Database Context
    /// İster: Veritabanı bağlantısı ve CRUD işlemleri
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet tanımlamaları
        public DbSet<User> Users { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Model> Models { get; set; }
        public DbSet<Spec> Specs { get; set; }
        public DbSet<Listing> Listings { get; set; }
        public DbSet<Prediction> Predictions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User konfigürasyonu
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(50);
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
                entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(20);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            });

            // Brand konfigürasyonu
            modelBuilder.Entity<Brand>(entity =>
            {
                entity.ToTable("brands");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            // Model konfigürasyonu
            modelBuilder.Entity<Model>(entity =>
            {
                entity.ToTable("models");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.BrandId).HasColumnName("brand_id");
                entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100);
                entity.Property(e => e.ReleaseYear).HasColumnName("release_year");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                
                entity.HasOne(e => e.Brand)
                    .WithMany(b => b.Models)
                    .HasForeignKey(e => e.BrandId);
            });

            // Spec konfigürasyonu
            modelBuilder.Entity<Spec>(entity =>
            {
                entity.ToTable("specs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ModelId).HasColumnName("model_id");
                entity.Property(e => e.RamGb).HasColumnName("ram_gb");
                entity.Property(e => e.StorageGb).HasColumnName("storage_gb");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                
                entity.HasOne(e => e.Model)
                    .WithMany(m => m.Specs)
                    .HasForeignKey(e => e.ModelId);
            });

            // Listing konfigürasyonu
            modelBuilder.Entity<Listing>(entity =>
            {
                entity.ToTable("listings");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.SpecId).HasColumnName("spec_id");
                entity.Property(e => e.Price).HasColumnName("price").HasColumnType("decimal(10,2)");
                entity.Property(e => e.Condition).HasColumnName("condition").HasMaxLength(20);
                entity.Property(e => e.Source).HasColumnName("source").HasMaxLength(50);
                entity.Property(e => e.Url).HasColumnName("url");
                entity.Property(e => e.ScrapedAt).HasColumnName("scraped_at");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                
                entity.HasOne(e => e.Spec)
                    .WithMany(s => s.Listings)
                    .HasForeignKey(e => e.SpecId);
            });

            // Prediction konfigürasyonu
            modelBuilder.Entity<Prediction>(entity =>
            {
                entity.ToTable("predictions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.SpecId).HasColumnName("spec_id");
                entity.Property(e => e.ConditionValue).HasColumnName("condition").HasMaxLength(20);
                entity.Property(e => e.PredictedPrice).HasColumnName("predicted_price").HasColumnType("decimal(10,2)");
                entity.Property(e => e.ConfidenceScore).HasColumnName("confidence_score").HasColumnType("decimal(5,2)");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Predictions)
                    .HasForeignKey(e => e.UserId);
                    
                entity.HasOne(e => e.Spec)
                    .WithMany(s => s.Predictions)
                    .HasForeignKey(e => e.SpecId);
            });
        }
    }
}

