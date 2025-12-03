using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using IphonePriceWeb.Models;

namespace IphonePriceWeb.Controllers
{
    /// <summary>
    /// Kullanıcı hesap işlemleri - Login/Logout/Register
    /// </summary>
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;

        public AccountController(ILogger<AccountController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Login sayfası
        /// </summary>
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// Login işlemi
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

            // Basit kullanıcı doğrulama (demo amaçlı)
            // Gerçek projede veritabanından kontrol edilmeli
            var (isValid, role) = ValidateUser(model.Username, model.Password);

            if (isValid)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, model.Username),
                    new Claim(ClaimTypes.Role, role),
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
            return View();
        }

        /// <summary>
        /// Kayıt işlemi
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Demo: Kayıt başarılı varsayılıyor
            // Gerçek projede veritabanına kaydedilmeli
            TempData["SuccessMessage"] = "Kayıt başarılı! Şimdi giriş yapabilirsiniz.";
            return RedirectToAction("Login");
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
        /// Basit kullanıcı doğrulama (demo)
        /// </summary>
        private (bool IsValid, string Role) ValidateUser(string username, string password)
        {
            // Demo kullanıcılar
            var users = new Dictionary<string, (string Password, string Role)>
            {
                { "admin", ("admin123", "Admin") },
                { "user", ("user123", "User") },
                { "testuser", ("test123", "User") }
            };

            if (users.TryGetValue(username.ToLower(), out var userData))
            {
                if (userData.Password == password)
                {
                    return (true, userData.Role);
                }
            }

            return (false, "");
        }
    }
}

