-- =====================================================
-- VERİTABANI GÜVENLİK, ROL VE MASKELEME
-- İster: Yetkilendirme ve maskeleme operasyonları
-- =====================================================

-- NOT: Bu script veritabanı yöneticisi (postgres) olarak çalıştırılmalıdır.

-- =====================================================
-- 1. VERİTABANI ROLLERİ OLUŞTURMA
-- =====================================================

-- Admin rolü - tam yetki
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'app_admin') THEN
        CREATE ROLE app_admin WITH LOGIN PASSWORD 'admin_secure_2024';
    END IF;
END
$$;

-- User rolü - sınırlı yetki
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'app_user') THEN
        CREATE ROLE app_user WITH LOGIN PASSWORD 'user_secure_2024';
    END IF;
END
$$;

-- API Service rolü - uygulama bağlantısı için
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'app_api') THEN
        CREATE ROLE app_api WITH LOGIN PASSWORD 'api_secure_2024';
    END IF;
END
$$;

-- =====================================================
-- 2. YETKİLENDİRME (GRANT)
-- =====================================================

-- Veritabanı bağlantı yetkisi
GRANT CONNECT ON DATABASE iphone_price_db TO app_admin;
GRANT CONNECT ON DATABASE iphone_price_db TO app_user;
GRANT CONNECT ON DATABASE iphone_price_db TO app_api;

-- Schema yetkisi
GRANT USAGE ON SCHEMA public TO app_admin;
GRANT USAGE ON SCHEMA public TO app_user;
GRANT USAGE ON SCHEMA public TO app_api;

-- =====================================================
-- APP_ADMIN ROLÜ YETKİLERİ (Tam yetki)
-- =====================================================
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO app_admin;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO app_admin;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO app_admin;

-- =====================================================
-- APP_USER ROLÜ YETKİLERİ (Sınırlı)
-- =====================================================
-- Sadece okuma
GRANT SELECT ON segments, conditions, models, specs TO app_user;
GRANT SELECT ON v_specs_catalog, v_condition_catalog TO app_user;

-- Tahmin ekleme
GRANT SELECT, INSERT ON predictions TO app_user;
GRANT USAGE ON SEQUENCE predictions_prediction_id_seq TO app_user;

-- Kendi geçmişini görme (RLS ile)
GRANT SELECT ON v_user_history_masked TO app_user;

-- SP çağırma
GRANT EXECUTE ON FUNCTION sp_create_prediction TO app_user;
GRANT EXECUTE ON FUNCTION sp_get_user_history TO app_user;
GRANT EXECUTE ON FUNCTION fn_specs_label TO app_user;

-- =====================================================
-- APP_API ROLÜ YETKİLERİ (Uygulama erişimi)
-- =====================================================
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA public TO app_api;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO app_api;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO app_api;

-- =====================================================
-- 3. ROW LEVEL SECURITY (RLS)
-- =====================================================

-- Users tablosu için RLS
ALTER TABLE users ENABLE ROW LEVEL SECURITY;

-- Admin her şeyi görebilir
CREATE POLICY admin_all_users ON users
    FOR ALL
    TO app_admin
    USING (true);

-- API tüm kullanıcıları görebilir (uygulama login için)
CREATE POLICY api_all_users ON users
    FOR SELECT
    TO app_api
    USING (true);

-- User sadece kendi kaydını görebilir
CREATE POLICY user_own_record ON users
    FOR SELECT
    TO app_user
    USING (username = current_user);

-- =====================================================
-- Predictions tablosu için RLS
-- =====================================================
ALTER TABLE predictions ENABLE ROW LEVEL SECURITY;

-- Admin tüm tahminleri görebilir
CREATE POLICY admin_all_predictions ON predictions
    FOR ALL
    TO app_admin
    USING (true);

-- API tüm tahminleri görebilir
CREATE POLICY api_all_predictions ON predictions
    FOR ALL
    TO app_api
    USING (true);

-- User sadece kendi tahminlerini görebilir
CREATE POLICY user_own_predictions ON predictions
    FOR SELECT
    TO app_user
    USING (user_id IN (SELECT user_id FROM users WHERE username = current_user));

-- User kendi tahminini ekleyebilir
CREATE POLICY user_insert_predictions ON predictions
    FOR INSERT
    TO app_user
    WITH CHECK (user_id IN (SELECT user_id FROM users WHERE username = current_user));

-- =====================================================
-- 4. EK MASKELEME FONKSİYONLARI
-- =====================================================

-- Password hash maskeleme (güvenlik için)
CREATE OR REPLACE FUNCTION fn_mask_password_hash(p_hash VARCHAR)
RETURNS VARCHAR AS $$
BEGIN
    IF p_hash IS NULL THEN RETURN NULL; END IF;
    RETURN LEFT(p_hash, 8) || '...[MASKED]';
END;
$$ LANGUAGE plpgsql IMMUTABLE SECURITY DEFINER;

COMMENT ON FUNCTION fn_mask_password_hash IS 'Şifre hash değerini maskeler';

-- =====================================================
-- 5. AUDIT TRIGGER
-- =====================================================

-- Audit trigger fonksiyonu
CREATE OR REPLACE FUNCTION fn_audit_trigger()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        INSERT INTO audit_log(table_name, operation, record_id, old_data, new_data)
        VALUES (TG_TABLE_NAME, 'DELETE', OLD.user_id, row_to_json(OLD)::jsonb, NULL);
        RETURN OLD;
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO audit_log(table_name, operation, record_id, old_data, new_data)
        VALUES (TG_TABLE_NAME, 'UPDATE', NEW.user_id, row_to_json(OLD)::jsonb, row_to_json(NEW)::jsonb);
        RETURN NEW;
    ELSIF TG_OP = 'INSERT' THEN
        INSERT INTO audit_log(table_name, operation, record_id, old_data, new_data)
        VALUES (TG_TABLE_NAME, 'INSERT', NEW.user_id, NULL, row_to_json(NEW)::jsonb);
        RETURN NEW;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- Users tablosu için audit trigger
DROP TRIGGER IF EXISTS audit_users_trigger ON users;
CREATE TRIGGER audit_users_trigger
AFTER INSERT OR UPDATE OR DELETE ON users
FOR EACH ROW EXECUTE FUNCTION fn_audit_trigger();

-- =====================================================
-- ÖZET
-- =====================================================
/*
VERİTABANI ROLLERİ:
  - app_admin: Tam yetki (tüm tablolar, tüm işlemler)
  - app_user: Sınırlı yetki (sadece kendi verileri, tahmin ekleme)
  - app_api: Uygulama bağlantısı (geniş okuma/yazma)

ROW LEVEL SECURITY:
  - users: Kullanıcı sadece kendi kaydını görür
  - predictions: Kullanıcı sadece kendi tahminlerini görür

MASKELEME FONKSİYONLARI:
  - fn_mask_username(): Kullanıcı adı maskeleme
  - fn_mask_email(): E-posta maskeleme
  - fn_mask_password_hash(): Şifre hash maskeleme

AUDIT:
  - audit_log tablosu tüm değişiklikleri kaydeder
  - Trigger ile otomatik loglama

NOT: Uygulama (UI) tarafındaki "Admin/User" rolleri
ile veritabanı rolleri (app_admin/app_user) farklıdır.
UI rolleri "roles" tablosunda, DB rolleri PostgreSQL'de.
*/
