using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Npgsql;
using IphonePriceWeb.Models;
using IphonePriceWeb.Data;
using IphonePriceWeb.Data.Entities;

namespace IphonePriceWeb.Controllers
{
    /// <summary>
    /// Kullanıcı Tahmin Geçmişi Controller
    /// v_user_history_masked view'ını kullanır
    /// </summary>
    [Authorize]
    public class HistoryController : Controller
    {
        private readonly ILogger<HistoryController> _logger;
        private readonly ApplicationDbContext _context;

        public HistoryController(ILogger<HistoryController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Kullanıcının tahmin geçmişi - v_user_history_masked view'ını kullanır
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int userId = int.TryParse(userIdClaim, out var id) ? id : 0;

                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                // v_user_history_masked view'ından kullanıcının geçmişini al
                var history = await _context.UserHistory
                    .Where(h => h.UserId == userId)
                    .OrderByDescending(h => h.CreatedAt)
                    .ToListAsync();

                // Model'e dönüştür
                var historyItems = history.Select(h => new PredictionHistoryItem
                {
                    PredictionId = h.PredictionId,
                    SpecsLabel = h.SpecsLabel,
                    ModelName = h.ModelName,
                    StorageGb = h.StorageGb,
                    RamGb = h.RamGb,
                    ConditionName = h.ConditionName,
                    PredictedPrice = h.PredictedPrice,
                    FormattedPrice = h.FormattedPrice,
                    ConfidenceScore = h.ConfidenceScore,
                    CreatedAt = h.CreatedAt
                }).ToList();

                // Özet istatistikler
                if (historyItems.Any())
                {
                    ViewBag.TotalPredictions = historyItems.Count;
                    ViewBag.AveragePrice = historyItems.Average(h => h.PredictedPrice);
                    ViewBag.AverageConfidence = historyItems.Where(h => h.ConfidenceScore.HasValue).Average(h => h.ConfidenceScore) ?? 0;
                    ViewBag.MostPredictedModel = historyItems
                        .GroupBy(h => h.ModelName)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key ?? "Yok";
                }
                else
                {
                    ViewBag.TotalPredictions = 0;
                    ViewBag.AveragePrice = 0;
                    ViewBag.AverageConfidence = 0;
                    ViewBag.MostPredictedModel = "Yok";
                }

                return View(historyItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tahmin geçmişi yüklenirken hata");
                
                // Fallback: Doğrudan predictions tablosundan çek
                return await GetHistoryFromTables();
            }
        }

        /// <summary>
        /// Tablolardan tahmin geçmişini al (View yoksa fallback)
        /// </summary>
        private async Task<IActionResult> GetHistoryFromTables()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int userId = int.TryParse(userIdClaim, out var id) ? id : 0;

            var predictions = await _context.Predictions
                .Include(p => p.Spec)
                .ThenInclude(s => s!.Model)
                .Include(p => p.Condition)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var historyItems = predictions.Select(p => new PredictionHistoryItem
            {
                PredictionId = p.PredictionId,
                SpecsLabel = $"{p.Spec?.Model?.ModelName ?? "Bilinmeyen"} {p.Spec?.StorageGb}GB {p.Spec?.RamGb}GB RAM",
                ModelName = p.Spec?.Model?.ModelName ?? "Bilinmeyen",
                StorageGb = p.Spec?.StorageGb ?? 0,
                RamGb = p.Spec?.RamGb ?? 0,
                ConditionName = p.Condition?.ConditionName ?? "Bilinmeyen",
                PredictedPrice = p.PredictedPrice,
                FormattedPrice = $"₺{p.PredictedPrice:N0}",
                ConfidenceScore = p.ConfidenceScore,
                CreatedAt = p.CreatedAt
            }).ToList();

            // Özet istatistikler
            if (historyItems.Any())
            {
                ViewBag.TotalPredictions = historyItems.Count;
                ViewBag.AveragePrice = historyItems.Average(h => h.PredictedPrice);
                ViewBag.AverageConfidence = historyItems.Where(h => h.ConfidenceScore.HasValue).Average(h => h.ConfidenceScore) ?? 0;
                ViewBag.MostPredictedModel = historyItems
                    .GroupBy(h => h.ModelName)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? "Yok";
            }
            else
            {
                ViewBag.TotalPredictions = 0;
                ViewBag.AveragePrice = 0;
                ViewBag.AverageConfidence = 0;
                ViewBag.MostPredictedModel = "Yok";
            }

            return View(historyItems);
        }

        /// <summary>
        /// Tahmin detayı
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int userId = int.TryParse(userIdClaim, out var uid) ? uid : 0;

            var prediction = await _context.Predictions
                .Include(p => p.Spec)
                .ThenInclude(s => s!.Model)
                .ThenInclude(m => m!.Segment)
                .Include(p => p.Condition)
                .FirstOrDefaultAsync(p => p.PredictionId == id && p.UserId == userId);

            if (prediction == null)
            {
                return NotFound();
            }

            // Fonksiyon çağrıları ile ek bilgiler al
            if (prediction.SpecsId > 0)
            {
                // fn_specs_label fonksiyonu çağrısı
                ViewBag.SpecsLabel = await GetSpecsLabelAsync(prediction.SpecsId);
            }

            if (prediction.PredictedPrice > 0)
            {
                // fn_format_price fonksiyonu çağrısı
                ViewBag.FormattedPrice = await GetFormattedPriceAsync(prediction.PredictedPrice);
            }

            return View(prediction);
        }

        // =====================================================
        // FONKSİYON ÇAĞRILARI - SELECT ile doğrudan çağrı
        // =====================================================

        /// <summary>
        /// fn_specs_label fonksiyonunu çağırır
        /// </summary>
        private async Task<string> GetSpecsLabelAsync(int specsId)
        {
            try
            {
                // SELECT fn_specs_label(@p_specs_id)
                var result = await _context.Database
                    .SqlQueryRaw<string>("SELECT fn_specs_label(@p_specs_id)", 
                        new NpgsqlParameter("p_specs_id", specsId))
                    .FirstOrDefaultAsync();
                
                return result ?? "Bilinmeyen Model";
            }
            catch
            {
                return "Bilinmeyen Model";
            }
        }

        /// <summary>
        /// fn_format_price fonksiyonunu çağırır
        /// </summary>
        private async Task<string> GetFormattedPriceAsync(decimal price)
        {
            try
            {
                // SELECT fn_format_price(@p_price)
                var result = await _context.Database
                    .SqlQueryRaw<string>("SELECT fn_format_price(@p_price)", 
                        new NpgsqlParameter("p_price", price))
                    .FirstOrDefaultAsync();
                
                return result ?? $"₺{price:N0}";
            }
            catch
            {
                return $"₺{price:N0}";
            }
        }

        /// <summary>
        /// fn_get_prediction_count fonksiyonunu çağırır
        /// </summary>
        private async Task<int> GetPredictionCountAsync(int userId)
        {
            try
            {
                // SELECT fn_get_prediction_count(@p_user_id)
                var result = await _context.Database
                    .SqlQueryRaw<int>("SELECT fn_get_prediction_count(@p_user_id)", 
                        new NpgsqlParameter("p_user_id", userId))
                    .FirstOrDefaultAsync();
                
                return result;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// fn_is_admin fonksiyonunu çağırır
        /// </summary>
        private async Task<bool> IsUserAdminAsync(int userId)
        {
            try
            {
                // SELECT fn_is_admin(@p_user_id)
                var result = await _context.Database
                    .SqlQueryRaw<bool>("SELECT fn_is_admin(@p_user_id)", 
                        new NpgsqlParameter("p_user_id", userId))
                    .FirstOrDefaultAsync();
                
                return result;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// fn_calculate_model_age fonksiyonunu çağırır
        /// </summary>
        private async Task<int> GetModelAgeAsync(int modelId)
        {
            try
            {
                // SELECT fn_calculate_model_age(@p_model_id)
                var result = await _context.Database
                    .SqlQueryRaw<int>("SELECT fn_calculate_model_age(@p_model_id)", 
                        new NpgsqlParameter("p_model_id", modelId))
                    .FirstOrDefaultAsync();
                
                return result;
            }
            catch
            {
                return 0;
            }
        }
    }
}

