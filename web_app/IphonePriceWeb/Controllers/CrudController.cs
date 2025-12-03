using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using IphonePriceWeb.Data;
using IphonePriceWeb.Data.Entities;

namespace IphonePriceWeb.Controllers
{
    /// <summary>
    /// CRUD işlemleri Controller
    /// İster: Veritabanı bağlantısı ve temel CRUD işlemlerinin her biri kullanılmalı
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class CrudController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CrudController> _logger;

        public CrudController(ApplicationDbContext context, ILogger<CrudController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region LISTING CRUD

        /// <summary>
        /// READ - Tüm listingleri getir
        /// </summary>
        public async Task<IActionResult> Listings()
        {
            var listings = await _context.Listings
                .Include(l => l.Spec)
                    .ThenInclude(s => s!.Model)
                .OrderByDescending(l => l.ScrapedAt)
                .Take(100)
                .ToListAsync();

            return View(listings);
        }

        /// <summary>
        /// CREATE - Yeni listing formu
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CreateListing()
        {
            ViewBag.Specs = await _context.Specs
                .Include(s => s.Model)
                .ToListAsync();
            return View();
        }

        /// <summary>
        /// CREATE - Yeni listing kaydet
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateListing(Listing listing)
        {
            if (ModelState.IsValid)
            {
                listing.ScrapedAt = DateTime.UtcNow;
                listing.IsActive = true;
                
                _context.Listings.Add(listing);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Yeni listing oluşturuldu: ID={listing.Id}");
                TempData["SuccessMessage"] = "Listing başarıyla oluşturuldu!";
                
                return RedirectToAction(nameof(Listings));
            }

            ViewBag.Specs = await _context.Specs.Include(s => s.Model).ToListAsync();
            return View(listing);
        }

        /// <summary>
        /// UPDATE - Listing düzenle formu
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditListing(int id)
        {
            var listing = await _context.Listings.FindAsync(id);
            if (listing == null)
            {
                return NotFound();
            }

            ViewBag.Specs = await _context.Specs.Include(s => s.Model).ToListAsync();
            return View(listing);
        }

        /// <summary>
        /// UPDATE - Listing güncelle
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditListing(int id, Listing listing)
        {
            if (id != listing.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(listing);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Listing güncellendi: ID={listing.Id}");
                    TempData["SuccessMessage"] = "Listing başarıyla güncellendi!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Listings.AnyAsync(e => e.Id == listing.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Listings));
            }

            ViewBag.Specs = await _context.Specs.Include(s => s.Model).ToListAsync();
            return View(listing);
        }

        /// <summary>
        /// DELETE - Listing sil onay
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DeleteListing(int id)
        {
            var listing = await _context.Listings
                .Include(l => l.Spec)
                    .ThenInclude(s => s!.Model)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (listing == null)
            {
                return NotFound();
            }

            return View(listing);
        }

        /// <summary>
        /// DELETE - Listing sil işlemi
        /// </summary>
        [HttpPost, ActionName("DeleteListing")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteListingConfirmed(int id)
        {
            var listing = await _context.Listings.FindAsync(id);
            if (listing != null)
            {
                _context.Listings.Remove(listing);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Listing silindi: ID={id}");
                TempData["SuccessMessage"] = "Listing başarıyla silindi!";
            }

            return RedirectToAction(nameof(Listings));
        }

        #endregion

        #region USER CRUD

        /// <summary>
        /// READ - Kullanıcı listesi
        /// </summary>
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .OrderBy(u => u.Username)
                .ToListAsync();

            return View(users);
        }

        /// <summary>
        /// UPDATE - Kullanıcı rolü değiştir
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ToggleUserRole(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.Role = user.Role == "Admin" ? "User" : "Admin";
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Kullanıcı rolü değiştirildi: {user.Username} -> {user.Role}";
            }

            return RedirectToAction(nameof(Users));
        }

        #endregion

        #region STATISTICS

        /// <summary>
        /// READ - Veritabanı istatistikleri
        /// </summary>
        public async Task<IActionResult> Statistics()
        {
            var stats = new
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalBrands = await _context.Brands.CountAsync(),
                TotalModels = await _context.Models.CountAsync(),
                TotalSpecs = await _context.Specs.CountAsync(),
                TotalListings = await _context.Listings.CountAsync(),
                ActiveListings = await _context.Listings.CountAsync(l => l.IsActive),
                TotalPredictions = await _context.Predictions.CountAsync(),
                AvgPrice = await _context.Listings.Where(l => l.IsActive).AverageAsync(l => (double?)l.Price) ?? 0
            };

            return View(stats);
        }

        #endregion
    }
}

