using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using IphonePriceWeb.Services;
using IphonePriceWeb.Models;
using IphonePriceWeb.Data;

namespace IphonePriceWeb.Controllers
{
    /// <summary>
    /// Tahmin istatistikleri Controller
    /// v_admin_model_stats ve v_admin_condition_stats view'larını kullanır
    /// </summary>
    public class PredictionController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PredictionController> _logger;

        public PredictionController(ApiService apiService, ApplicationDbContext context, ILogger<PredictionController> logger)
        {
            _apiService = apiService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Tahmin istatistikleri (Admin) - GERÇEK VERİ
        /// v_dashboard_stats, v_admin_model_stats, v_admin_condition_stats view'larını kullanır
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Statistics()
        {
            try
            {
                // Gerçek veritabanı sorguları
                var totalPredictions = await _context.Predictions.CountAsync();
                var todayPredictions = await _context.Predictions
                    .CountAsync(p => p.CreatedAt.Date == DateTime.UtcNow.Date);
                
                // Ortalama güven skoru
                var avgConfidence = await _context.Predictions
                    .Where(p => p.ConfidenceScore.HasValue)
                    .AverageAsync(p => (double?)p.ConfidenceScore) ?? 0;
                
                // En çok tahmin yapılan model
                var mostPredictedSpecsId = await _context.Predictions
                    .GroupBy(p => p.SpecsId)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefaultAsync();
                
                string mostPredictedModel = "Henüz tahmin yok";
                if (mostPredictedSpecsId > 0)
                {
                    var spec = await _context.Specs
                        .Include(s => s.Model)
                        .FirstOrDefaultAsync(s => s.SpecsId == mostPredictedSpecsId);
                    mostPredictedModel = spec?.Model?.ModelName ?? "Bilinmiyor";
                }
                
                // Ortalama tahmin fiyatı
                var avgPredictedPrice = await _context.Predictions
                    .AverageAsync(p => (double?)p.PredictedPrice) ?? 0;

                var stats = new PredictionStatsViewModel
                {
                    TotalPredictions = totalPredictions,
                    TodayPredictions = todayPredictions,
                    AverageConfidence = (decimal)avgConfidence,
                    MostPredictedModel = mostPredictedModel,
                    AveragePredictedPrice = (decimal)avgPredictedPrice
                };

                // View'lardan ek istatistikler
                try
                {
                    ViewBag.ModelStats = await _context.AdminModelStats
                        .OrderByDescending(s => s.PredictionCount)
                        .Take(10)
                        .ToListAsync();

                    ViewBag.ConditionStats = await _context.AdminConditionStats
                        .OrderByDescending(s => s.ConditionPuan)
                        .ToListAsync();
                }
                catch
                {
                    ViewBag.ModelStats = new List<object>();
                    ViewBag.ConditionStats = new List<object>();
                }

                return View(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İstatistik yüklenirken hata");
                
                // Hata durumunda boş istatistik göster
                var stats = new PredictionStatsViewModel
                {
                    TotalPredictions = 0,
                    TodayPredictions = 0,
                    AverageConfidence = 0,
                    MostPredictedModel = "Veri yüklenemedi",
                    AveragePredictedPrice = 0
                };

                ViewBag.Error = "İstatistikler yüklenirken bir hata oluştu.";
                return View(stats);
            }
        }
    }
}
