using System.Text;
using System.Text.Json;
using IphonePriceWeb.Models;

namespace IphonePriceWeb.Services
{
    /// <summary>
    /// Node.js API ile iletişim servisi
    /// </summary>
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly ILogger<ApiService> _logger;

        public ApiService(HttpClient httpClient, IConfiguration configuration, ILogger<ApiService> logger)
        {
            _httpClient = httpClient;
            _apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:3000";
            _logger = logger;

            _httpClient.BaseAddress = new Uri(_apiBaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Tüm markaları getir
        /// </summary>
        public async Task<List<Brand>> GetBrandsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/brands");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<Brand>>>(content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return apiResponse?.Data ?? new List<Brand>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Marka listesi alınamadı");
                return new List<Brand>();
            }
        }

        /// <summary>
        /// Tüm modelleri getir
        /// </summary>
        public async Task<List<PhoneModel>> GetModelsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/models");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<PhoneModel>>>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return apiResponse?.Data ?? new List<PhoneModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Model listesi alınamadı");
                return new List<PhoneModel>();
            }
        }

        /// <summary>
        /// Fiyat tahmini yap (Legacy format - backward compatibility)
        /// </summary>
        public async Task<PredictionResponse> PredictPriceAsync(LegacyPredictionRequest request)
        {
            try
            {
                var jsonContent = JsonSerializer.Serialize(new
                {
                    model_id = request.ModelId,
                    ram_gb = request.RamGb,
                    storage_gb = request.StorageGb,
                    condition = request.Condition,
                    release_year = request.ReleaseYear
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/predict", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"API Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<PredictionResponse>(responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result ?? new PredictionResponse { Success = false, Error = "Yanıt parse edilemedi" };
                }
                else
                {
                    return new PredictionResponse 
                    { 
                        Success = false, 
                        Error = $"API hatası: {response.StatusCode}" 
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tahmin isteği başarısız");
                return new PredictionResponse 
                { 
                    Success = false, 
                    Error = ex.Message 
                };
            }
        }

        /// <summary>
        /// Dashboard istatistiklerini getir
        /// </summary>
        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/stats/dashboard");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<DashboardStats>>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return apiResponse?.Data ?? new DashboardStats();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard istatistikleri alınamadı");
                return new DashboardStats();
            }
        }

        /// <summary>
        /// Health check
        /// </summary>
        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Model istatistiklerini getir (karşılaştırma için)
        /// </summary>
        public async Task<ModelStats?> GetModelStatsAsync(string modelName)
        {
            try
            {
                var encodedName = Uri.EscapeDataString(modelName);
                var response = await _httpClient.GetAsync($"/api/model-stats/{encodedName}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<ModelStats>>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return apiResponse?.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Model istatistikleri alınamadı: {modelName}");
                return null;
            }
        }
    }
}
