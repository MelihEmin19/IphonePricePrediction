using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IphonePriceWeb.Services;
using IphonePriceWeb.Models;

namespace IphonePriceWeb.Controllers
{
    /// <summary>
    /// iPhone Model yönetimi Controller
    /// </summary>
    public class ModelController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ILogger<ModelController> _logger;

        public ModelController(ApiService apiService, ILogger<ModelController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        /// <summary>
        /// Tüm modellerin listesi
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var models = await _apiService.GetModelsAsync();
                return View(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Modeller yüklenirken hata");
                ViewBag.Error = "Modeller yüklenemedi.";
                return View(new List<PhoneModel>());
            }
        }

        /// <summary>
        /// Model detayları
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var models = await _apiService.GetModelsAsync();
                var model = models.FirstOrDefault(m => m.Id == id);

                if (model == null)
                {
                    return NotFound();
                }

                // ViewBag ile ek bilgi aktarımı (İster: ViewBag kullanımı)
                ViewBag.ModelId = id;
                ViewBag.LastUpdated = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Model detay hatası: {id}");
                return NotFound();
            }
        }

        /// <summary>
        /// Model karşılaştırma sayfası
        /// </summary>
        public async Task<IActionResult> Compare()
        {
            try
            {
                var models = await _apiService.GetModelsAsync();
                ViewBag.Models = models;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Karşılaştırma sayfası hatası");
                return View();
            }
        }

        /// <summary>
        /// Model karşılaştırma sonucu (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CompareResult(int model1Id, int model2Id)
        {
            try
            {
                var models = await _apiService.GetModelsAsync();
                var model1 = models.FirstOrDefault(m => m.Id == model1Id);
                var model2 = models.FirstOrDefault(m => m.Id == model2Id);

                if (model1 == null || model2 == null)
                {
                    TempData["Error"] = "Seçilen modeller bulunamadı.";
                    return RedirectToAction("Compare");
                }

                // Model istatistiklerini al (RAM, Storage, Camera MP, Ortalama Fiyat)
                var stats1 = await _apiService.GetModelStatsAsync(model1.Name);
                var stats2 = await _apiService.GetModelStatsAsync(model2.Name);

                // TempData ile veri aktarımı (İster: TempData kullanımı)
                TempData["Model1Name"] = model1.Name;
                TempData["Model2Name"] = model2.Name;

                var compareModel = new ModelCompareViewModel
                {
                    Model1 = model1,
                    Model2 = model2,
                    Stats1 = stats1,
                    Stats2 = stats2
                };

                return View(compareModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Karşılaştırma hatası");
                TempData["Error"] = "Karşılaştırma yapılırken hata oluştu.";
                return RedirectToAction("Compare");
            }
        }

        /// <summary>
        /// Fiyat geçmişi grafiği (Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        public IActionResult PriceHistory(int id)
        {
            ViewBag.ModelId = id;
            return View();
        }
    }
}

