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
}

