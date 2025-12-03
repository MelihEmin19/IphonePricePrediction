-- Views (Görünümler)

-- 1. vw_BrandAveragePrices: Marka bazlı ortalama fiyatlar (PDF isteri)
CREATE OR REPLACE VIEW vw_BrandAveragePrices AS
SELECT 
    b.id AS brand_id,
    b.name AS brand_name,
    COUNT(DISTINCT m.id) AS model_count,
    COUNT(l.id) AS listing_count,
    AVG(l.price) AS avg_price,
    MIN(l.price) AS min_price,
    MAX(l.price) AS max_price,
    STDDEV(l.price) AS price_std_dev
FROM brands b
JOIN models m ON b.id = m.brand_id
JOIN specs s ON m.id = s.model_id
JOIN listings l ON s.id = l.spec_id
WHERE l.is_active = TRUE
GROUP BY b.id, b.name;

-- 2. vw_ModelPriceStats: Model bazlı fiyat istatistikleri
CREATE OR REPLACE VIEW vw_ModelPriceStats AS
SELECT 
    m.id AS model_id,
    b.name AS brand_name,
    m.name AS model_name,
    m.release_year,
    COUNT(DISTINCT s.id) AS spec_variants,
    COUNT(l.id) AS listing_count,
    AVG(l.price) AS avg_price,
    MIN(l.price) AS min_price,
    MAX(l.price) AS max_price,
    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY l.price) AS median_price
FROM models m
JOIN brands b ON m.brand_id = b.id
JOIN specs s ON m.id = s.model_id
LEFT JOIN listings l ON s.id = l.spec_id AND l.is_active = TRUE
GROUP BY m.id, b.name, m.name, m.release_year
ORDER BY m.release_year DESC, m.name;

-- 3. vw_SpecDetailedInfo: Spec detaylı bilgiler (Tahmin için)
CREATE OR REPLACE VIEW vw_SpecDetailedInfo AS
SELECT 
    s.id AS spec_id,
    b.name AS brand_name,
    m.name AS model_name,
    m.release_year,
    s.ram_gb,
    s.storage_gb,
    COUNT(l.id) FILTER (WHERE l.is_active = TRUE) AS active_listings,
    AVG(l.price) FILTER (WHERE l.is_active = TRUE) AS avg_price,
    AVG(l.price) FILTER (WHERE l.condition = 'Mükemmel' AND l.is_active = TRUE) AS avg_price_perfect,
    AVG(l.price) FILTER (WHERE l.condition = 'Çok İyi' AND l.is_active = TRUE) AS avg_price_very_good,
    AVG(l.price) FILTER (WHERE l.condition = 'İyi' AND l.is_active = TRUE) AS avg_price_good,
    AVG(l.price) FILTER (WHERE l.condition = 'Orta' AND l.is_active = TRUE) AS avg_price_fair,
    MAX(l.scraped_at) AS last_updated
FROM specs s
JOIN models m ON s.model_id = m.id
JOIN brands b ON m.brand_id = b.id
LEFT JOIN listings l ON s.id = l.spec_id
GROUP BY s.id, b.name, m.name, m.release_year, s.ram_gb, s.storage_gb;

-- 4. vw_RecentListings: Son eklenen aktif ilanlar
CREATE OR REPLACE VIEW vw_RecentListings AS
SELECT 
    l.id AS listing_id,
    b.name AS brand_name,
    m.name AS model_name,
    s.ram_gb,
    s.storage_gb,
    l.condition,
    l.price,
    l.source,
    l.scraped_at,
    EXTRACT(DAY FROM CURRENT_TIMESTAMP - l.scraped_at) AS days_old
FROM listings l
JOIN specs s ON l.spec_id = s.id
JOIN models m ON s.model_id = m.id
JOIN brands b ON m.brand_id = b.id
WHERE l.is_active = TRUE
ORDER BY l.scraped_at DESC
LIMIT 100;

-- 5. vw_UserActivity: Kullanıcı aktivite özeti
CREATE OR REPLACE VIEW vw_UserActivity AS
SELECT 
    u.id AS user_id,
    u.username,
    u.role,
    COUNT(p.id) AS total_predictions,
    AVG(p.predicted_price) AS avg_predicted_price,
    MAX(p.created_at) AS last_prediction_date,
    EXTRACT(DAY FROM CURRENT_TIMESTAMP - MAX(p.created_at)) AS days_since_last_activity
FROM users u
LEFT JOIN predictions p ON u.id = p.user_id
GROUP BY u.id, u.username, u.role;

-- 6. vw_ConditionPriceImpact: Kozmetik durumun fiyata etkisi
CREATE OR REPLACE VIEW vw_ConditionPriceImpact AS
SELECT 
    m.name AS model_name,
    s.storage_gb,
    l.condition,
    COUNT(l.id) AS listing_count,
    AVG(l.price) AS avg_price,
    MIN(l.price) AS min_price,
    MAX(l.price) AS max_price
FROM listings l
JOIN specs s ON l.spec_id = s.id
JOIN models m ON s.model_id = m.id
WHERE l.is_active = TRUE
GROUP BY m.name, s.storage_gb, l.condition
ORDER BY m.name, s.storage_gb, 
    CASE l.condition
        WHEN 'Mükemmel' THEN 1
        WHEN 'Çok İyi' THEN 2
        WHEN 'İyi' THEN 3
        WHEN 'Orta' THEN 4
    END;

-- 7. vw_PredictionAccuracy: Tahmin doğruluğu analizi (gelecekte gerçek satış verileriyle karşılaştırma için)
CREATE OR REPLACE VIEW vw_PredictionAccuracy AS
SELECT 
    p.id AS prediction_id,
    u.username,
    m.name AS model_name,
    s.ram_gb,
    s.storage_gb,
    p.condition,
    p.predicted_price,
    p.confidence_score,
    (SELECT AVG(price) 
     FROM listings 
     WHERE spec_id = p.spec_id 
       AND condition = p.condition 
       AND is_active = TRUE) AS actual_avg_price,
    p.created_at
FROM predictions p
JOIN users u ON p.user_id = u.id
JOIN specs s ON p.spec_id = s.id
JOIN models m ON s.model_id = m.id
ORDER BY p.created_at DESC;

-- 8. vw_DashboardStats: Admin dashboard için özet istatistikler
CREATE OR REPLACE VIEW vw_DashboardStats AS
SELECT 
    (SELECT COUNT(*) FROM users) AS total_users,
    (SELECT COUNT(*) FROM users WHERE role = 'Admin') AS admin_count,
    (SELECT COUNT(*) FROM models) AS total_models,
    (SELECT COUNT(*) FROM specs) AS total_specs,
    (SELECT COUNT(*) FROM listings WHERE is_active = TRUE) AS active_listings,
    (SELECT COUNT(*) FROM predictions) AS total_predictions,
    (SELECT AVG(price) FROM listings WHERE is_active = TRUE) AS overall_avg_price,
    (SELECT MAX(scraped_at) FROM listings) AS last_scrape_time,
    (SELECT COUNT(*) FROM predictions WHERE created_at > CURRENT_TIMESTAMP - INTERVAL '24 hours') AS predictions_last_24h;

-- Kullanım örnekleri (Yorum olarak)
/*
-- Marka bazlı ortalamalar
SELECT * FROM vw_BrandAveragePrices;

-- Model fiyat istatistikleri
SELECT * FROM vw_ModelPriceStats WHERE model_name LIKE '%iPhone 13%';

-- Spec detayları
SELECT * FROM vw_SpecDetailedInfo WHERE spec_id = 1;

-- Son ilanlar
SELECT * FROM vw_RecentListings LIMIT 20;

-- Kullanıcı aktiviteleri
SELECT * FROM vw_UserActivity ORDER BY total_predictions DESC;

-- Kozmetik durum etkisi
SELECT * FROM vw_ConditionPriceImpact WHERE model_name = 'iPhone 11';

-- Tahmin doğruluğu
SELECT * FROM vw_PredictionAccuracy WHERE confidence_score > 80;

-- Dashboard istatistikleri
SELECT * FROM vw_DashboardStats;
*/

COMMENT ON VIEW vw_BrandAveragePrices IS 'Marka bazlı ortalama fiyat ve istatistikler (PDF isteri)';
COMMENT ON VIEW vw_ModelPriceStats IS 'Model bazlı detaylı fiyat istatistikleri';
COMMENT ON VIEW vw_SpecDetailedInfo IS 'Her spec için kozmetik duruma göre ortalama fiyatlar';
COMMENT ON VIEW vw_RecentListings IS 'Son eklenen aktif ilanların listesi';
COMMENT ON VIEW vw_UserActivity IS 'Kullanıcı aktivite özeti';
COMMENT ON VIEW vw_ConditionPriceImpact IS 'Kozmetik durumun model ve hafızaya göre fiyat etkisi';
COMMENT ON VIEW vw_PredictionAccuracy IS 'Tahmin doğruluğu analizi için karşılaştırmalı view';
COMMENT ON VIEW vw_DashboardStats IS 'Admin dashboard için özet istatistikler';

