using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using IphonePriceWeb.Models;
using IphonePriceWeb.Services;
using IphonePriceWeb.Data;
using IphonePriceWeb.Data.Entities;

namespace IphonePriceWeb.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApiService _apiService;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApiService apiService, ApplicationDbContext context)
    {
        _logger = logger;
        _apiService = apiService;
        _context = context;
    }

    /// <summary>
    /// Ana sayfa - Fiyat tahmin formu
    /// Cascade dropdown: Model -> Storage -> SpecsId
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            // Model listesi (benzersiz modeller)
            var models = await _context.Models
                .Include(m => m.Segment)
                .OrderByDescending(m => m.ReleaseYear)
                .ThenByDescending(m => m.Segment!.SegmentPuan)
                .ToListAsync();

            ViewBag.ModelOptions = models.Select(m => new
            {
                m.ModelId,
                DisplayName = $"{m.ModelName} - {m.ReleaseYear}"
            }).ToList();

            // Specs listesi (JavaScript için JSON)
            var specs = await _context.Specs
                .Include(s => s.Model)
                .OrderBy(s => s.StorageGb)
                .ToListAsync();

            var specsJson = specs.Select(s => new
            {
                specsId = s.SpecsId,
                modelId = s.ModelId,
                storageGb = s.StorageGb,
                ramGb = s.RamGb
            }).ToList();

            ViewBag.SpecsJson = JsonSerializer.Serialize(specsJson);

            // Condition listesi
            var conditions = await _context.Conditions
                .OrderByDescending(c => c.ConditionPuan)
                .ToListAsync();

            ViewBag.ConditionOptions = new SelectList(
                conditions.Select(c => new { c.ConditionId, c.ConditionName }),
                "ConditionId",
                "ConditionName"
            );

            // Giriş yapılmamışsa uyarı göster
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                ViewBag.LoginWarning = "Fiyat tahmini yapmak için giriş yapmanız gerekmektedir.";
            }

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ana sayfa yüklenirken hata");
            
            // Fallback
            await LoadDropdownOptionsFromTablesAsync();
            
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                ViewBag.LoginWarning = "Fiyat tahmini yapmak için giriş yapmanız gerekmektedir.";
            }
            
            return View();
        }
    }

    /// <summary>
    /// Fiyat tahmini yap ve sonuç göster
    /// sp_create_prediction SP'sini çağırır
    /// GİRİŞ ZORUNLU
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Predict(PredictionRequest request)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownOptionsAsync();
            return View("Index", request);
        }

        try
        {
            // Seçilen specs bilgilerini al
            var spec = await _context.Specs
                .Include(s => s.Model)
                .ThenInclude(m => m!.Segment)
                .FirstOrDefaultAsync(s => s.SpecsId == request.SpecsId);

            if (spec == null)
            {
                ViewBag.Error = "Seçilen cihaz bulunamadı.";
                await LoadDropdownOptionsAsync();
                return View("Index");
            }

            // Seçilen condition bilgisini al
            var condition = await _context.Conditions
                .FirstOrDefaultAsync(c => c.ConditionId == request.ConditionId);

            if (condition == null)
            {
                ViewBag.Error = "Seçilen durum bulunamadı.";
                await LoadDropdownOptionsAsync();
                return View("Index");
            }

            // Eski API formatına çevir (backward compatibility)
            var legacyRequest = new LegacyPredictionRequest
            {
                ModelId = spec.ModelId,
                RamGb = spec.RamGb,
                StorageGb = spec.StorageGb,
                Condition = condition.ConditionName,
                ReleaseYear = spec.Model?.ReleaseYear ?? 2020
            };

            // API'den tahmin al
            var result = await _apiService.PredictPriceAsync(legacyRequest);
            
            if (result.Success && result.Data?.Prediction != null)
            {
                // Kullanıcı ID'sini al
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int userId = int.TryParse(userIdClaim, out var id) ? id : 0;

                // Tahmini veritabanına kaydet
                // SP varsa CALL sp_create_prediction kullanılır, yoksa EF Core
                if (userId > 0)
                {
                    try
                    {
                        // Önce SP ile dene
                        var predictionIdParam = new NpgsqlParameter("p_prediction_id", DbType.Int32)
                        {
                            Direction = ParameterDirection.InputOutput,
                            Value = DBNull.Value
                        };

                        await _context.Database.ExecuteSqlRawAsync(
                            "CALL sp_create_prediction(@p_user_id, @p_specs_id, @p_condition_id, @p_predicted_price, @p_confidence_score, @p_prediction_id)",
                            new NpgsqlParameter("p_user_id", userId),
                            new NpgsqlParameter("p_specs_id", request.SpecsId),
                            new NpgsqlParameter("p_condition_id", request.ConditionId),
                            new NpgsqlParameter("p_predicted_price", (decimal)result.Data.Prediction.PriceTl),
                            new NpgsqlParameter("p_confidence_score", (decimal)result.Data.Prediction.Confidence),
                            predictionIdParam
                        );

                        var newPredictionId = predictionIdParam.Value != DBNull.Value ? (int)predictionIdParam.Value : 0;
                        _logger.LogInformation($"SP: sp_create_prediction çağrıldı. ID={newPredictionId}");
                    }
                    catch (Exception spEx)
                    {
                        // SP yoksa EF Core ile kaydet (fallback)
                        _logger.LogWarning(spEx, "SP bulunamadı, EF Core kullanılıyor");
                        
                        var prediction = new Prediction
                        {
                            UserId = userId,
                            SpecsId = request.SpecsId,
                            ConditionId = request.ConditionId,
                            PredictedPrice = (decimal)result.Data.Prediction.PriceTl,
                            ConfidenceScore = (decimal)result.Data.Prediction.Confidence,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Predictions.Add(prediction);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"EF Core: Tahmin kaydedildi. ID={prediction.PredictionId}");
                    }
                }

                // Sonuç view modeli
                ViewBag.SpecsLabel = $"{spec.Model?.ModelName} {spec.StorageGb}GB {spec.RamGb}GB RAM";
                ViewBag.ConditionName = condition.ConditionName;

                return View("Result", result);
            }
            else
            {
                await LoadDropdownOptionsAsync();
                ViewBag.Error = result.Error ?? "Tahmin yapılamadı.";
                return View("Index");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tahmin yapılırken hata");
            await LoadDropdownOptionsAsync();
            ViewBag.Error = "Tahmin yapılırken bir hata oluştu: " + ex.Message;
            return View("Index");
        }
    }

    /// <summary>
    /// Dropdown seçeneklerini yükle (cascade dropdown için)
    /// </summary>
    private async Task LoadDropdownOptionsAsync()
    {
        try
        {
            // Model listesi
            var models = await _context.Models
                .Include(m => m.Segment)
                .OrderByDescending(m => m.ReleaseYear)
                .ThenByDescending(m => m.Segment!.SegmentPuan)
                .ToListAsync();

            ViewBag.ModelOptions = models.Select(m => new
            {
                m.ModelId,
                DisplayName = $"{m.ModelName} - {m.ReleaseYear}"
            }).ToList();

            // Specs JSON
            var specs = await _context.Specs
                .Include(s => s.Model)
                .OrderBy(s => s.StorageGb)
                .ToListAsync();

            var specsJson = specs.Select(s => new
            {
                specsId = s.SpecsId,
                modelId = s.ModelId,
                storageGb = s.StorageGb,
                ramGb = s.RamGb
            }).ToList();

            ViewBag.SpecsJson = JsonSerializer.Serialize(specsJson);

            // Condition listesi
            var conditions = await _context.Conditions
                .OrderByDescending(c => c.ConditionPuan)
                .ToListAsync();

            ViewBag.ConditionOptions = new SelectList(
                conditions.Select(c => new { c.ConditionId, c.ConditionName }),
                "ConditionId",
                "ConditionName"
            );
        }
        catch
        {
            await LoadDropdownOptionsFromTablesAsync();
        }
    }

    /// <summary>
    /// Tablolardan dropdown seçeneklerini yükle (fallback)
    /// </summary>
    private async Task LoadDropdownOptionsFromTablesAsync()
    {
        try
        {
            // Model listesi
            var models = await _context.Models
                .Include(m => m.Segment)
                .OrderByDescending(m => m.ReleaseYear)
                .ToListAsync();

            ViewBag.ModelOptions = models.Select(m => new
            {
                m.ModelId,
                DisplayName = $"{m.ModelName} ({m.Segment?.SegmentName ?? "Base"}) - {m.ReleaseYear}"
            }).ToList();

            // Specs JSON
            var specs = await _context.Specs
                .OrderBy(s => s.StorageGb)
                .ToListAsync();

            var specsJson = specs.Select(s => new
            {
                specsId = s.SpecsId,
                modelId = s.ModelId,
                storageGb = s.StorageGb,
                ramGb = s.RamGb
            }).ToList();

            ViewBag.SpecsJson = JsonSerializer.Serialize(specsJson);

            // Conditions
            var conditions = await _context.Conditions
                .OrderByDescending(c => c.ConditionPuan)
                .ToListAsync();

            ViewBag.ConditionOptions = new SelectList(
                conditions.Select(c => new { c.ConditionId, c.ConditionName }),
                "ConditionId",
                "ConditionName"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dropdown yüklenemedi");
            
            // En son fallback - boş liste
            ViewBag.ModelOptions = new List<object>();
            ViewBag.SpecsJson = "[]";
            ViewBag.ConditionOptions = new SelectList(new List<object>());
        }
    }

    /// <summary>
    /// Hakkında sayfası
    /// </summary>
    public IActionResult About()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
