using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IphonePriceWeb.Models;
using IphonePriceWeb.Data;
using IphonePriceWeb.Data.Entities;

namespace IphonePriceWeb.Controllers
{
    /// <summary>
    /// Kullanıcı hesap işlemleri - Login/Logout/Register
    /// Gerçek veritabanı entegrasyonu ile
    /// </summary>
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;

        public AccountController(ILogger<AccountController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// SHA256 ile şifre hash'leme
        /// </summary>
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// Login sayfası
        /// </summary>
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Zaten giriş yapmışsa ana sayfaya yönlendir
            if (User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Index", "Home");
            }
            
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// Login işlemi - Veritabanından kontrol
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Veritabanından kullanıcı doğrulama
            var (isValid, role, userId) = await ValidateUserAsync(model.Username, model.Password);

            if (isValid)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, model.Username),
                    new Claim(ClaimTypes.Role, role),
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim("LoginTime", DateTime.Now.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation($"Kullanıcı giriş yaptı: {model.Username} ({role})");
                
                TempData["SuccessMessage"] = $"Hoş geldiniz, {model.Username}!";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Admin ise Admin paneline yönlendir
                if (role == "Admin")
                {
                    return RedirectToAction("Panel", "Admin");
                }

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya şifre");
            return View(model);
        }

        /// <summary>
        /// Logout işlemi
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "Başarıyla çıkış yaptınız.";
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Kayıt sayfası
        /// </summary>
        [HttpGet]
        public IActionResult Register()
        {
            // Zaten giriş yapmışsa ana sayfaya yönlendir
            if (User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Index", "Home");
            }
            
            return View();
        }

        /// <summary>
        /// Kayıt işlemi - Veritabanına kaydeder
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kullanıcı adı kontrolü
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == model.Username.ToLower());

            if (existingUser != null)
            {
                ModelState.AddModelError("Username", "Bu kullanıcı adı zaten kullanılıyor.");
                return View(model);
            }

            // Yeni kullanıcı oluştur
            var newUser = new User
            {
                Username = model.Username,
                PasswordHash = HashPassword(model.Password),
                Role = "User", // Yeni kayıtlar sadece User rolünde
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Yeni kullanıcı kaydedildi: {model.Username}");
                TempData["SuccessMessage"] = "Kayıt başarılı! Şimdi giriş yapabilirsiniz.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı kaydı sırasında hata");
                ModelState.AddModelError(string.Empty, "Kayıt sırasında bir hata oluştu. Lütfen tekrar deneyin.");
                return View(model);
            }
        }

        /// <summary>
        /// Erişim reddedildi sayfası
        /// </summary>
        public IActionResult AccessDenied()
        {
            return View();
        }

        /// <summary>
        /// Kullanıcı profili
        /// </summary>
        public IActionResult Profile()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login");
            }

            var model = new UserProfileViewModel
            {
                Username = User.Identity.Name ?? "Bilinmiyor",
                Role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User",
                LoginTime = User.FindFirst("LoginTime")?.Value ?? "Bilinmiyor"
            };

            return View(model);
        }

        /// <summary>
        /// Veritabanından kullanıcı doğrulama
        /// Admin için seed data fallback var
        /// </summary>
        private async Task<(bool IsValid, string Role, int UserId)> ValidateUserAsync(string username, string password)
        {
            // Önce veritabanından kontrol et
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

            if (user != null)
            {
                // Şifre hash'ini kontrol et
                string hashedPassword = HashPassword(password);
                if (user.PasswordHash == hashedPassword)
                {
                    _logger.LogInformation($"Veritabanından giriş: {username}");
                    return (true, user.Role, user.Id);
                }
            }

            // Admin için seed data fallback (ilk kurulumda veritabanında yoksa)
            if (username.ToLower() == "admin" && password == "admin123")
            {
                // Admin yoksa veritabanına ekle
                var adminUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == "admin");
                
                if (adminUser == null)
                {
                    adminUser = new User
                    {
                        Username = "admin",
                        PasswordHash = HashPassword("admin123"),
                        Role = "Admin",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Users.Add(adminUser);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Admin kullanıcısı seed data olarak oluşturuldu");
                }

                return (true, "Admin", adminUser.Id);
            }

            return (false, "", 0);
        }
    }
}
