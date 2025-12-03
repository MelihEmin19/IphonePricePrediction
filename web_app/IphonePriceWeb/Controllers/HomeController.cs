using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using IphonePriceWeb.Models;
using IphonePriceWeb.Services;

namespace IphonePriceWeb.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApiService _apiService;

    public HomeController(ILogger<HomeController> logger, ApiService apiService)
    {
        _logger = logger;
        _apiService = apiService;
    }

    /// <summary>
    /// Ana sayfa - Fiyat tahmin formu
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            // Modelleri API'den çek
            var models = await _apiService.GetModelsAsync();
            
            // Dropdown için listeyi hazırla
            ViewBag.Models = new SelectList(models, "Id", "Name");
            
            // Hafıza seçenekleri
            ViewBag.StorageOptions = new SelectList(new[] { 64, 128, 256, 512, 1024 });
            
            // RAM seçenekleri
            ViewBag.RamOptions = new SelectList(new[] { 4, 6, 8 });
            
            // Durum seçenekleri
            ViewBag.ConditionOptions = new SelectList(new[] 
            { 
                "Mükemmel", 
                "Çok İyi", 
                "İyi", 
                "Orta" 
            });

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ana sayfa yüklenirken hata");
            ViewBag.Error = "Veriler yüklenemedi. API servisi çalışıyor mu kontrol edin.";
            return View();
        }
    }

    /// <summary>
    /// Fiyat tahmini yap ve sonuç göster
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Predict(PredictionRequest request)
    {
        if (!ModelState.IsValid)
        {
            // Hata durumunda formu tekrar göster
            var models = await _apiService.GetModelsAsync();
            ViewBag.Models = new SelectList(models, "Id", "Name");
            ViewBag.StorageOptions = new SelectList(new[] { 64, 128, 256, 512, 1024 });
            ViewBag.RamOptions = new SelectList(new[] { 4, 6, 8 });
            ViewBag.ConditionOptions = new SelectList(new[] { "Mükemmel", "Çok İyi", "İyi", "Orta" });
            
            return View("Index", request);
        }

        try
        {
            // API'den tahmin al
            var result = await _apiService.PredictPriceAsync(request);
            
            if (result.Success)
            {
                // Başarılı - Result sayfasına yönlendir
                return View("Result", result);
            }
            else
            {
                ViewBag.Error = result.Error;
                return View("Index");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tahmin yapılırken hata");
            ViewBag.Error = "Tahmin yapılırken bir hata oluştu: " + ex.Message;
            return View("Index");
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
