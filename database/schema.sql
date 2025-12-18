-- =====================================================
-- iPhone Fiyat Tahmin Sistemi - YENİ VERİTABANI ŞEMASI
-- UI Tasarımına Uygun - View/SP/Function Destekli
-- PostgreSQL 15+
-- =====================================================

-- Veritabanı oluşturma (varsa yoksay)
-- CREATE DATABASE iphone_price_db;
-- \c iphone_price_db;

-- Eski tabloları temizle (DİKKAT: Veri silinir!)
DROP TABLE IF EXISTS predictions CASCADE;
DROP TABLE IF EXISTS user_roles CASCADE;
DROP TABLE IF EXISTS specs CASCADE;
DROP TABLE IF EXISTS models CASCADE;
DROP TABLE IF EXISTS segments CASCADE;
DROP TABLE IF EXISTS conditions CASCADE;
DROP TABLE IF EXISTS roles CASCADE;
DROP TABLE IF EXISTS users CASCADE;
DROP TABLE IF EXISTS audit_log CASCADE;

-- =====================================================
-- 1. SEGMENTS TABLOSU (Segment kategorileri)
-- =====================================================
CREATE TABLE segments (
    segment_id SERIAL PRIMARY KEY,
    segment_name VARCHAR(20) NOT NULL UNIQUE,
    segment_puan INTEGER NOT NULL UNIQUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE segments IS 'iPhone segment kategorileri (Mini, Base, Plus, Pro, Pro Max)';

-- Segment seed data
INSERT INTO segments (segment_name, segment_puan) VALUES
    ('Mini', 1),
    ('Base', 2),
    ('Plus', 3),
    ('Pro', 4),
    ('Pro Max', 5);

-- =====================================================
-- 2. CONDITIONS TABLOSU (Cihaz durumları)
-- =====================================================
CREATE TABLE conditions (
    condition_id SERIAL PRIMARY KEY,
    condition_name VARCHAR(20) NOT NULL UNIQUE,
    condition_puan INTEGER NOT NULL UNIQUE,
    multiplier DECIMAL(4,2) NOT NULL DEFAULT 1.00,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE conditions IS 'Cihaz kozmetik durum kategorileri';

-- Condition seed data (CSV'deki cihaz_durum_puan'a göre)
INSERT INTO conditions (condition_name, condition_puan, multiplier) VALUES
    ('Outlet', 1, 0.75),
    ('İyi', 2, 0.85),
    ('Çok İyi', 3, 0.93),
    ('Mükemmel', 4, 1.00);

-- =====================================================
-- 3. ROLES TABLOSU (Uygulama rolleri)
-- =====================================================
CREATE TABLE roles (
    role_id SERIAL PRIMARY KEY,
    role_name VARCHAR(20) NOT NULL UNIQUE,
    description VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE roles IS 'Uygulama kullanıcı rolleri';

-- Role seed data
INSERT INTO roles (role_name, description) VALUES
    ('Admin', 'Tam yetkili yönetici'),
    ('User', 'Normal kullanıcı');

-- =====================================================
-- 4. USERS TABLOSU (Kullanıcılar)
-- =====================================================
CREATE TABLE users (
    user_id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    email VARCHAR(100),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE users IS 'Sistem kullanıcıları';

-- =====================================================
-- 5. USER_ROLES TABLOSU (Kullanıcı-Rol ilişkisi - M:N)
-- =====================================================
CREATE TABLE user_roles (
    user_role_id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    role_id INTEGER NOT NULL REFERENCES roles(role_id) ON DELETE CASCADE,
    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, role_id)
);

COMMENT ON TABLE user_roles IS 'Kullanıcı-rol ilişki tablosu';

-- =====================================================
-- 6. MODELS TABLOSU (iPhone modelleri)
-- =====================================================
CREATE TABLE models (
    model_id SERIAL PRIMARY KEY,
    model_name VARCHAR(100) NOT NULL,
    segment_id INTEGER NOT NULL REFERENCES segments(segment_id) ON DELETE RESTRICT,
    release_year INTEGER NOT NULL,
    model_kodu INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(model_name, segment_id)
);

COMMENT ON TABLE models IS 'iPhone model bilgileri';

-- =====================================================
-- 7. SPECS TABLOSU (Teknik özellikler - CSV kolonlarına göre)
-- =====================================================
CREATE TABLE specs (
    specs_id SERIAL PRIMARY KEY,
    model_id INTEGER NOT NULL REFERENCES models(model_id) ON DELETE CASCADE,
    ram_gb INTEGER NOT NULL,
    storage_gb INTEGER NOT NULL,
    kamera_mp INTEGER NOT NULL,
    ekran_boyutu DECIMAL(3,1) NOT NULL,
    batarya_mah INTEGER NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(model_id, ram_gb, storage_gb)
);

COMMENT ON TABLE specs IS 'iPhone teknik özellikleri (RAM, Storage, Kamera, Ekran, Batarya)';

-- =====================================================
-- 8. PREDICTIONS TABLOSU (Tahminler)
-- =====================================================
CREATE TABLE predictions (
    prediction_id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(user_id) ON DELETE SET NULL,
    specs_id INTEGER NOT NULL REFERENCES specs(specs_id) ON DELETE CASCADE,
    condition_id INTEGER NOT NULL REFERENCES conditions(condition_id) ON DELETE RESTRICT,
    predicted_price DECIMAL(10,2) NOT NULL,
    confidence_score DECIMAL(5,2),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE predictions IS 'Kullanıcı fiyat tahminleri';

-- =====================================================
-- 9. AUDIT_LOG TABLOSU (Değişiklik kayıtları)
-- =====================================================
CREATE TABLE audit_log (
    audit_id SERIAL PRIMARY KEY,
    table_name VARCHAR(50) NOT NULL,
    operation VARCHAR(10) NOT NULL,
    record_id INTEGER,
    old_data JSONB,
    new_data JSONB,
    changed_by VARCHAR(50) DEFAULT current_user,
    changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE audit_log IS 'Veritabanı değişiklik kayıtları';

-- =====================================================
-- İNDEKSLER
-- =====================================================
CREATE INDEX idx_models_segment ON models(segment_id);
CREATE INDEX idx_models_year ON models(release_year);
CREATE INDEX idx_specs_model ON specs(model_id);
CREATE INDEX idx_specs_storage ON specs(storage_gb);
CREATE INDEX idx_predictions_user ON predictions(user_id);
CREATE INDEX idx_predictions_specs ON predictions(specs_id);
CREATE INDEX idx_predictions_created ON predictions(created_at);
CREATE INDEX idx_user_roles_user ON user_roles(user_id);
CREATE INDEX idx_user_roles_role ON user_roles(role_id);

-- =====================================================
-- TRIGGER: updated_at otomatik güncellensin
-- =====================================================
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER update_users_updated_at 
BEFORE UPDATE ON users
FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =====================================================
-- SEED DATA: Admin kullanıcı
-- Şifre: admin123 (SHA256)
-- =====================================================
INSERT INTO users (username, password_hash) VALUES
    ('admin', '240be518fabd2724ddb6f04eeb9d5b8628effa9b7f1d2ea4e5e5c5e5e5e5e5e5');

-- Admin'e Admin rolü ata
INSERT INTO user_roles (user_id, role_id) 
SELECT u.user_id, r.role_id 
FROM users u, roles r 
WHERE u.username = 'admin' AND r.role_name = 'Admin';

-- =====================================================
-- ÖZET
-- =====================================================
/*
TABLOLAR (8 adet):
  1. segments     - Segment kategorileri (Mini, Base, Plus, Pro, Pro Max)
  2. conditions   - Cihaz durumları (Outlet, İyi, Çok İyi, Mükemmel)
  3. roles        - Uygulama rolleri (Admin, User)
  4. users        - Kullanıcılar
  5. user_roles   - Kullanıcı-rol ilişkisi
  6. models       - iPhone modelleri
  7. specs        - Teknik özellikler
  8. predictions  - Tahminler
  9. audit_log    - Değişiklik kayıtları

İLİŞKİLER:
  - models -> segments (N:1)
  - specs -> models (N:1)
  - predictions -> specs (N:1)
  - predictions -> conditions (N:1)
  - predictions -> users (N:1)
  - user_roles -> users (N:1)
  - user_roles -> roles (N:1)
*/
