namespace IphonePriceWeb.Data.Entities
{
    /// <summary>
    /// Segment Entity - iPhone segment kategorileri (Mini, Base, Plus, Pro, Pro Max)
    /// </summary>
    public class Segment
    {
        public int SegmentId { get; set; }
        public string SegmentName { get; set; } = string.Empty;
        public int SegmentPuan { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public ICollection<Model> Models { get; set; } = new List<Model>();
    }

    /// <summary>
    /// Condition Entity - Cihaz durumları (Outlet, İyi, Çok İyi, Mükemmel)
    /// </summary>
    public class Condition
    {
        public int ConditionId { get; set; }
        public string ConditionName { get; set; } = string.Empty;
        public int ConditionPuan { get; set; }
        public decimal Multiplier { get; set; } = 1.00m;
        public DateTime CreatedAt { get; set; }

        // Navigation
        public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
    }

    /// <summary>
    /// Role Entity - Uygulama rolleri (Admin, User)
    /// </summary>
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    /// <summary>
    /// User Entity - Sistem kullanıcıları
    /// </summary>
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
    }

    /// <summary>
    /// UserRole Entity - Kullanıcı-Rol ilişkisi (M:N)
    /// </summary>
    public class UserRole
    {
        public int UserRoleId { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public DateTime AssignedAt { get; set; }

        // Navigation
        public User? User { get; set; }
        public Role? Role { get; set; }
    }

    /// <summary>
    /// Model Entity - iPhone modelleri
    /// </summary>
    public class Model
    {
        public int ModelId { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public int SegmentId { get; set; }
        public int ReleaseYear { get; set; }
        public int? ModelKodu { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public Segment? Segment { get; set; }
        public ICollection<Spec> Specs { get; set; } = new List<Spec>();
    }

    /// <summary>
    /// Spec Entity - Teknik özellikler (RAM, Storage, Kamera, Ekran, Batarya)
    /// </summary>
    public class Spec
    {
        public int SpecsId { get; set; }
        public int ModelId { get; set; }
        public int RamGb { get; set; }
        public int StorageGb { get; set; }
        public int KameraMp { get; set; }
        public decimal EkranBoyutu { get; set; }
        public int BataryaMah { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public Model? Model { get; set; }
        public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
    }

    /// <summary>
    /// Prediction Entity - Kullanıcı tahminleri
    /// </summary>
    public class Prediction
    {
        public int PredictionId { get; set; }
        public int? UserId { get; set; }
        public int SpecsId { get; set; }
        public int ConditionId { get; set; }
        public decimal PredictedPrice { get; set; }
        public decimal? ConfidenceScore { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public User? User { get; set; }
        public Spec? Spec { get; set; }
        public Condition? Condition { get; set; }
    }

    /// <summary>
    /// AuditLog Entity - Değişiklik kayıtları
    /// </summary>
    public class AuditLog
    {
        public int AuditId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public int? RecordId { get; set; }
        public string? OldData { get; set; }
        public string? NewData { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
    }

    // =====================================================
    // VIEW ENTITY'LERİ (Read-Only)
    // =====================================================

    /// <summary>
    /// v_specs_catalog View Entity - Specs kataloğu (Dropdown için)
    /// </summary>
    public class SpecsCatalogView
    {
        public int SpecsId { get; set; }
        public int ModelId { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public string SegmentName { get; set; } = string.Empty;
        public int SegmentPuan { get; set; }
        public int ReleaseYear { get; set; }
        public int RamGb { get; set; }
        public int StorageGb { get; set; }
        public int KameraMp { get; set; }
        public decimal EkranBoyutu { get; set; }
        public int BataryaMah { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>
    /// v_condition_catalog View Entity
    /// </summary>
    public class ConditionCatalogView
    {
        public int ConditionId { get; set; }
        public string ConditionName { get; set; } = string.Empty;
        public int ConditionPuan { get; set; }
        public decimal Multiplier { get; set; }
    }

    /// <summary>
    /// v_models_catalog View Entity
    /// </summary>
    public class ModelsCatalogView
    {
        public int ModelId { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public int SegmentId { get; set; }
        public string SegmentName { get; set; } = string.Empty;
        public int ReleaseYear { get; set; }
        public int? ModelKodu { get; set; }
        public int SpecsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// v_segments_catalog View Entity
    /// </summary>
    public class SegmentsCatalogView
    {
        public int SegmentId { get; set; }
        public string SegmentName { get; set; } = string.Empty;
        public int SegmentPuan { get; set; }
        public int ModelCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// v_user_history_masked View Entity
    /// </summary>
    public class UserHistoryView
    {
        public int PredictionId { get; set; }
        public int UserId { get; set; }
        public string UsernameMasked { get; set; } = string.Empty;
        public string SpecsLabel { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public int StorageGb { get; set; }
        public int RamGb { get; set; }
        public string ConditionName { get; set; } = string.Empty;
        public decimal PredictedPrice { get; set; }
        public string FormattedPrice { get; set; } = string.Empty;
        public decimal? ConfidenceScore { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// v_admin_model_stats View Entity
    /// </summary>
    public class AdminModelStatsView
    {
        public int ModelId { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public string SegmentName { get; set; } = string.Empty;
        public int ReleaseYear { get; set; }
        public int SpecsCount { get; set; }
        public int PredictionCount { get; set; }
        public decimal AvgPredictedPrice { get; set; }
        public decimal MinPredictedPrice { get; set; }
        public decimal MaxPredictedPrice { get; set; }
        public decimal AvgConfidence { get; set; }
    }

    /// <summary>
    /// v_admin_condition_stats View Entity
    /// </summary>
    public class AdminConditionStatsView
    {
        public int ConditionId { get; set; }
        public string ConditionName { get; set; } = string.Empty;
        public int ConditionPuan { get; set; }
        public int PredictionCount { get; set; }
        public decimal AvgPredictedPrice { get; set; }
        public decimal MinPredictedPrice { get; set; }
        public decimal MaxPredictedPrice { get; set; }
    }

    /// <summary>
    /// v_users_masked View Entity
    /// </summary>
    public class UsersMaskedView
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UsernameMasked { get; set; } = string.Empty;
        public string? EmailMasked { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int PredictionCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// v_dashboard_stats View Entity
    /// </summary>
    public class DashboardStatsView
    {
        public int TotalUsers { get; set; }
        public int AdminCount { get; set; }
        public int TotalModels { get; set; }
        public int TotalSpecs { get; set; }
        public int TotalPredictions { get; set; }
        public int PredictionsToday { get; set; }
        public decimal AvgPredictedPrice { get; set; }
        public decimal AvgConfidence { get; set; }
        public string? MostPredictedModel { get; set; }
    }

    /// <summary>
    /// v_user_roles_detail View Entity
    /// </summary>
    public class UserRolesDetailView
    {
        public int UserRoleId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? RoleDescription { get; set; }
        public DateTime AssignedAt { get; set; }
    }
}
