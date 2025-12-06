using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using IphonePriceWeb.Models;
using IphonePriceWeb.Services;
using IphonePriceWeb.Data;

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
    /// Giriş yapmadan görüntülenebilir
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            // Modelleri API'den çek
            var models = await _apiService.GetModelsAsync();
            
            // Dropdown için listeyi hazırla
            ViewBag.Models = new SelectList(models, "Id", "Name");
            
            // Veritabanından dinamik RAM ve Storage seçenekleri
            var specs = await _context.Specs.ToListAsync();
            
            // Benzersiz RAM değerleri
            var ramOptions = specs.Select(s => s.RamGb).Distinct().OrderBy(r => r).ToList();
            ViewBag.RamOptions = new SelectList(ramOptions.Count > 0 ? ramOptions : new List<int> { 4, 6, 8 });
            
            // Benzersiz Storage değerleri
            var storageOptions = specs.Select(s => s.StorageGb).Distinct().OrderBy(s => s).ToList();
            ViewBag.StorageOptions = new SelectList(storageOptions.Count > 0 ? storageOptions : new List<int> { 64, 128, 256, 512, 1024 });
            
            // Durum seçenekleri (sabit kalabilir)
            ViewBag.ConditionOptions = new SelectList(new[] 
            { 
                "Mükemmel", 
                "Çok İyi", 
                "İyi", 
                "Orta" 
            });

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
            ViewBag.Error = "Veriler yüklenemedi. API servisi çalışıyor mu kontrol edin.";
            return View();
        }
    }

    /// <summary>
    /// Fiyat tahmini yap ve sonuç göster
    /// GİRİŞ ZORUNLU
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Predict(PredictionRequest request)
    {
        if (!ModelState.IsValid)
        {
            // Hata durumunda formu tekrar göster
            await LoadDropdownOptionsAsync();
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
                await LoadDropdownOptionsAsync();
                ViewBag.Error = result.Error;
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
    /// Dropdown seçeneklerini yükle (yardımcı metod)
    /// </summary>
    private async Task LoadDropdownOptionsAsync()
    {
        var models = await _apiService.GetModelsAsync();
        ViewBag.Models = new SelectList(models, "Id", "Name");
        
        var specs = await _context.Specs.ToListAsync();
        var ramOptions = specs.Select(s => s.RamGb).Distinct().OrderBy(r => r).ToList();
        ViewBag.RamOptions = new SelectList(ramOptions.Count > 0 ? ramOptions : new List<int> { 4, 6, 8 });
        
        var storageOptions = specs.Select(s => s.StorageGb).Distinct().OrderBy(s => s).ToList();
        ViewBag.StorageOptions = new SelectList(storageOptions.Count > 0 ? storageOptions : new List<int> { 64, 128, 256, 512, 1024 });
        
        ViewBag.ConditionOptions = new SelectList(new[] { "Mükemmel", "Çok İyi", "İyi", "Orta" });
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
