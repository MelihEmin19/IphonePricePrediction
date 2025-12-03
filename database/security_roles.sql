-- =====================================================
-- VERİTABANI GÜVENLİK, ROL VE MASKELEME
-- İster: Yetkilendirme ve maskeleme operasyonları (10 puan)
-- =====================================================

-- NOT: Bu script veritabanı yöneticisi (postgres) olarak çalıştırılmalıdır.
-- PostgreSQL superuser yetkileri gerektirir.

-- =====================================================
-- 1. ROL OLUŞTURMA
-- =====================================================

-- Admin rolü - tam yetki
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'iphone_admin') THEN
        CREATE ROLE iphone_admin WITH LOGIN PASSWORD 'admin_secure_123';
    END IF;
END
$$;

-- User rolü - sınırlı yetki
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'iphone_user') THEN
        CREATE ROLE iphone_user WITH LOGIN PASSWORD 'user_secure_123';
    END IF;
END
$$;

-- ReadOnly rolü - sadece okuma
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'iphone_readonly') THEN
        CREATE ROLE iphone_readonly WITH LOGIN PASSWORD 'readonly_123';
    END IF;
END
$$;

-- API Service rolü - uygulama bağlantısı için
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'iphone_api') THEN
        CREATE ROLE iphone_api WITH LOGIN PASSWORD 'api_secure_123';
    END IF;
END
$$;


-- =====================================================
-- 2. YETKİLENDİRME (GRANT)
-- =====================================================

-- Veritabanı bağlantı yetkisi
GRANT CONNECT ON DATABASE iphone_price_db TO iphone_admin;
GRANT CONNECT ON DATABASE iphone_price_db TO iphone_user;
GRANT CONNECT ON DATABASE iphone_price_db TO iphone_readonly;
GRANT CONNECT ON DATABASE iphone_price_db TO iphone_api;

-- Schema yetkisi
GRANT USAGE ON SCHEMA public TO iphone_admin;
GRANT USAGE ON SCHEMA public TO iphone_user;
GRANT USAGE ON SCHEMA public TO iphone_readonly;
GRANT USAGE ON SCHEMA public TO iphone_api;


-- ADMIN ROLÜ YETKİLERİ (Tam yetki)
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO iphone_admin;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO iphone_admin;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO iphone_admin;


-- USER ROLÜ YETKİLERİ (CRUD - users tablosu hariç)
GRANT SELECT, INSERT, UPDATE ON listings TO iphone_user;
GRANT SELECT, INSERT ON predictions TO iphone_user;
GRANT SELECT ON brands, models, specs TO iphone_user;
GRANT USAGE ON SEQUENCE listings_id_seq TO iphone_user;
GRANT USAGE ON SEQUENCE predictions_id_seq TO iphone_user;
-- Users tablosuna sadece kendi kaydına erişim (RLS ile)


-- READONLY ROLÜ YETKİLERİ (Sadece SELECT)
GRANT SELECT ON brands, models, specs, listings TO iphone_readonly;
GRANT SELECT ON vw_BrandAveragePrices, vw_ModelPriceStats, vw_DashboardStats TO iphone_readonly;
-- Hassas tablolara (users, predictions) erişim yok


-- API ROLÜ YETKİLERİ
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA public TO iphone_api;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO iphone_api;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO iphone_api;


-- =====================================================
-- 3. ROW LEVEL SECURITY (RLS)
-- =====================================================

-- Users tablosu için RLS aktif et
ALTER TABLE users ENABLE ROW LEVEL SECURITY;

-- Admin her şeyi görebilir
CREATE POLICY admin_all_users ON users
    FOR ALL
    TO iphone_admin
    USING (true);

-- User sadece kendi kaydını görebilir
CREATE POLICY user_own_record ON users
    FOR SELECT
    TO iphone_user
    USING (username = current_user);

-- Predictions tablosu için RLS
ALTER TABLE predictions ENABLE ROW LEVEL SECURITY;

-- Admin tüm tahminleri görebilir
CREATE POLICY admin_all_predictions ON predictions
    FOR ALL
    TO iphone_admin
    USING (true);

-- User sadece kendi tahminlerini görebilir
CREATE POLICY user_own_predictions ON predictions
    FOR ALL
    TO iphone_user
    USING (user_id IN (SELECT id FROM users WHERE username = current_user));


-- =====================================================
-- 4. VERİ MASKELEME (Data Masking)
-- =====================================================

-- Maskeleme fonksiyonu: E-posta maskeleme
CREATE OR REPLACE FUNCTION fn_MaskEmail(email VARCHAR)
RETURNS VARCHAR AS $$
DECLARE
    at_pos INTEGER;
    local_part VARCHAR;
    domain_part VARCHAR;
BEGIN
    IF email IS NULL THEN RETURN NULL; END IF;
    
    at_pos := POSITION('@' IN email);
    IF at_pos = 0 THEN RETURN email; END IF;
    
    local_part := LEFT(email, at_pos - 1);
    domain_part := SUBSTRING(email FROM at_pos);
    
    -- İlk 2 karakter göster, geri kalanı maskele
    RETURN LEFT(local_part, 2) || REPEAT('*', LENGTH(local_part) - 2) || domain_part;
END;
$$ LANGUAGE plpgsql IMMUTABLE SECURITY DEFINER;


-- Maskeleme fonksiyonu: Şifre hash maskeleme
CREATE OR REPLACE FUNCTION fn_MaskPasswordHash(hash VARCHAR)
RETURNS VARCHAR AS $$
BEGIN
    IF hash IS NULL THEN RETURN NULL; END IF;
    -- Sadece ilk 10 karakter göster
    RETURN LEFT(hash, 10) || '...[MASKED]';
END;
$$ LANGUAGE plpgsql IMMUTABLE SECURITY DEFINER;


-- Maskeleme fonksiyonu: Kullanıcı adı kısmi maskeleme
CREATE OR REPLACE FUNCTION fn_MaskUsername(username VARCHAR)
RETURNS VARCHAR AS $$
BEGIN
    IF username IS NULL THEN RETURN NULL; END IF;
    IF LENGTH(username) <= 3 THEN RETURN REPEAT('*', LENGTH(username)); END IF;
    
    -- İlk ve son karakter göster
    RETURN LEFT(username, 1) || REPEAT('*', LENGTH(username) - 2) || RIGHT(username, 1);
END;
$$ LANGUAGE plpgsql IMMUTABLE SECURITY DEFINER;


-- =====================================================
-- 5. MASKELİ VİEW'LAR
-- =====================================================

-- Kullanıcılar için maskelenmiş view
CREATE OR REPLACE VIEW vw_MaskedUsers AS
SELECT 
    id,
    fn_MaskUsername(username) AS username_masked,
    fn_MaskPasswordHash(password_hash) AS password_hash_masked,
    role,
    created_at,
    -- Hassas bilgileri gizle
    CASE 
        WHEN current_user = 'iphone_admin' THEN username
        ELSE fn_MaskUsername(username)
    END AS username_conditional
FROM users;

-- Normal kullanıcılar için güvenli view
GRANT SELECT ON vw_MaskedUsers TO iphone_user;
GRANT SELECT ON vw_MaskedUsers TO iphone_readonly;


-- Listings için güvenli view (URL maskelenmiş)
CREATE OR REPLACE VIEW vw_SecureListings AS
SELECT 
    l.id,
    m.name AS model_name,
    s.ram_gb,
    s.storage_gb,
    l.condition,
    l.price,
    l.source,
    -- URL'yi maskele (dış kaynak koruma)
    CASE 
        WHEN current_user IN ('iphone_admin', 'postgres') THEN l.url
        ELSE LEFT(COALESCE(l.url, ''), 30) || '...[hidden]'
    END AS url_masked,
    l.scraped_at,
    l.is_active
FROM listings l
JOIN specs s ON l.spec_id = s.id
JOIN models m ON s.model_id = m.id;

GRANT SELECT ON vw_SecureListings TO iphone_user;
GRANT SELECT ON vw_SecureListings TO iphone_readonly;


-- =====================================================
-- 6. AUDIT LOG TABLOSU
-- =====================================================

CREATE TABLE IF NOT EXISTS audit_log (
    id SERIAL PRIMARY KEY,
    table_name VARCHAR(50) NOT NULL,
    operation VARCHAR(10) NOT NULL,
    old_data JSONB,
    new_data JSONB,
    changed_by VARCHAR(50) DEFAULT current_user,
    changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Audit trigger fonksiyonu
CREATE OR REPLACE FUNCTION fn_AuditTrigger()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        INSERT INTO audit_log(table_name, operation, old_data, new_data)
        VALUES (TG_TABLE_NAME, 'DELETE', row_to_json(OLD)::jsonb, NULL);
        RETURN OLD;
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO audit_log(table_name, operation, old_data, new_data)
        VALUES (TG_TABLE_NAME, 'UPDATE', row_to_json(OLD)::jsonb, row_to_json(NEW)::jsonb);
        RETURN NEW;
    ELSIF TG_OP = 'INSERT' THEN
        INSERT INTO audit_log(table_name, operation, old_data, new_data)
        VALUES (TG_TABLE_NAME, 'INSERT', NULL, row_to_json(NEW)::jsonb);
        RETURN NEW;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- Users tablosu için audit trigger
DROP TRIGGER IF EXISTS audit_users_trigger ON users;
CREATE TRIGGER audit_users_trigger
AFTER INSERT OR UPDATE OR DELETE ON users
FOR EACH ROW EXECUTE FUNCTION fn_AuditTrigger();

-- Listings tablosu için audit trigger
DROP TRIGGER IF EXISTS audit_listings_trigger ON listings;
CREATE TRIGGER audit_listings_trigger
AFTER INSERT OR UPDATE OR DELETE ON listings
FOR EACH ROW EXECUTE FUNCTION fn_AuditTrigger();


-- =====================================================
-- ÖZET
-- =====================================================
/*
OLUŞTURULAN ROLLER:
  - iphone_admin: Tam yetki
  - iphone_user: CRUD (kısıtlı)
  - iphone_readonly: Sadece SELECT
  - iphone_api: Uygulama bağlantısı

MASKELEME FONKSİYONLARI:
  - fn_MaskEmail(): E-posta maskeleme
  - fn_MaskPasswordHash(): Şifre hash maskeleme  
  - fn_MaskUsername(): Kullanıcı adı maskeleme

GÜVENLİ VIEW'LAR:
  - vw_MaskedUsers: Maskelenmiş kullanıcı bilgileri
  - vw_SecureListings: URL'si maskelenmiş ilanlar

ROW LEVEL SECURITY:
  - users: Kullanıcı sadece kendi kaydını görür
  - predictions: Kullanıcı sadece kendi tahminlerini görür

AUDIT:
  - audit_log tablosu tüm değişiklikleri kaydeder
*/

