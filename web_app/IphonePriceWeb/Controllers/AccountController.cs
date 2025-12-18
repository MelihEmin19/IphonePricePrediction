using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Data;
using Npgsql;
using IphonePriceWeb.Models;
using IphonePriceWeb.Data;
using IphonePriceWeb.Data.Entities;

namespace IphonePriceWeb.Controllers
{
    /// <summary>
    /// Kullanıcı hesap işlemleri - Login/Logout/Register
    /// Yeni veritabanı yapısı: users + user_roles + roles
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
        /// Kullanıcının rollerini al (roles tablosundan)
        /// </summary>
        private async Task<string> GetUserRoleAsync(int userId)
        {
            // user_roles ve roles tablolarından kullanıcının rolünü al
            var userRole = await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId)
                .OrderBy(ur => ur.Role!.RoleName == "Admin" ? 0 : 1) // Admin öncelikli
                .FirstOrDefaultAsync();

            return userRole?.Role?.RoleName ?? "User";
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
        /// Login işlemi - Veritabanından kontrol (users + user_roles + roles)
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
                    return RedirectToAction("Dashboard", "Admin");
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
        /// Kayıt işlemi - sp_register_user STORED PROCEDURE çağırır
        /// Yeni kullanıcıya otomatik "User" rolü atanır
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // SP çağrısı: CALL sp_register_user(@p_username, @p_password_hash, @p_email, @p_user_id)
                var userIdParam = new NpgsqlParameter("p_user_id", DbType.Int32)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = DBNull.Value
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "CALL sp_register_user(@p_username, @p_password_hash, @p_email, @p_user_id)",
                    new NpgsqlParameter("p_username", model.Username),
                    new NpgsqlParameter("p_password_hash", HashPassword(model.Password)),
                    new NpgsqlParameter("p_email", (object?)model.Email ?? DBNull.Value),
                    userIdParam
                );

                var newUserId = userIdParam.Value != DBNull.Value ? (int)userIdParam.Value : 0;
                _logger.LogInformation($"SP: sp_register_user çağrıldı. Yeni kullanıcı kaydedildi: {model.Username}, ID: {newUserId}");
                TempData["SuccessMessage"] = "Kayıt başarılı! Şimdi giriş yapabilirsiniz.";
                return RedirectToAction("Login");
            }
            catch (PostgresException ex) when (ex.Message.Contains("zaten mevcut") || ex.Message.Contains("zaten kullanılıyor"))
            {
                ModelState.AddModelError("Username", "Bu kullanıcı adı veya e-posta zaten kullanılıyor.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı kaydı sırasında hata (SP)");
                ModelState.AddModelError(string.Empty, $"Kayıt sırasında bir hata oluştu: {ex.Message}");
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
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int userId = int.TryParse(userIdClaim, out var id) ? id : 0;

            // Kullanıcı tahmin sayısını al
            var predictionCount = await _context.Predictions
                .CountAsync(p => p.UserId == userId);

            var model = new UserProfileViewModel
            {
                Username = User.Identity.Name ?? "Bilinmiyor",
                Role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User",
                LoginTime = User.FindFirst("LoginTime")?.Value ?? "Bilinmiyor",
                PredictionCount = predictionCount
            };

            return View(model);
        }

        /// <summary>
        /// Veritabanından kullanıcı doğrulama
        /// users + user_roles + roles tablolarını kullanır
        /// </summary>
        private async Task<(bool IsValid, string Role, int UserId)> ValidateUserAsync(string username, string password)
        {
            // Veritabanından kullanıcıyı bul
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower() && u.IsActive);

            if (user != null)
            {
                // Şifre hash'ini kontrol et
                string hashedPassword = HashPassword(password);
                if (user.PasswordHash == hashedPassword)
                {
                    // Kullanıcının rolünü al (user_roles + roles)
                    string role = await GetUserRoleAsync(user.UserId);
                    _logger.LogInformation($"Veritabanından giriş: {username} (Rol: {role})");
                    return (true, role, user.UserId);
                }
            }

            // Admin için seed data fallback (ilk kurulumda veritabanında yoksa)
            if (username.ToLower() == "admin" && password == "admin123")
            {
                var adminUser = await EnsureAdminExistsAsync();
                return (true, "Admin", adminUser.UserId);
            }

            return (false, "", 0);
        }

        /// <summary>
        /// Admin kullanıcısının veritabanında var olduğundan emin ol
        /// Yoksa oluştur (seed data)
        /// </summary>
        private async Task<User> EnsureAdminExistsAsync()
        {
            var adminUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == "admin");

            if (adminUser == null)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Admin kullanıcısı oluştur
                    adminUser = new User
                    {
                        Username = "admin",
                        PasswordHash = HashPassword("admin123"),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Users.Add(adminUser);
                    await _context.SaveChangesAsync();

                    // Admin rolünü bul ve ata
                    var adminRole = await _context.Roles
                        .FirstOrDefaultAsync(r => r.RoleName == "Admin");

                    if (adminRole != null)
                    {
                        var userRole = new UserRole
                        {
                            UserId = adminUser.UserId,
                            RoleId = adminRole.RoleId,
                            AssignedAt = DateTime.UtcNow
                        };
                        _context.UserRoles.Add(userRole);
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                    _logger.LogInformation("Admin kullanıcısı seed data olarak oluşturuldu");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            return adminUser;
        }
    }
}
