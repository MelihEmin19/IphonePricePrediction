using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using System.Data;
using IphonePriceWeb.Models;
using IphonePriceWeb.Data;
using IphonePriceWeb.Data.Entities;

namespace IphonePriceWeb.Controllers
{
    /// <summary>
    /// Admin Panel Controller
    /// Segment, Model, Specs, Condition, User/Role yönetimi
    /// View'lar ve Stored Procedure'ler kullanılır
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _context;

        public AdminController(ILogger<AdminController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // =====================================================
        // DASHBOARD
        // =====================================================

        /// <summary>
        /// Admin Dashboard - v_dashboard_stats view'ını kullanır
        /// </summary>
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // v_dashboard_stats view'ından istatistikleri al
                var stats = await _context.DashboardStats.FirstOrDefaultAsync();

                // Eğer view yoksa tablolardan hesapla
                if (stats == null)
                {
                    stats = new DashboardStatsView
                    {
                        TotalUsers = await _context.Users.CountAsync(),
                        TotalModels = await _context.Models.CountAsync(),
                        TotalSpecs = await _context.Specs.CountAsync(),
                        TotalPredictions = await _context.Predictions.CountAsync(),
                        PredictionsToday = await _context.Predictions.CountAsync(p => p.CreatedAt.Date == DateTime.Today),
                        AvgPredictedPrice = await _context.Predictions.AverageAsync(p => (decimal?)p.PredictedPrice) ?? 0,
                        AvgConfidence = await _context.Predictions.AverageAsync(p => (decimal?)p.ConfidenceScore) ?? 0
                    };
                }

                // Model bazlı istatistikler - v_admin_model_stats
                ViewBag.ModelStats = await _context.AdminModelStats
                    .OrderByDescending(s => s.PredictionCount)
                    .Take(10)
                    .ToListAsync();

                // Durum bazlı istatistikler - v_admin_condition_stats
                ViewBag.ConditionStats = await _context.AdminConditionStats
                    .OrderByDescending(s => s.ConditionPuan)
                    .ToListAsync();

                return View(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard yüklenirken hata");
                TempData["ErrorMessage"] = "Dashboard yüklenirken hata oluştu.";
                return View(new DashboardStatsView());
            }
        }

        // =====================================================
        // SEGMENT YÖNETİMİ - sp_admin_add_segment SP kullanır
        // =====================================================

        /// <summary>
        /// Segment listesi - v_segments_catalog view'ını kullanır
        /// </summary>
        public async Task<IActionResult> Segments()
        {
            var segments = await _context.SegmentsCatalog.ToListAsync();
            return View(segments);
        }

        /// <summary>
        /// Segment ekleme formu
        /// </summary>
        [HttpGet]
        public IActionResult AddSegment()
        {
            return View(new SegmentFormModel());
        }

        /// <summary>
        /// Segment ekle - sp_admin_add_segment STORED PROCEDURE çağırır
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSegment(SegmentFormModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // SP çağrısı: CALL sp_admin_add_segment(@p_segment_name, @p_segment_id)
                var segmentIdParam = new NpgsqlParameter("p_segment_id", DbType.Int32)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = DBNull.Value
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "CALL sp_admin_add_segment(@p_segment_name, @p_segment_id)",
                    new NpgsqlParameter("p_segment_name", model.SegmentName),
                    segmentIdParam
                );

                var newId = segmentIdParam.Value != DBNull.Value ? (int)segmentIdParam.Value : 0;
                _logger.LogInformation($"SP: sp_admin_add_segment çağrıldı. Segment eklendi: {model.SegmentName}, ID: {newId}");
                TempData["SuccessMessage"] = $"Segment başarıyla eklendi. (ID: {newId})";
                return RedirectToAction("Segments");
            }
            catch (PostgresException ex) when (ex.Message.Contains("zaten mevcut"))
            {
                ModelState.AddModelError("SegmentName", "Bu segment zaten mevcut.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Segment eklenirken hata (SP)");
                ModelState.AddModelError("", $"Segment eklenirken hata oluştu: {ex.Message}");
                return View(model);
            }
        }

        // =====================================================
        // MODEL YÖNETİMİ - sp_admin_add_model, sp_admin_update_model SP kullanır
        // =====================================================

        /// <summary>
        /// Model listesi - v_models_catalog view'ını kullanır
        /// </summary>
        public async Task<IActionResult> Models()
        {
            var models = await _context.ModelsCatalog
                .OrderByDescending(m => m.ReleaseYear)
                .ThenBy(m => m.ModelName)
                .ToListAsync();
            return View(models);
        }

        /// <summary>
        /// Model ekleme formu
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AddModel()
        {
            ViewBag.Segments = new SelectList(
                await _context.Segments.OrderBy(s => s.SegmentPuan).ToListAsync(),
                "SegmentName",
                "SegmentName"
            );
            return View(new ModelFormModel());
        }

        /// <summary>
        /// Model ekle - sp_admin_add_model STORED PROCEDURE çağırır
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddModel(ModelFormModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Segments = new SelectList(
                    await _context.Segments.OrderBy(s => s.SegmentPuan).ToListAsync(),
                    "SegmentName",
                    "SegmentName"
                );
                return View(model);
            }

            try
            {
                // SP çağrısı: CALL sp_admin_add_model(@p_model_name, @p_segment_name, @p_release_year, @p_model_id)
                var modelIdParam = new NpgsqlParameter("p_model_id", DbType.Int32)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = DBNull.Value
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "CALL sp_admin_add_model(@p_model_name, @p_segment_name, @p_release_year, @p_model_id)",
                    new NpgsqlParameter("p_model_name", model.ModelName),
                    new NpgsqlParameter("p_segment_name", model.SegmentName),
                    new NpgsqlParameter("p_release_year", model.ReleaseYear),
                    modelIdParam
                );

                var newId = modelIdParam.Value != DBNull.Value ? (int)modelIdParam.Value : 0;
                _logger.LogInformation($"SP: sp_admin_add_model çağrıldı. Model eklendi: {model.ModelName}, ID: {newId}");
                TempData["SuccessMessage"] = $"Model başarıyla eklendi. (ID: {newId})";
                return RedirectToAction("Models");
            }
            catch (PostgresException ex)
            {
                _logger.LogError(ex, "Model eklenirken SP hatası");
                ModelState.AddModelError("", $"Model eklenirken hata: {ex.Message}");
                ViewBag.Segments = new SelectList(
                    await _context.Segments.OrderBy(s => s.SegmentPuan).ToListAsync(),
                    "SegmentName",
                    "SegmentName"
                );
                return View(model);
            }
        }

        /// <summary>
        /// Model düzenleme formu
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditModel(int id)
        {
            var model = await _context.Models
                .Include(m => m.Segment)
                .FirstOrDefaultAsync(m => m.ModelId == id);

            if (model == null)
            {
                return NotFound();
            }

            ViewBag.Segments = new SelectList(
                await _context.Segments.OrderBy(s => s.SegmentPuan).ToListAsync(),
                "SegmentName",
                "SegmentName",
                model.Segment?.SegmentName
            );

            return View(new ModelUpdateModel
            {
                ModelId = model.ModelId,
                ModelName = model.ModelName,
                SegmentName = model.Segment?.SegmentName ?? "",
                ReleaseYear = model.ReleaseYear
            });
        }

        /// <summary>
        /// Model güncelle - sp_admin_update_model STORED PROCEDURE çağırır
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModel(ModelUpdateModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Segments = new SelectList(
                    await _context.Segments.OrderBy(s => s.SegmentPuan).ToListAsync(),
                    "SegmentName",
                    "SegmentName"
                );
                return View(model);
            }

            try
            {
                // SP çağrısı: CALL sp_admin_update_model(@p_model_id, @p_model_name, @p_segment_name, @p_release_year, @p_success)
                var successParam = new NpgsqlParameter("p_success", DbType.Boolean)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = false
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "CALL sp_admin_update_model(@p_model_id, @p_model_name, @p_segment_name, @p_release_year, @p_success)",
                    new NpgsqlParameter("p_model_id", model.ModelId),
                    new NpgsqlParameter("p_model_name", model.ModelName),
                    new NpgsqlParameter("p_segment_name", model.SegmentName),
                    new NpgsqlParameter("p_release_year", model.ReleaseYear),
                    successParam
                );

                _logger.LogInformation($"SP: sp_admin_update_model çağrıldı. Model güncellendi: {model.ModelName}");
                TempData["SuccessMessage"] = "Model başarıyla güncellendi.";
                return RedirectToAction("Models");
            }
            catch (PostgresException ex)
            {
                _logger.LogError(ex, "Model güncellenirken SP hatası");
                ModelState.AddModelError("", $"Model güncellenirken hata: {ex.Message}");
                ViewBag.Segments = new SelectList(
                    await _context.Segments.OrderBy(s => s.SegmentPuan).ToListAsync(),
                    "SegmentName",
                    "SegmentName"
                );
                return View(model);
            }
        }

        // =====================================================
        // SPECS YÖNETİMİ - sp_admin_add_specs, sp_admin_update_specs SP kullanır
        // =====================================================

        /// <summary>
        /// Specs listesi - v_specs_catalog view'ını kullanır
        /// </summary>
        public async Task<IActionResult> Specs()
        {
            var specs = await _context.SpecsCatalog
                .OrderByDescending(s => s.ReleaseYear)
                .ThenBy(s => s.ModelName)
                .ThenBy(s => s.StorageGb)
                .ToListAsync();
            return View(specs);
        }

        /// <summary>
        /// Specs ekleme formu
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AddSpecs()
        {
            ViewBag.Models = new SelectList(
                await _context.Models
                    .Include(m => m.Segment)
                    .OrderByDescending(m => m.ReleaseYear)
                    .Select(m => new { m.ModelId, DisplayName = m.ModelName + " (" + m.Segment!.SegmentName + ")" })
                    .ToListAsync(),
                "ModelId",
                "DisplayName"
            );
            return View(new SpecsFormModel());
        }

        /// <summary>
        /// Specs ekle - sp_admin_add_specs STORED PROCEDURE çağırır
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSpecs(SpecsFormModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadSpecsViewBag();
                return View(model);
            }

            try
            {
                // SP çağrısı: CALL sp_admin_add_specs(@p_model_id, @p_ram_gb, @p_storage_gb, @p_kamera_mp, @p_ekran_boyutu, @p_batarya_mah, @p_specs_id)
                var specsIdParam = new NpgsqlParameter("p_specs_id", DbType.Int32)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = DBNull.Value
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "CALL sp_admin_add_specs(@p_model_id, @p_ram_gb, @p_storage_gb, @p_kamera_mp, @p_ekran_boyutu, @p_batarya_mah, @p_specs_id)",
                    new NpgsqlParameter("p_model_id", model.ModelId),
                    new NpgsqlParameter("p_ram_gb", model.RamGb),
                    new NpgsqlParameter("p_storage_gb", model.StorageGb),
                    new NpgsqlParameter("p_kamera_mp", model.KameraMp),
                    new NpgsqlParameter("p_ekran_boyutu", model.EkranBoyutu),
                    new NpgsqlParameter("p_batarya_mah", model.BataryaMah),
                    specsIdParam
                );

                var newId = specsIdParam.Value != DBNull.Value ? (int)specsIdParam.Value : 0;
                _logger.LogInformation($"SP: sp_admin_add_specs çağrıldı. Specs eklendi, ID: {newId}");
                TempData["SuccessMessage"] = $"Specs başarıyla eklendi. (ID: {newId})";
                return RedirectToAction("Specs");
            }
            catch (PostgresException ex)
            {
                _logger.LogError(ex, "Specs eklenirken SP hatası");
                ModelState.AddModelError("", $"Specs eklenirken hata: {ex.Message}");
                await LoadSpecsViewBag();
                return View(model);
            }
        }

        /// <summary>
        /// Specs düzenleme formu
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditSpecs(int id)
        {
            var spec = await _context.Specs.FindAsync(id);
            if (spec == null)
            {
                return NotFound();
            }

            await LoadSpecsViewBag(spec.ModelId);

            return View(new SpecsUpdateModel
            {
                SpecsId = spec.SpecsId,
                ModelId = spec.ModelId,
                RamGb = spec.RamGb,
                StorageGb = spec.StorageGb,
                KameraMp = spec.KameraMp,
                EkranBoyutu = spec.EkranBoyutu,
                BataryaMah = spec.BataryaMah
            });
        }

        /// <summary>
        /// Specs güncelle - sp_admin_update_specs STORED PROCEDURE çağırır
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSpecs(SpecsUpdateModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadSpecsViewBag(model.ModelId);
                return View(model);
            }

            try
            {
                // SP çağrısı: CALL sp_admin_update_specs(@p_specs_id, @p_ram_gb, @p_storage_gb, @p_kamera_mp, @p_ekran_boyutu, @p_batarya_mah, @p_success)
                var successParam = new NpgsqlParameter("p_success", DbType.Boolean)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = false
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "CALL sp_admin_update_specs(@p_specs_id, @p_ram_gb, @p_storage_gb, @p_kamera_mp, @p_ekran_boyutu, @p_batarya_mah, @p_success)",
                    new NpgsqlParameter("p_specs_id", model.SpecsId),
                    new NpgsqlParameter("p_ram_gb", model.RamGb),
                    new NpgsqlParameter("p_storage_gb", model.StorageGb),
                    new NpgsqlParameter("p_kamera_mp", model.KameraMp),
                    new NpgsqlParameter("p_ekran_boyutu", model.EkranBoyutu),
                    new NpgsqlParameter("p_batarya_mah", model.BataryaMah),
                    successParam
                );

                _logger.LogInformation($"SP: sp_admin_update_specs çağrıldı. Specs güncellendi, ID: {model.SpecsId}");
                TempData["SuccessMessage"] = "Specs başarıyla güncellendi.";
                return RedirectToAction("Specs");
            }
            catch (PostgresException ex)
            {
                _logger.LogError(ex, "Specs güncellenirken SP hatası");
                ModelState.AddModelError("", $"Specs güncellenirken hata: {ex.Message}");
                await LoadSpecsViewBag(model.ModelId);
                return View(model);
            }
        }

        private async Task LoadSpecsViewBag(int? selectedModelId = null)
        {
            var models = await _context.Models
                .Include(m => m.Segment)
                .OrderByDescending(m => m.ReleaseYear)
                .Select(m => new { m.ModelId, DisplayName = m.ModelName + " (" + m.Segment!.SegmentName + ")" })
                .ToListAsync();

            ViewBag.Models = new SelectList(models, "ModelId", "DisplayName", selectedModelId);
        }

        // =====================================================
        // CONDITION YÖNETİMİ - sp_admin_add_condition SP kullanır
        // =====================================================

        /// <summary>
        /// Condition listesi - v_condition_catalog view'ını kullanır
        /// </summary>
        public async Task<IActionResult> Conditions()
        {
            var conditions = await _context.ConditionCatalog
                .OrderByDescending(c => c.ConditionPuan)
                .ToListAsync();
            return View(conditions);
        }

        /// <summary>
        /// Condition ekleme formu
        /// </summary>
        [HttpGet]
        public IActionResult AddCondition()
        {
            return View(new ConditionFormModel());
        }

        /// <summary>
        /// Condition ekle - sp_admin_add_condition STORED PROCEDURE çağırır
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCondition(ConditionFormModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // SP çağrısı: CALL sp_admin_add_condition(@p_condition_name, @p_multiplier, @p_condition_id)
                var conditionIdParam = new NpgsqlParameter("p_condition_id", DbType.Int32)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = DBNull.Value
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "CALL sp_admin_add_condition(@p_condition_name, @p_multiplier, @p_condition_id)",
                    new NpgsqlParameter("p_condition_name", model.ConditionName),
                    new NpgsqlParameter("p_multiplier", model.Multiplier),
                    conditionIdParam
                );

                var newId = conditionIdParam.Value != DBNull.Value ? (int)conditionIdParam.Value : 0;
                _logger.LogInformation($"SP: sp_admin_add_condition çağrıldı. Durum eklendi: {model.ConditionName}, ID: {newId}");
                TempData["SuccessMessage"] = $"Durum başarıyla eklendi. (ID: {newId})";
                return RedirectToAction("Conditions");
            }
            catch (PostgresException ex) when (ex.Message.Contains("zaten mevcut"))
            {
                ModelState.AddModelError("ConditionName", "Bu durum zaten mevcut.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Condition eklenirken hata (SP)");
                ModelState.AddModelError("", $"Durum eklenirken hata oluştu: {ex.Message}");
                return View(model);
            }
        }

        // =====================================================
        // KULLANICI VE ROL YÖNETİMİ - sp_admin_assign_role SP kullanır
        // =====================================================

        /// <summary>
        /// Kullanıcı listesi - v_users_masked view'ını kullanır
        /// </summary>
        public async Task<IActionResult> Users()
        {
            var users = await _context.UsersMasked
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
            return View(users);
        }

        /// <summary>
        /// Rol atama formu
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AssignRole(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            ViewBag.UserName = user.Username;
            ViewBag.Roles = new SelectList(
                await _context.Roles.ToListAsync(),
                "RoleName",
                "RoleName"
            );

            return View(new RoleAssignModel { UserId = id });
        }

        /// <summary>
        /// Rol ata - sp_admin_assign_role STORED PROCEDURE çağırır
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(RoleAssignModel model)
        {
            if (!ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(model.UserId);
                ViewBag.UserName = user?.Username;
                ViewBag.Roles = new SelectList(await _context.Roles.ToListAsync(), "RoleName", "RoleName");
                return View(model);
            }

            try
            {
                // SP çağrısı: CALL sp_admin_assign_role(@p_user_id, @p_role_name, @p_success)
                var successParam = new NpgsqlParameter("p_success", DbType.Boolean)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = false
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "CALL sp_admin_assign_role(@p_user_id, @p_role_name, @p_success)",
                    new NpgsqlParameter("p_user_id", model.UserId),
                    new NpgsqlParameter("p_role_name", model.RoleName),
                    successParam
                );

                _logger.LogInformation($"SP: sp_admin_assign_role çağrıldı. Rol atandı: User={model.UserId}, Role={model.RoleName}");
                TempData["SuccessMessage"] = "Rol başarıyla atandı.";
                return RedirectToAction("Users");
            }
            catch (PostgresException ex)
            {
                _logger.LogError(ex, "Rol atanırken SP hatası");
                ModelState.AddModelError("", $"Rol atanırken hata: {ex.Message}");
                var user = await _context.Users.FindAsync(model.UserId);
                ViewBag.UserName = user?.Username;
                ViewBag.Roles = new SelectList(await _context.Roles.ToListAsync(), "RoleName", "RoleName");
                return View(model);
            }
        }

        /// <summary>
        /// Tahmin detayları - v_prediction_details view'ını kullanır
        /// </summary>
        public async Task<IActionResult> Predictions()
        {
            var predictions = await _context.Set<UserHistoryView>()
                .OrderByDescending(p => p.CreatedAt)
                .Take(100)
                .ToListAsync();
            return View(predictions);
        }
    }
}
