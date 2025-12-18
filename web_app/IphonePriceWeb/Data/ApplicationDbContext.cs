using Microsoft.EntityFrameworkCore;
using IphonePriceWeb.Data.Entities;

namespace IphonePriceWeb.Data
{
    /// <summary>
    /// Entity Framework Core Database Context
    /// Yeni veritabanı yapısına uygun - View ve SP destekli
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =====================================================
        // TABLO DbSet'leri
        // =====================================================
        public DbSet<Segment> Segments { get; set; }
        public DbSet<Condition> Conditions { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Model> Models { get; set; }
        public DbSet<Spec> Specs { get; set; }
        public DbSet<Prediction> Predictions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        // =====================================================
        // VIEW DbSet'leri (Read-Only)
        // =====================================================
        public DbSet<SpecsCatalogView> SpecsCatalog { get; set; }
        public DbSet<ConditionCatalogView> ConditionCatalog { get; set; }
        public DbSet<ModelsCatalogView> ModelsCatalog { get; set; }
        public DbSet<SegmentsCatalogView> SegmentsCatalog { get; set; }
        public DbSet<UserHistoryView> UserHistory { get; set; }
        public DbSet<AdminModelStatsView> AdminModelStats { get; set; }
        public DbSet<AdminConditionStatsView> AdminConditionStats { get; set; }
        public DbSet<UsersMaskedView> UsersMasked { get; set; }
        public DbSet<DashboardStatsView> DashboardStats { get; set; }
        public DbSet<UserRolesDetailView> UserRolesDetail { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================================================
            // TABLO KONFIGURASRASYONLARI
            // =====================================================

            // Segment konfigürasyonu
            modelBuilder.Entity<Segment>(entity =>
            {
                entity.ToTable("segments");
                entity.HasKey(e => e.SegmentId);
                entity.Property(e => e.SegmentId).HasColumnName("segment_id");
                entity.Property(e => e.SegmentName).HasColumnName("segment_name").HasMaxLength(20);
                entity.Property(e => e.SegmentPuan).HasColumnName("segment_puan");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            // Condition konfigürasyonu
            modelBuilder.Entity<Condition>(entity =>
            {
                entity.ToTable("conditions");
                entity.HasKey(e => e.ConditionId);
                entity.Property(e => e.ConditionId).HasColumnName("condition_id");
                entity.Property(e => e.ConditionName).HasColumnName("condition_name").HasMaxLength(20);
                entity.Property(e => e.ConditionPuan).HasColumnName("condition_puan");
                entity.Property(e => e.Multiplier).HasColumnName("multiplier").HasColumnType("decimal(4,2)");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            // Role konfigürasyonu
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("roles");
                entity.HasKey(e => e.RoleId);
                entity.Property(e => e.RoleId).HasColumnName("role_id");
                entity.Property(e => e.RoleName).HasColumnName("role_name").HasMaxLength(20);
                entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(100);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            // User konfigürasyonu
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(50);
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(100);
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            });

            // UserRole konfigürasyonu
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("user_roles");
                entity.HasKey(e => e.UserRoleId);
                entity.Property(e => e.UserRoleId).HasColumnName("user_role_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.RoleId).HasColumnName("role_id");
                entity.Property(e => e.AssignedAt).HasColumnName("assigned_at");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(e => e.UserId);

                entity.HasOne(e => e.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(e => e.RoleId);
            });

            // Model konfigürasyonu
            modelBuilder.Entity<Model>(entity =>
            {
                entity.ToTable("models");
                entity.HasKey(e => e.ModelId);
                entity.Property(e => e.ModelId).HasColumnName("model_id");
                entity.Property(e => e.ModelName).HasColumnName("model_name").HasMaxLength(100);
                entity.Property(e => e.SegmentId).HasColumnName("segment_id");
                entity.Property(e => e.ReleaseYear).HasColumnName("release_year");
                entity.Property(e => e.ModelKodu).HasColumnName("model_kodu");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.HasOne(e => e.Segment)
                    .WithMany(s => s.Models)
                    .HasForeignKey(e => e.SegmentId);
            });

            // Spec konfigürasyonu
            modelBuilder.Entity<Spec>(entity =>
            {
                entity.ToTable("specs");
                entity.HasKey(e => e.SpecsId);
                entity.Property(e => e.SpecsId).HasColumnName("specs_id");
                entity.Property(e => e.ModelId).HasColumnName("model_id");
                entity.Property(e => e.RamGb).HasColumnName("ram_gb");
                entity.Property(e => e.StorageGb).HasColumnName("storage_gb");
                entity.Property(e => e.KameraMp).HasColumnName("kamera_mp");
                entity.Property(e => e.EkranBoyutu).HasColumnName("ekran_boyutu").HasColumnType("decimal(3,1)");
                entity.Property(e => e.BataryaMah).HasColumnName("batarya_mah");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.HasOne(e => e.Model)
                    .WithMany(m => m.Specs)
                    .HasForeignKey(e => e.ModelId);
            });

            // Prediction konfigürasyonu
            modelBuilder.Entity<Prediction>(entity =>
            {
                entity.ToTable("predictions");
                entity.HasKey(e => e.PredictionId);
                entity.Property(e => e.PredictionId).HasColumnName("prediction_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.SpecsId).HasColumnName("specs_id");
                entity.Property(e => e.ConditionId).HasColumnName("condition_id");
                entity.Property(e => e.PredictedPrice).HasColumnName("predicted_price").HasColumnType("decimal(10,2)");
                entity.Property(e => e.ConfidenceScore).HasColumnName("confidence_score").HasColumnType("decimal(5,2)");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Predictions)
                    .HasForeignKey(e => e.UserId);

                entity.HasOne(e => e.Spec)
                    .WithMany(s => s.Predictions)
                    .HasForeignKey(e => e.SpecsId);

                entity.HasOne(e => e.Condition)
                    .WithMany(c => c.Predictions)
                    .HasForeignKey(e => e.ConditionId);
            });

            // AuditLog konfigürasyonu
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("audit_log");
                entity.HasKey(e => e.AuditId);
                entity.Property(e => e.AuditId).HasColumnName("audit_id");
                entity.Property(e => e.TableName).HasColumnName("table_name").HasMaxLength(50);
                entity.Property(e => e.Operation).HasColumnName("operation").HasMaxLength(10);
                entity.Property(e => e.RecordId).HasColumnName("record_id");
                entity.Property(e => e.OldData).HasColumnName("old_data").HasColumnType("jsonb");
                entity.Property(e => e.NewData).HasColumnName("new_data").HasColumnType("jsonb");
                entity.Property(e => e.ChangedBy).HasColumnName("changed_by").HasMaxLength(50);
                entity.Property(e => e.ChangedAt).HasColumnName("changed_at");
            });

            // =====================================================
            // VIEW KONFİGÜRASYONLARI (Keyless)
            // =====================================================

            // v_specs_catalog
            modelBuilder.Entity<SpecsCatalogView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("v_specs_catalog");
                entity.Property(e => e.SpecsId).HasColumnName("specs_id");
                entity.Property(e => e.ModelId).HasColumnName("model_id");
                entity.Property(e => e.ModelName).HasColumnName("model_name");
                entity.Property(e => e.SegmentName).HasColumnName("segment_name");
                entity.Property(e => e.SegmentPuan).HasColumnName("segment_puan");
                entity.Property(e => e.ReleaseYear).HasColumnName("release_year");
                entity.Property(e => e.RamGb).HasColumnName("ram_gb");
                entity.Property(e => e.StorageGb).HasColumnName("storage_gb");
                entity.Property(e => e.KameraMp).HasColumnName("kamera_mp");
                entity.Property(e => e.EkranBoyutu).HasColumnName("ekran_boyutu");
                entity.Property(e => e.BataryaMah).HasColumnName("batarya_mah");
                entity.Property(e => e.Label).HasColumnName("label");
            });

            // v_condition_catalog
            modelBuilder.Entity<ConditionCatalogView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("v_condition_catalog");
                entity.Property(e => e.ConditionId).HasColumnName("condition_id");
                entity.Property(e => e.ConditionName).HasColumnName("condition_name");
                entity.Property(e => e.ConditionPuan).HasColumnName("condition_puan");
                entity.Property(e => e.Multiplier).HasColumnName("multiplier");
            });

            // v_models_catalog
            modelBuilder.Entity<ModelsCatalogView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("v_models_catalog");
                entity.Property(e => e.ModelId).HasColumnName("model_id");
                entity.Property(e => e.ModelName).HasColumnName("model_name");
                entity.Property(e => e.SegmentId).HasColumnName("segment_id");
                entity.Property(e => e.SegmentName).HasColumnName("segment_name");
                entity.Property(e => e.ReleaseYear).HasColumnName("release_year");
                entity.Property(e => e.ModelKodu).HasColumnName("model_kodu");
                entity.Property(e => e.SpecsCount).HasColumnName("specs_count");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            // v_segments_catalog
            modelBuilder.Entity<SegmentsCatalogView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("v_segments_catalog");
                entity.Property(e => e.SegmentId).HasColumnName("segment_id");
                entity.Property(e => e.SegmentName).HasColumnName("segment_name");
                entity.Property(e => e.SegmentPuan).HasColumnName("segment_puan");
                entity.Property(e => e.ModelCount).HasColumnName("model_count");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            // v_user_history_masked
            modelBuilder.Entity<UserHistoryView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("v_user_history_masked");
                entity.Property(e => e.PredictionId).HasColumnName("prediction_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.UsernameMasked).HasColumnName("username_masked");
                entity.Property(e => e.SpecsLabel).HasColumnName("specs_label");
                entity.Property(e => e.ModelName).HasColumnName("model_name");
                entity.Property(e => e.StorageGb).HasColumnName("storage_gb");
                entity.Property(e => e.RamGb).HasColumnName("ram_gb");
                entity.Property(e => e.ConditionName).HasColumnName("condition_name");
                entity.Property(e => e.PredictedPrice).HasColumnName("predicted_price");
                entity.Property(e => e.FormattedPrice).HasColumnName("formatted_price");
                entity.Property(e => e.ConfidenceScore).HasColumnName("confidence_score");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            // v_admin_model_stats
            modelBuilder.Entity<AdminModelStatsView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("v_admin_model_stats");
                entity.Property(e => e.ModelId).HasColumnName("model_id");
                entity.Property(e => e.ModelName).HasColumnName("model_name");
                entity.Property(e => e.SegmentName).HasColumnName("segment_name");
                entity.Property(e => e.ReleaseYear).HasColumnName("release_year");
                entity.Property(e => e.SpecsCount).HasColumnName("specs_count");
                entity.Property(e => e.PredictionCount).HasColumnName("prediction_count");
                entity.Property(e => e.AvgPredictedPrice).HasColumnName("avg_predicted_price");
                entity.Property(e => e.MinPredictedPrice).HasColumnName("min_predicted_price");
                entity.Property(e => e.MaxPredictedPrice).HasColumnName("max_predicted_price");
                entity.Property(e => e.AvgConfidence).HasColumnName("avg_confidence");
            });

            // v_admin_condition_stats
            modelBuilder.Entity<AdminConditionStatsView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("v_admin_condition_stats");
                entity.Property(e => e.ConditionId).HasColumnName("condition_id");
                entity.Property(e => e.ConditionName).HasColumnName("condition_name");
                entity.Property(e => e.ConditionPuan).HasColumnName("condition_puan");
                entity.Property(e => e.PredictionCount).HasColumnName("prediction_count");
                entity.Property(e => e.AvgPredictedPrice).HasColumnName("avg_predicted_price");
                entity.Property(e => e.MinPredictedPrice).HasColumnName("min_predicted_price");
                entity.Property(e => e.MaxPredictedPrice).HasColumnName("max_predicted_price");
            });

            // v_users_masked
            modelBuilder.Entity<UsersMaskedView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("v_users_masked");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Username).HasColumnName("username");
                entity.Property(e => e.UsernameMasked).HasColumnName("username_masked");
                entity.Property(e => e.EmailMasked).HasColumnName("email_masked");
                entity.Property(e => e.RoleName).HasColumnName("role_name");
                entity.Property(e => e.PredictionCount).HasColumnName("prediction_count");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            });

            // v_dashboard_stats
            modelBuilder.Entity<DashboardStatsView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("v_dashboard_stats");
                entity.Property(e => e.TotalUsers).HasColumnName("total_users");
                entity.Property(e => e.AdminCount).HasColumnName("admin_count");
                entity.Property(e => e.TotalModels).HasColumnName("total_models");
                entity.Property(e => e.TotalSpecs).HasColumnName("total_specs");
                entity.Property(e => e.TotalPredictions).HasColumnName("total_predictions");
                entity.Property(e => e.PredictionsToday).HasColumnName("predictions_today");
                entity.Property(e => e.AvgPredictedPrice).HasColumnName("avg_predicted_price");
                entity.Property(e => e.AvgConfidence).HasColumnName("avg_confidence");
                entity.Property(e => e.MostPredictedModel).HasColumnName("most_predicted_model");
            });

            // v_user_roles_detail
            modelBuilder.Entity<UserRolesDetailView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("v_user_roles_detail");
                entity.Property(e => e.UserRoleId).HasColumnName("user_role_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Username).HasColumnName("username");
                entity.Property(e => e.RoleId).HasColumnName("role_id");
                entity.Property(e => e.RoleName).HasColumnName("role_name");
                entity.Property(e => e.RoleDescription).HasColumnName("role_description");
                entity.Property(e => e.AssignedAt).HasColumnName("assigned_at");
            });
        }
    }
}
