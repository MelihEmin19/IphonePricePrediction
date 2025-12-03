using Microsoft.AspNetCore.Mvc;
using IphonePriceWeb.Services;
using IphonePriceWeb.Models;

namespace IphonePriceWeb.Controllers
{
    /// <summary>
    /// Internal API Controller - AJAX istekleri için
    /// </summary>
    [Route("api/[controller]")]
    public class ApiController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ILogger<ApiController> _logger;

        public ApiController(ApiService apiService, ILogger<ApiController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        /// <summary>
        /// Modelleri JSON olarak döndür
        /// </summary>
        [HttpGet("models")]
        public async Task<IActionResult> GetModels()
        {
            try
            {
                var models = await _apiService.GetModelsAsync();
                return Json(new { success = true, data = models });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API models hatası");
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Belirli bir modelin özelliklerini getir
        /// </summary>
        [HttpGet("specs/{modelId}")]
        public async Task<IActionResult> GetSpecs(int modelId)
        {
            try
            {
                // Demo specs - gerçek projede API'den çekilmeli
                var specs = new List<object>
                {
                    new { ram = 4, storage = 64 },
                    new { ram = 4, storage = 128 },
                    new { ram = 4, storage = 256 },
                    new { ram = 6, storage = 128 },
                    new { ram = 6, storage = 256 },
                    new { ram = 6, storage = 512 }
                };

                return Json(new { success = true, data = specs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"API specs hatası: {modelId}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Hızlı fiyat tahmini (AJAX)
        /// </summary>
        [HttpPost("quick-predict")]
        public async Task<IActionResult> QuickPredict([FromBody] PredictionRequest request)
        {
            try
            {
                var result = await _apiService.PredictPriceAsync(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Quick predict hatası");
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Dashboard istatistikleri
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var stats = await _apiService.GetDashboardStatsAsync();
                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API stats hatası");
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Sağlık kontrolü
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Json(new
            {
                status = "healthy",
                timestamp = DateTime.Now,
                version = "1.0.0"
            });
        }
    }
}

