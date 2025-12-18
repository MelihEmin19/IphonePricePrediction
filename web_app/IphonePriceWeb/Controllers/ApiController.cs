using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IphonePriceWeb.Services;
using IphonePriceWeb.Models;
using IphonePriceWeb.Data;

namespace IphonePriceWeb.Controllers
{
    /// <summary>
    /// Internal API Controller - AJAX istekleri için
    /// Dinamik veritabanı sorguları kullanıyor
    /// </summary>
    [Route("api/[controller]")]
    public class ApiController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApiController> _logger;

        public ApiController(ApiService apiService, ApplicationDbContext context, ILogger<ApiController> logger)
        {
            _apiService = apiService;
            _context = context;
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
        /// Belirli bir modelin özelliklerini getir - DİNAMİK VERİTABANI SORGUSU
        /// </summary>
        [HttpGet("specs/{modelId}")]
        public async Task<IActionResult> GetSpecs(int modelId)
        {
            try
            {
                // Veritabanından o modele ait spec'leri çek
                var specs = await _context.Specs
                    .Where(s => s.ModelId == modelId)
                    .Select(s => new { ram = s.RamGb, storage = s.StorageGb })
                    .Distinct()
                    .OrderBy(s => s.ram)
                    .ThenBy(s => s.storage)
                    .ToListAsync();

                // Eğer veritabanında spec yoksa tüm spec'leri getir
                if (specs.Count == 0)
                {
                    specs = await _context.Specs
                        .Select(s => new { ram = s.RamGb, storage = s.StorageGb })
                        .Distinct()
                        .OrderBy(s => s.ram)
                        .ThenBy(s => s.storage)
                        .ToListAsync();
                }

                // Hala boşsa varsayılan değerler
                if (specs.Count == 0)
                {
                    var defaultSpecs = new[]
                    {
                        new { ram = 4, storage = 64 },
                        new { ram = 4, storage = 128 },
                        new { ram = 4, storage = 256 },
                        new { ram = 6, storage = 128 },
                        new { ram = 6, storage = 256 },
                        new { ram = 6, storage = 512 },
                        new { ram = 8, storage = 256 },
                        new { ram = 8, storage = 512 },
                        new { ram = 8, storage = 1024 }
                    };
                    return Json(new { success = true, data = defaultSpecs });
                }

                return Json(new { success = true, data = specs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"API specs hatası: {modelId}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Model için RAM seçenekleri
        /// </summary>
        [HttpGet("ram-options/{modelId}")]
        public async Task<IActionResult> GetRamOptions(int modelId)
        {
            try
            {
                var ramOptions = await _context.Specs
                    .Where(s => s.ModelId == modelId)
                    .Select(s => s.RamGb)
                    .Distinct()
                    .OrderBy(r => r)
                    .ToListAsync();

                if (ramOptions.Count == 0)
                {
                    ramOptions = await _context.Specs
                        .Select(s => s.RamGb)
                        .Distinct()
                        .OrderBy(r => r)
                        .ToListAsync();
                }

                return Json(new { success = true, data = ramOptions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"RAM options hatası: {modelId}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Model için Storage seçenekleri
        /// </summary>
        [HttpGet("storage-options/{modelId}")]
        public async Task<IActionResult> GetStorageOptions(int modelId)
        {
            try
            {
                var storageOptions = await _context.Specs
                    .Where(s => s.ModelId == modelId)
                    .Select(s => s.StorageGb)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToListAsync();

                if (storageOptions.Count == 0)
                {
                    storageOptions = await _context.Specs
                        .Select(s => s.StorageGb)
                        .Distinct()
                        .OrderBy(s => s)
                        .ToListAsync();
                }

                return Json(new { success = true, data = storageOptions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Storage options hatası: {modelId}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Hızlı fiyat tahmini (AJAX)
        /// </summary>
        [HttpPost("quick-predict")]
        public async Task<IActionResult> QuickPredict([FromBody] LegacyPredictionRequest request)
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
