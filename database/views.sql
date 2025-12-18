-- =====================================================
-- VIEWS (GÖRÜNÜMLER)
-- İster: En az 10 view
-- UI Tasarımına Uygun View'lar
-- =====================================================

-- =====================================================
-- 1. v_specs_catalog: Specs kataloğu (Dropdown için)
-- Kullanım: User "Fiyat Tahmin" ekranında specs seçimi
-- =====================================================
CREATE OR REPLACE VIEW v_specs_catalog AS
SELECT 
    s.specs_id,
    s.model_id,
    m.model_name,
    seg.segment_name,
    seg.segment_puan,
    m.release_year,
    s.ram_gb,
    s.storage_gb,
    s.kamera_mp,
    s.ekran_boyutu,
    s.batarya_mah,
    fn_specs_label(s.specs_id) AS label
FROM specs s
JOIN models m ON s.model_id = m.model_id
JOIN segments seg ON m.segment_id = seg.segment_id
ORDER BY m.release_year DESC, seg.segment_puan DESC, s.storage_gb;

COMMENT ON VIEW v_specs_catalog IS 'Specs kataloğu - Dropdown menüler için okunabilir etiketlerle';

-- =====================================================
-- 2. v_condition_catalog: Cihaz durumları kataloğu
-- Kullanım: User "Fiyat Tahmin" ekranında durum seçimi
-- =====================================================
CREATE OR REPLACE VIEW v_condition_catalog AS
SELECT 
    condition_id,
    condition_name,
    condition_puan,
    multiplier
FROM conditions
ORDER BY condition_puan DESC;

COMMENT ON VIEW v_condition_catalog IS 'Cihaz durumları kataloğu - Dropdown için';

-- =====================================================
-- 3. v_segments_catalog: Segment kataloğu
-- Kullanım: Admin "Segment Yönetimi" ekranı
-- =====================================================
CREATE OR REPLACE VIEW v_segments_catalog AS
SELECT 
    segment_id,
    segment_name,
    segment_puan,
    (SELECT COUNT(*) FROM models m WHERE m.segment_id = s.segment_id) AS model_count,
    created_at
FROM segments s
ORDER BY segment_puan;

COMMENT ON VIEW v_segments_catalog IS 'Segment kataloğu - Admin yönetim paneli için';

-- =====================================================
-- 4. v_models_catalog: Model kataloğu
-- Kullanım: Admin "Model Yönetimi" ekranı
-- =====================================================
CREATE OR REPLACE VIEW v_models_catalog AS
SELECT 
    m.model_id,
    m.model_name,
    m.segment_id,
    seg.segment_name,
    m.release_year,
    m.model_kodu,
    (SELECT COUNT(*) FROM specs s WHERE s.model_id = m.model_id) AS specs_count,
    m.created_at
FROM models m
JOIN segments seg ON m.segment_id = seg.segment_id
ORDER BY m.release_year DESC, seg.segment_puan DESC;

COMMENT ON VIEW v_models_catalog IS 'Model kataloğu - Admin yönetim paneli için';

-- =====================================================
-- 5. v_user_history_masked: Kullanıcı tahmin geçmişi (maskelenmiş)
-- Kullanım: User "Tahmin Geçmişim" ekranı
-- =====================================================
CREATE OR REPLACE VIEW v_user_history_masked AS
SELECT 
    p.prediction_id,
    p.user_id,
    fn_mask_username(u.username) AS username_masked,
    fn_specs_label(p.specs_id) AS specs_label,
    m.model_name,
    s.storage_gb,
    s.ram_gb,
    c.condition_name,
    p.predicted_price,
    fn_format_price(p.predicted_price) AS formatted_price,
    p.confidence_score,
    p.created_at
FROM predictions p
JOIN users u ON p.user_id = u.user_id
JOIN specs s ON p.specs_id = s.specs_id
JOIN models m ON s.model_id = m.model_id
JOIN conditions c ON p.condition_id = c.condition_id
ORDER BY p.created_at DESC;

COMMENT ON VIEW v_user_history_masked IS 'Kullanıcı tahmin geçmişi - Maskelenmiş kullanıcı adlarıyla';

-- =====================================================
-- 6. v_admin_model_stats: Model bazlı istatistikler
-- Kullanım: Admin Dashboard raporları
-- =====================================================
CREATE OR REPLACE VIEW v_admin_model_stats AS
SELECT 
    m.model_id,
    m.model_name,
    seg.segment_name,
    m.release_year,
    COUNT(DISTINCT s.specs_id) AS specs_count,
    COUNT(p.prediction_id) AS prediction_count,
    COALESCE(AVG(p.predicted_price), 0) AS avg_predicted_price,
    COALESCE(MIN(p.predicted_price), 0) AS min_predicted_price,
    COALESCE(MAX(p.predicted_price), 0) AS max_predicted_price,
    COALESCE(AVG(p.confidence_score), 0) AS avg_confidence
FROM models m
JOIN segments seg ON m.segment_id = seg.segment_id
LEFT JOIN specs s ON m.model_id = s.model_id
LEFT JOIN predictions p ON s.specs_id = p.specs_id
GROUP BY m.model_id, m.model_name, seg.segment_name, m.release_year
ORDER BY prediction_count DESC, m.release_year DESC;

COMMENT ON VIEW v_admin_model_stats IS 'Model bazlı tahmin istatistikleri - Admin raporları için';

-- =====================================================
-- 7. v_admin_condition_stats: Durum bazlı istatistikler
-- Kullanım: Admin Dashboard raporları
-- =====================================================
CREATE OR REPLACE VIEW v_admin_condition_stats AS
SELECT 
    c.condition_id,
    c.condition_name,
    c.condition_puan,
    COUNT(p.prediction_id) AS prediction_count,
    COALESCE(AVG(p.predicted_price), 0) AS avg_predicted_price,
    COALESCE(MIN(p.predicted_price), 0) AS min_predicted_price,
    COALESCE(MAX(p.predicted_price), 0) AS max_predicted_price
FROM conditions c
LEFT JOIN predictions p ON c.condition_id = p.condition_id
GROUP BY c.condition_id, c.condition_name, c.condition_puan
ORDER BY c.condition_puan DESC;

COMMENT ON VIEW v_admin_condition_stats IS 'Durum bazlı tahmin istatistikleri - Admin raporları için';

-- =====================================================
-- 8. v_users_masked: Maskelenmiş kullanıcı listesi
-- Kullanım: Admin "Kullanıcı Yönetimi" ekranı
-- =====================================================
CREATE OR REPLACE VIEW v_users_masked AS
SELECT 
    u.user_id,
    u.username,
    fn_mask_username(u.username) AS username_masked,
    fn_mask_email(u.email) AS email_masked,
    fn_get_user_role(u.user_id) AS role_name,
    fn_get_prediction_count(u.user_id) AS prediction_count,
    u.is_active,
    u.created_at,
    u.updated_at
FROM users u
ORDER BY u.created_at DESC;

COMMENT ON VIEW v_users_masked IS 'Maskelenmiş kullanıcı listesi - Admin paneli için';

-- =====================================================
-- 9. v_dashboard_stats: Admin Dashboard özet istatistikleri
-- Kullanım: Admin Dashboard kartları
-- =====================================================
CREATE OR REPLACE VIEW v_dashboard_stats AS
SELECT 
    (SELECT COUNT(*) FROM users WHERE is_active = TRUE) AS total_users,
    (SELECT COUNT(*) FROM users u JOIN user_roles ur ON u.user_id = ur.user_id JOIN roles r ON ur.role_id = r.role_id WHERE r.role_name = 'Admin') AS admin_count,
    (SELECT COUNT(*) FROM models) AS total_models,
    (SELECT COUNT(*) FROM specs) AS total_specs,
    (SELECT COUNT(*) FROM predictions) AS total_predictions,
    (SELECT COUNT(*) FROM predictions WHERE created_at > CURRENT_TIMESTAMP - INTERVAL '24 hours') AS predictions_today,
    (SELECT COALESCE(AVG(predicted_price), 0) FROM predictions) AS avg_predicted_price,
    (SELECT COALESCE(AVG(confidence_score), 0) FROM predictions) AS avg_confidence,
    (SELECT m.model_name FROM models m 
     JOIN specs s ON m.model_id = s.model_id 
     JOIN predictions p ON s.specs_id = p.specs_id 
     GROUP BY m.model_id, m.model_name 
     ORDER BY COUNT(*) DESC LIMIT 1) AS most_predicted_model;

COMMENT ON VIEW v_dashboard_stats IS 'Admin Dashboard için özet istatistikler';

-- =====================================================
-- 10. v_user_roles_detail: Kullanıcı-rol detay görünümü
-- Kullanım: Admin "Rol Yönetimi" ekranı
-- =====================================================
CREATE OR REPLACE VIEW v_user_roles_detail AS
SELECT 
    ur.user_role_id,
    u.user_id,
    u.username,
    r.role_id,
    r.role_name,
    r.description AS role_description,
    ur.assigned_at
FROM user_roles ur
JOIN users u ON ur.user_id = u.user_id
JOIN roles r ON ur.role_id = r.role_id
ORDER BY u.username, r.role_name;

COMMENT ON VIEW v_user_roles_detail IS 'Kullanıcı-rol ilişkileri detayı';

-- =====================================================
-- 11. v_prediction_details: Tahmin detayları (Admin için)
-- =====================================================
CREATE OR REPLACE VIEW v_prediction_details AS
SELECT 
    p.prediction_id,
    u.username,
    m.model_name,
    seg.segment_name,
    s.ram_gb,
    s.storage_gb,
    c.condition_name,
    p.predicted_price,
    fn_format_price(p.predicted_price) AS formatted_price,
    p.confidence_score,
    p.created_at
FROM predictions p
LEFT JOIN users u ON p.user_id = u.user_id
JOIN specs s ON p.specs_id = s.specs_id
JOIN models m ON s.model_id = m.model_id
JOIN segments seg ON m.segment_id = seg.segment_id
JOIN conditions c ON p.condition_id = c.condition_id
ORDER BY p.created_at DESC;

COMMENT ON VIEW v_prediction_details IS 'Tahmin detayları - Admin paneli için';

-- =====================================================
-- 12. v_segment_stats: Segment bazlı istatistikler
-- =====================================================
CREATE OR REPLACE VIEW v_segment_stats AS
SELECT 
    seg.segment_id,
    seg.segment_name,
    seg.segment_puan,
    COUNT(DISTINCT m.model_id) AS model_count,
    COUNT(DISTINCT s.specs_id) AS specs_count,
    COUNT(p.prediction_id) AS prediction_count,
    COALESCE(AVG(p.predicted_price), 0) AS avg_predicted_price
FROM segments seg
LEFT JOIN models m ON seg.segment_id = m.segment_id
LEFT JOIN specs s ON m.model_id = s.model_id
LEFT JOIN predictions p ON s.specs_id = p.specs_id
GROUP BY seg.segment_id, seg.segment_name, seg.segment_puan
ORDER BY seg.segment_puan;

COMMENT ON VIEW v_segment_stats IS 'Segment bazlı istatistikler';

-- =====================================================
-- ÖZET
-- =====================================================
/*
VIEW'LAR (12 adet):

USER TARAFINDA KULLANILAN:
  1. v_specs_catalog        - Specs dropdown
  2. v_condition_catalog    - Condition dropdown
  5. v_user_history_masked  - Tahmin geçmişi

ADMIN TARAFINDA KULLANILAN:
  3. v_segments_catalog     - Segment yönetimi listesi
  4. v_models_catalog       - Model yönetimi listesi
  6. v_admin_model_stats    - Model bazlı raporlar
  7. v_admin_condition_stats- Durum bazlı raporlar
  8. v_users_masked         - Kullanıcı yönetimi
  9. v_dashboard_stats      - Dashboard kartları
  10. v_user_roles_detail   - Rol atamaları
  11. v_prediction_details  - Tahmin detayları
  12. v_segment_stats       - Segment istatistikleri
*/
