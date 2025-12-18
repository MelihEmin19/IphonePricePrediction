using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IphonePriceWeb.Models
{
    /// <summary>
    /// Fiyat tahmini için input modeli - YENİ YAPI
    /// v_specs_catalog ve v_condition_catalog view'larını kullanır
    /// </summary>
    public class PredictionRequest
    {
        [Required(ErrorMessage = "Cihaz seçimi zorunludur")]
        [Display(Name = "Cihaz / Specs")]
        public int SpecsId { get; set; }

        [Required(ErrorMessage = "Durum seçimi zorunludur")]
        [Display(Name = "Cihaz Durumu")]
        public int ConditionId { get; set; }
    }

    /// <summary>
    /// Eski API uyumluluğu için eski format
    /// </summary>
    public class LegacyPredictionRequest
    {
        [Required(ErrorMessage = "Model seçimi zorunludur")]
        [Display(Name = "iPhone Modeli")]
        public int ModelId { get; set; }

        [Required(ErrorMessage = "RAM seçimi zorunludur")]
        [Display(Name = "RAM (GB)")]
        public int RamGb { get; set; }

        [Required(ErrorMessage = "Hafıza seçimi zorunludur")]
        [Display(Name = "Hafıza (GB)")]
        public int StorageGb { get; set; }

        [Required(ErrorMessage = "Durum seçimi zorunludur")]
        [Display(Name = "Kozmetik Durum")]
        public string Condition { get; set; } = string.Empty;

        [Display(Name = "Çıkış Yılı")]
        public int ReleaseYear { get; set; }
    }

    /// <summary>
    /// API'den gelen tahmin yanıtı
    /// </summary>
    public class PredictionResponse
    {
        public bool Success { get; set; }
        public PredictionData? Data { get; set; }
        public string? Error { get; set; }
    }

    public class PredictionData
    {
        [JsonPropertyName("prediction")]
        public PredictionDetail? Prediction { get; set; }
        
        [JsonPropertyName("exchange_rate")]
        public double ExchangeRate { get; set; }
        
        [JsonPropertyName("input")]
        public PredictionInput? Input { get; set; }
        
        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }
    }

    public class PredictionDetail
    {
        [JsonPropertyName("price_tl")]
        public double PriceTl { get; set; }
        
        [JsonPropertyName("price_usd")]
        public double PriceUsd { get; set; }
        
        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
        
        [JsonPropertyName("range")]
        public PriceRange? Range { get; set; }
    }

    public class PriceRange
    {
        public double Min { get; set; }
        public double Max { get; set; }
    }

    public class PredictionInput
    {
        [JsonPropertyName("model_id")]
        public int ModelId { get; set; }
        
        [JsonPropertyName("ram_gb")]
        public int RamGb { get; set; }
        
        [JsonPropertyName("storage_gb")]
        public int StorageGb { get; set; }
        
        [JsonPropertyName("condition")]
        public string? Condition { get; set; }
    }

    /// <summary>
    /// Specs Dropdown Item - v_specs_catalog view'ından
    /// </summary>
    public class SpecsDropdownItem
    {
        public int SpecsId { get; set; }
        public string Label { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string SegmentName { get; set; } = string.Empty;
        public int ReleaseYear { get; set; }
        public int RamGb { get; set; }
        public int StorageGb { get; set; }
    }

    /// <summary>
    /// Condition Dropdown Item - v_condition_catalog view'ından
    /// </summary>
    public class ConditionDropdownItem
    {
        public int ConditionId { get; set; }
        public string ConditionName { get; set; } = string.Empty;
        public int ConditionPuan { get; set; }
    }

    /// <summary>
    /// Marka modeli (eski uyumluluk)
    /// </summary>
    public class Brand
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model modeli
    /// </summary>
    public class PhoneModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("release_year")]
        public int ReleaseYear { get; set; }
        
        [JsonPropertyName("segment_name")]
        public string SegmentName { get; set; } = string.Empty;
        
        [JsonPropertyName("segment_id")]
        public int SegmentId { get; set; }

        [JsonPropertyName("brand_name")]
        public string BrandName { get; set; } = "Apple";
    }

    /// <summary>
    /// API yanıt wrapper
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Dashboard istatistikleri - v_dashboard_stats view'ından
    /// </summary>
    public class DashboardStats
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
    /// Login modeli
    /// </summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı zorunludur")]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Beni Hatırla")]
        public bool RememberMe { get; set; }
    }

    /// <summary>
    /// Register modeli
    /// </summary>
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı zorunludur")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-50 karakter arasında olmalıdır")]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [Display(Name = "E-posta (Opsiyonel)")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre tekrarı zorunludur")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Şifreler uyuşmuyor")]
        [Display(Name = "Şifre Tekrar")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Kullanıcı profil modeli
    /// </summary>
    public class UserProfileViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string LoginTime { get; set; } = string.Empty;
        public int PredictionCount { get; set; }
    }

    /// <summary>
    /// Model karşılaştırma modeli
    /// </summary>
    public class ModelCompareViewModel
    {
        public PhoneModel? Model1 { get; set; }
        public PhoneModel? Model2 { get; set; }
        public ModelStats? Stats1 { get; set; }
        public ModelStats? Stats2 { get; set; }
    }

    /// <summary>
    /// Model istatistikleri (karşılaştırma için)
    /// </summary>
    public class ModelStats
    {
        [JsonPropertyName("model_name")]
        public string ModelName { get; set; } = string.Empty;

        [JsonPropertyName("ram_gb")]
        public List<int> RamGb { get; set; } = new List<int>();

        [JsonPropertyName("storage_gb")]
        public List<int> StorageGb { get; set; } = new List<int>();

        [JsonPropertyName("camera_mp")]
        public int CameraMp { get; set; }

        [JsonPropertyName("segment")]
        public string Segment { get; set; } = string.Empty;

        [JsonPropertyName("release_year")]
        public int ReleaseYear { get; set; }

        [JsonPropertyName("avg_price")]
        public double AvgPrice { get; set; }

        [JsonPropertyName("min_price")]
        public double MinPrice { get; set; }

        [JsonPropertyName("max_price")]
        public double MaxPrice { get; set; }

        [JsonPropertyName("listing_count")]
        public int ListingCount { get; set; }
    }

    /// <summary>
    /// Tahmin geçmişi öğesi - v_user_history_masked view'ından
    /// </summary>
    public class PredictionHistoryItem
    {
        public int PredictionId { get; set; }
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
    /// Tahmin istatistikleri
    /// </summary>
    public class PredictionStatsViewModel
    {
        public int TotalPredictions { get; set; }
        public int TodayPredictions { get; set; }
        public decimal AverageConfidence { get; set; }
        public string MostPredictedModel { get; set; } = string.Empty;
        public decimal AveragePredictedPrice { get; set; }
    }

    // =====================================================
    // ADMIN PANEL MODELLERİ
    // =====================================================

    /// <summary>
    /// Segment ekleme/düzenleme modeli
    /// </summary>
    public class SegmentFormModel
    {
        [Required(ErrorMessage = "Segment adı zorunludur")]
        [StringLength(20, ErrorMessage = "Segment adı en fazla 20 karakter olabilir")]
        [Display(Name = "Segment Adı")]
        public string SegmentName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model ekleme modeli
    /// </summary>
    public class ModelFormModel
    {
        [Required(ErrorMessage = "Model adı zorunludur")]
        [StringLength(100, ErrorMessage = "Model adı en fazla 100 karakter olabilir")]
        [Display(Name = "Model Adı")]
        public string ModelName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Segment seçimi zorunludur")]
        [Display(Name = "Segment")]
        public string SegmentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Çıkış yılı zorunludur")]
        [Range(2010, 2030, ErrorMessage = "Geçerli bir yıl giriniz")]
        [Display(Name = "Çıkış Yılı")]
        public int ReleaseYear { get; set; }
    }

    /// <summary>
    /// Model güncelleme modeli
    /// </summary>
    public class ModelUpdateModel : ModelFormModel
    {
        public int ModelId { get; set; }
    }

    /// <summary>
    /// Specs ekleme modeli
    /// </summary>
    public class SpecsFormModel
    {
        [Required(ErrorMessage = "Model seçimi zorunludur")]
        [Display(Name = "Model")]
        public int ModelId { get; set; }

        [Required(ErrorMessage = "RAM zorunludur")]
        [Range(1, 32, ErrorMessage = "RAM 1-32 GB arasında olmalıdır")]
        [Display(Name = "RAM (GB)")]
        public int RamGb { get; set; }

        [Required(ErrorMessage = "Depolama zorunludur")]
        [Display(Name = "Depolama (GB)")]
        public int StorageGb { get; set; }

        [Required(ErrorMessage = "Kamera MP zorunludur")]
        [Range(1, 200, ErrorMessage = "Kamera 1-200 MP arasında olmalıdır")]
        [Display(Name = "Kamera (MP)")]
        public int KameraMp { get; set; }

        [Required(ErrorMessage = "Ekran boyutu zorunludur")]
        [Range(3.0, 8.0, ErrorMessage = "Ekran boyutu 3-8 inç arasında olmalıdır")]
        [Display(Name = "Ekran Boyutu (inç)")]
        public decimal EkranBoyutu { get; set; }

        [Required(ErrorMessage = "Batarya kapasitesi zorunludur")]
        [Range(1000, 10000, ErrorMessage = "Batarya 1000-10000 mAh arasında olmalıdır")]
        [Display(Name = "Batarya (mAh)")]
        public int BataryaMah { get; set; }
    }

    /// <summary>
    /// Specs güncelleme modeli
    /// </summary>
    public class SpecsUpdateModel : SpecsFormModel
    {
        public int SpecsId { get; set; }
    }

    /// <summary>
    /// Condition ekleme modeli
    /// </summary>
    public class ConditionFormModel
    {
        [Required(ErrorMessage = "Durum adı zorunludur")]
        [StringLength(20, ErrorMessage = "Durum adı en fazla 20 karakter olabilir")]
        [Display(Name = "Durum Adı")]
        public string ConditionName { get; set; } = string.Empty;

        [Range(0.0, 1.5, ErrorMessage = "Çarpan 0-1.5 arasında olmalıdır")]
        [Display(Name = "Fiyat Çarpanı")]
        public decimal Multiplier { get; set; } = 1.00m;
    }

    /// <summary>
    /// Rol atama modeli
    /// </summary>
    public class RoleAssignModel
    {
        [Required(ErrorMessage = "Kullanıcı seçimi zorunludur")]
        [Display(Name = "Kullanıcı")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Rol seçimi zorunludur")]
        [Display(Name = "Rol")]
        public string RoleName { get; set; } = string.Empty;
    }
}
