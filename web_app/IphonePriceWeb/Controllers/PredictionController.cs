using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IphonePriceWeb.Services;
using IphonePriceWeb.Models;

namespace IphonePriceWeb.Controllers
{
    /// <summary>
    /// Tahmin geçmişi ve analiz Controller
    /// </summary>
    public class PredictionController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ILogger<PredictionController> _logger;

        public PredictionController(ApiService apiService, ILogger<PredictionController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        /// <summary>
        /// Tahmin geçmişi listesi
        /// </summary>
        [Authorize]
        public IActionResult History()
        {
            // Demo tahmin geçmişi
            var predictions = new List<PredictionHistoryItem>
            {
                new PredictionHistoryItem 
                { 
                    Id = 1, 
                    ModelName = "iPhone 14 Pro", 
                    Storage = 256, 
                    Condition = "Mükemmel",
                    PredictedPrice = 42500,
                    PredictedAt = DateTime.Now.AddDays(-1)
                },
                new PredictionHistoryItem 
                { 
                    Id = 2, 
                    ModelName = "iPhone 13", 
                    Storage = 128, 
                    Condition = "İyi",
                    PredictedPrice = 28000,
                    PredictedAt = DateTime.Now.AddDays(-3)
                },
                new PredictionHistoryItem 
                { 
                    Id = 3, 
                    ModelName = "iPhone 12", 
                    Storage = 64, 
                    Condition = "Orta",
                    PredictedPrice = 18500,
                    PredictedAt = DateTime.Now.AddDays(-7)
                }
            };

            return View(predictions);
        }

        /// <summary>
        /// Tahmin detayı
        /// </summary>
        [Authorize]
        public IActionResult Details(int id)
        {
            var prediction = new PredictionHistoryItem
            {
                Id = id,
                ModelName = "iPhone 14 Pro",
                Storage = 256,
                Ram = 6,
                Condition = "Mükemmel",
                PredictedPrice = 42500,
                PriceUsd = 1308,
                Confidence = 95.5m,
                PredictedAt = DateTime.Now.AddDays(-1)
            };

            // ViewData kullanımı (İster: ViewData)
            ViewData["PredictionId"] = id;
            ViewData["Title"] = $"Tahmin #{id} Detayı";

            return View(prediction);
        }

        /// <summary>
        /// Tahmin istatistikleri (Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        public IActionResult Statistics()
        {
            var stats = new PredictionStatsViewModel
            {
                TotalPredictions = 1250,
                TodayPredictions = 45,
                AverageConfidence = 92.5m,
                MostPredictedModel = "iPhone 14 Pro",
                AveragePredictedPrice = 35000
            };

            return View(stats);
        }

        /// <summary>
        /// Tahmin karşılaştırma
        /// </summary>
        public IActionResult Compare()
        {
            return View();
        }

        /// <summary>
        /// Toplu tahmin (Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Bulk()
        {
            return View();
        }

        /// <summary>
        /// Toplu tahmin işlemi
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult BulkProcess(List<PredictionRequest> requests)
        {
            TempData["Message"] = $"{requests?.Count ?? 0} tahmin işlendi.";
            return RedirectToAction("Bulk");
        }
    }
}

