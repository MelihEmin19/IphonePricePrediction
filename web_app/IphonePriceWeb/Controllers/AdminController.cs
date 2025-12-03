using Microsoft.AspNetCore.Mvc;
using IphonePriceWeb.Services;

namespace IphonePriceWeb.Controllers
{
    /// <summary>
    /// Admin Paneli Controller
    /// </summary>
    public class AdminController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApiService apiService, ILogger<AdminController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        /// <summary>
        /// Admin dashboard
        /// </summary>
        public async Task<IActionResult> Panel()
        {
            try
            {
                var stats = await _apiService.GetDashboardStatsAsync();
                return View(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard yüklenirken hata");
                ViewBag.Error = "Dashboard verileri yüklenemedi";
                return View();
            }
        }

        /// <summary>
        /// Scraper'ı tetikle
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> TriggerScraper()
        {
            try
            {
                // API'ye scraper tetikleme isteği gönder
                var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("http://localhost:3000");
                
                var response = await httpClient.PostAsync("/api/admin/scrape", null);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Message"] = "Scraper başarıyla başlatıldı (arka planda çalışıyor)";
                }
                else
                {
                    TempData["Error"] = "Scraper başlatılamadı";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scraper tetiklenemedi");
                TempData["Error"] = "Scraper tetiklenemedi: " + ex.Message;
            }

            return RedirectToAction("Panel");
        }
    }
}

