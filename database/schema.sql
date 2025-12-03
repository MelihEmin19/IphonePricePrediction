-- iPhone Fiyat Tahmin Sistemi - Database Schema
-- PostgreSQL 15+

-- Veritabanı oluşturma
CREATE DATABASE iphone_price_db;

\c iphone_price_db;

-- 1. USERS tablosu
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(20) NOT NULL CHECK (role IN ('Admin', 'User')),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 2. BRANDS tablosu (Sadece Apple olacak ama genişletilebilir)
CREATE TABLE brands (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) UNIQUE NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 3. MODELS tablosu
CREATE TABLE models (
    id SERIAL PRIMARY KEY,
    brand_id INTEGER REFERENCES brands(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    release_year INTEGER NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(brand_id, name)
);

-- 4. SPECS tablosu (Teknik özellikler)
CREATE TABLE specs (
    id SERIAL PRIMARY KEY,
    model_id INTEGER REFERENCES models(id) ON DELETE CASCADE,
    ram_gb INTEGER NOT NULL,
    storage_gb INTEGER NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(model_id, ram_gb, storage_gb)
);

-- 5. LISTINGS tablosu (Scrape edilen ilanlar)
CREATE TABLE listings (
    id SERIAL PRIMARY KEY,
    spec_id INTEGER REFERENCES specs(id) ON DELETE CASCADE,
    price DECIMAL(10, 2) NOT NULL,
    condition VARCHAR(20) NOT NULL CHECK (condition IN ('Mükemmel', 'Çok İyi', 'İyi', 'Orta')),
    source VARCHAR(50) NOT NULL,
    url TEXT,
    scraped_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_active BOOLEAN DEFAULT TRUE
);

-- 6. PREDICTIONS tablosu (Yapılan tahminler)
CREATE TABLE predictions (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) ON DELETE SET NULL,
    spec_id INTEGER REFERENCES specs(id) ON DELETE CASCADE,
    condition VARCHAR(20) NOT NULL,
    predicted_price DECIMAL(10, 2) NOT NULL,
    confidence_score DECIMAL(5, 2),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- İndeksler (Performans için)
CREATE INDEX idx_listings_spec_id ON listings(spec_id);
CREATE INDEX idx_listings_scraped_at ON listings(scraped_at);
CREATE INDEX idx_predictions_user_id ON predictions(user_id);
CREATE INDEX idx_predictions_created_at ON predictions(created_at);
CREATE INDEX idx_models_brand_id ON models(brand_id);
CREATE INDEX idx_specs_model_id ON specs(model_id);

-- Başlangıç verileri
INSERT INTO brands (name) VALUES ('Apple');

-- iPhone modelleri
INSERT INTO models (brand_id, name, release_year) VALUES
    (1, 'iPhone 11', 2019),
    (1, 'iPhone 11 Pro', 2019),
    (1, 'iPhone 11 Pro Max', 2019),
    (1, 'iPhone 12', 2020),
    (1, 'iPhone 12 Mini', 2020),
    (1, 'iPhone 12 Pro', 2020),
    (1, 'iPhone 12 Pro Max', 2020),
    (1, 'iPhone 13', 2021),
    (1, 'iPhone 13 Mini', 2021),
    (1, 'iPhone 13 Pro', 2021),
    (1, 'iPhone 13 Pro Max', 2021),
    (1, 'iPhone 14', 2022),
    (1, 'iPhone 14 Plus', 2022),
    (1, 'iPhone 14 Pro', 2022),
    (1, 'iPhone 14 Pro Max', 2022),
    (1, 'iPhone 15', 2023),
    (1, 'iPhone 15 Plus', 2023),
    (1, 'iPhone 15 Pro', 2023),
    (1, 'iPhone 15 Pro Max', 2023);

-- iPhone 11 serisinin specs'leri
INSERT INTO specs (model_id, ram_gb, storage_gb) VALUES
    -- iPhone 11
    (1, 4, 64),
    (1, 4, 128),
    (1, 4, 256),
    -- iPhone 11 Pro
    (2, 4, 64),
    (2, 4, 256),
    (2, 4, 512),
    -- iPhone 11 Pro Max
    (3, 4, 64),
    (3, 4, 256),
    (3, 4, 512);

-- iPhone 12 serisinin specs'leri
INSERT INTO specs (model_id, ram_gb, storage_gb) VALUES
    -- iPhone 12
    (4, 4, 64),
    (4, 4, 128),
    (4, 4, 256),
    -- iPhone 12 Mini
    (5, 4, 64),
    (5, 4, 128),
    (5, 4, 256),
    -- iPhone 12 Pro
    (6, 6, 128),
    (6, 6, 256),
    (6, 6, 512),
    -- iPhone 12 Pro Max
    (7, 6, 128),
    (7, 6, 256),
    (7, 6, 512);

-- iPhone 13 serisinin specs'leri
INSERT INTO specs (model_id, ram_gb, storage_gb) VALUES
    -- iPhone 13
    (8, 4, 128),
    (8, 4, 256),
    (8, 4, 512),
    -- iPhone 13 Mini
    (9, 4, 128),
    (9, 4, 256),
    (9, 4, 512),
    -- iPhone 13 Pro
    (10, 6, 128),
    (10, 6, 256),
    (10, 6, 512),
    (10, 6, 1024),
    -- iPhone 13 Pro Max
    (11, 6, 128),
    (11, 6, 256),
    (11, 6, 512),
    (11, 6, 1024);

-- iPhone 14 serisinin specs'leri
INSERT INTO specs (model_id, ram_gb, storage_gb) VALUES
    -- iPhone 14
    (12, 6, 128),
    (12, 6, 256),
    (12, 6, 512),
    -- iPhone 14 Plus
    (13, 6, 128),
    (13, 6, 256),
    (13, 6, 512),
    -- iPhone 14 Pro
    (14, 6, 128),
    (14, 6, 256),
    (14, 6, 512),
    (14, 6, 1024),
    -- iPhone 14 Pro Max
    (15, 6, 128),
    (15, 6, 256),
    (15, 6, 512),
    (15, 6, 1024);

-- iPhone 15 serisinin specs'leri
INSERT INTO specs (model_id, ram_gb, storage_gb) VALUES
    -- iPhone 15
    (16, 6, 128),
    (16, 6, 256),
    (16, 6, 512),
    -- iPhone 15 Plus
    (17, 6, 128),
    (17, 6, 256),
    (17, 6, 512),
    -- iPhone 15 Pro
    (18, 8, 128),
    (18, 8, 256),
    (18, 8, 512),
    (18, 8, 1024),
    -- iPhone 15 Pro Max
    (19, 8, 256),
    (19, 8, 512),
    (19, 8, 1024);

-- Admin ve test kullanıcısı
-- Şifre: admin123 (bcrypt hash)
INSERT INTO users (username, password_hash, role) VALUES
    ('admin', '$2b$10$rQ3K5kF5z9Z5z9Z5z9Z5zeE5YvH5YvH5YvH5YvH5YvH5YvH5YvH5Y', 'Admin'),
    ('testuser', '$2b$10$rQ3K5kF5z9Z5z9Z5z9Z5zeE5YvH5YvH5YvH5YvH5YvH5YvH5YvH5Y', 'User');

-- Trigger: updated_at otomatik güncellensin
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

COMMENT ON TABLE users IS 'Sistem kullanıcıları (Admin/User rolleri)';
COMMENT ON TABLE brands IS 'Telefon markaları (Apple)';
COMMENT ON TABLE models IS 'iPhone modelleri';
COMMENT ON TABLE specs IS 'Her modelin RAM ve hafıza kombinasyonları';
COMMENT ON TABLE listings IS 'Scraper ile çekilen gerçek pazar verileri';
COMMENT ON TABLE predictions IS 'Kullanıcıların yaptığı fiyat tahminleri';

