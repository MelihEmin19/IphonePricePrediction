using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IphonePriceWeb.Models
{
    /// <summary>
    /// Fiyat tahmini için input modeli
    /// </summary>
    public class PredictionRequest
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
        public string Condition { get; set; }

        [Display(Name = "Çıkış Yılı")]
        public int ReleaseYear { get; set; }
    }

    /// <summary>
    /// API'den gelen tahmin yanıtı
    /// </summary>
    public class PredictionResponse
    {
        public bool Success { get; set; }
        public PredictionData Data { get; set; }
        public string Error { get; set; }
    }

    public class PredictionData
    {
        [JsonPropertyName("prediction")]
        public PredictionDetail Prediction { get; set; }
        
        [JsonPropertyName("exchange_rate")]
        public double ExchangeRate { get; set; }
        
        [JsonPropertyName("input")]
        public PredictionInput Input { get; set; }
        
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }
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
        public PriceRange Range { get; set; }
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
        public string Condition { get; set; }
    }

    /// <summary>
    /// Marka modeli
    /// </summary>
    public class Brand
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// Model modeli
    /// </summary>
    public class PhoneModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("release_year")]
        public int ReleaseYear { get; set; }
        
        [JsonPropertyName("brand_name")]
        public string BrandName { get; set; }
        
        [JsonPropertyName("brand_id")]
        public int BrandId { get; set; }
    }

    /// <summary>
    /// API yanıt wrapper
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// Dashboard istatistikleri
    /// </summary>
    public class DashboardStats
    {
        [JsonPropertyName("overall")]
        public OverallStats Overall { get; set; }
        
        [JsonPropertyName("brands")]
        public List<BrandPrice> Brands { get; set; }
        
        [JsonPropertyName("scraper")]
        public ScraperStats Scraper { get; set; }
    }

    public class OverallStats
    {
        [JsonPropertyName("total_users")]
        public int TotalUsers { get; set; }
        
        [JsonPropertyName("total_models")]
        public int TotalModels { get; set; }
        
        [JsonPropertyName("active_listings")]
        public int ActiveListings { get; set; }
        
        [JsonPropertyName("total_predictions")]
        public int TotalPredictions { get; set; }
        
        [JsonPropertyName("overall_avg_price")]
        public double OverallAvgPrice { get; set; }
    }

    public class BrandPrice
    {
        [JsonPropertyName("brand_name")]
        public string BrandName { get; set; }
        
        [JsonPropertyName("listing_count")]
        public int ListingCount { get; set; }
        
        [JsonPropertyName("avg_price")]
        public double AvgPrice { get; set; }
        
        [JsonPropertyName("min_price")]
        public double MinPrice { get; set; }
        
        [JsonPropertyName("max_price")]
        public double MaxPrice { get; set; }
    }

    public class ScraperStats
    {
        [JsonPropertyName("total_listings")]
        public int TotalListings { get; set; }
        
        [JsonPropertyName("active_listings")]
        public int ActiveListings { get; set; }
        
        [JsonPropertyName("last_scrape_time")]
        public DateTime? LastScrapeTime { get; set; }
        
        [JsonPropertyName("avg_price")]
        public double AvgPrice { get; set; }
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
        [StringLength(50, MinimumLength = 3)]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur")]
        [StringLength(100, MinimumLength = 6)]
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
    /// Tahmin geçmişi öğesi
    /// </summary>
    public class PredictionHistoryItem
    {
        public int Id { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public int Storage { get; set; }
        public int Ram { get; set; }
        public string Condition { get; set; } = string.Empty;
        public decimal PredictedPrice { get; set; }
        public decimal PriceUsd { get; set; }
        public decimal Confidence { get; set; }
        public DateTime PredictedAt { get; set; }
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
}

