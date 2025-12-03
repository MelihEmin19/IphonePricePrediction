-- Stored Procedures

-- 1. sp_InsertListing: Mükerrer kontrol ile ilan ekleme
CREATE OR REPLACE FUNCTION sp_InsertListing(
    p_spec_id INTEGER,
    p_price DECIMAL(10, 2),
    p_condition VARCHAR(20),
    p_source VARCHAR(50),
    p_url TEXT
)
RETURNS INTEGER AS $$
DECLARE
    v_existing_count INTEGER;
    v_new_id INTEGER;
BEGIN
    -- Aynı spec_id, source ve URL'ye sahip aktif ilan var mı kontrol et
    SELECT COUNT(*) INTO v_existing_count
    FROM listings
    WHERE spec_id = p_spec_id
      AND source = p_source
      AND url = p_url
      AND is_active = TRUE;
    
    -- Eğer yoksa ekle
    IF v_existing_count = 0 THEN
        INSERT INTO listings (spec_id, price, condition, source, url, scraped_at, is_active)
        VALUES (p_spec_id, p_price, p_condition, p_source, p_url, CURRENT_TIMESTAMP, TRUE)
        RETURNING id INTO v_new_id;
        
        RETURN v_new_id;
    ELSE
        -- Varsa 0 döndür (duplicate)
        RETURN 0;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- 2. sp_GetModelSpecs: Bir modelin tüm özelliklerini getir
CREATE OR REPLACE FUNCTION sp_GetModelSpecs(p_model_id INTEGER)
RETURNS TABLE(
    spec_id INTEGER,
    ram_gb INTEGER,
    storage_gb INTEGER,
    avg_price DECIMAL(10, 2),
    listing_count BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        s.id,
        s.ram_gb,
        s.storage_gb,
        COALESCE(AVG(l.price), 0)::DECIMAL(10, 2) AS avg_price,
        COUNT(l.id) AS listing_count
    FROM specs s
    LEFT JOIN listings l ON s.id = l.spec_id AND l.is_active = TRUE
    WHERE s.model_id = p_model_id
    GROUP BY s.id, s.ram_gb, s.storage_gb
    ORDER BY s.storage_gb, s.ram_gb;
END;
$$ LANGUAGE plpgsql;

-- 3. sp_RecordPrediction: Tahmin kaydı ekle
CREATE OR REPLACE FUNCTION sp_RecordPrediction(
    p_user_id INTEGER,
    p_spec_id INTEGER,
    p_condition VARCHAR(20),
    p_predicted_price DECIMAL(10, 2),
    p_confidence_score DECIMAL(5, 2)
)
RETURNS INTEGER AS $$
DECLARE
    v_prediction_id INTEGER;
BEGIN
    INSERT INTO predictions (user_id, spec_id, condition, predicted_price, confidence_score, created_at)
    VALUES (p_user_id, p_spec_id, p_condition, p_predicted_price, p_confidence_score, CURRENT_TIMESTAMP)
    RETURNING id INTO v_prediction_id;
    
    RETURN v_prediction_id;
END;
$$ LANGUAGE plpgsql;

-- 4. sp_DeactivateOldListings: 30 günden eski ilanları pasif yap
CREATE OR REPLACE FUNCTION sp_DeactivateOldListings()
RETURNS INTEGER AS $$
DECLARE
    v_affected_rows INTEGER;
BEGIN
    UPDATE listings
    SET is_active = FALSE
    WHERE scraped_at < CURRENT_TIMESTAMP - INTERVAL '30 days'
      AND is_active = TRUE;
    
    GET DIAGNOSTICS v_affected_rows = ROW_COUNT;
    RETURN v_affected_rows;
END;
$$ LANGUAGE plpgsql;

-- 5. sp_GetUserPredictionHistory: Kullanıcının tahmin geçmişi
CREATE OR REPLACE FUNCTION sp_GetUserPredictionHistory(
    p_user_id INTEGER,
    p_limit INTEGER DEFAULT 10
)
RETURNS TABLE(
    prediction_id INTEGER,
    model_name VARCHAR(100),
    ram_gb INTEGER,
    storage_gb INTEGER,
    condition VARCHAR(20),
    predicted_price DECIMAL(10, 2),
    confidence_score DECIMAL(5, 2),
    created_at TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        p.id,
        m.name,
        s.ram_gb,
        s.storage_gb,
        p.condition,
        p.predicted_price,
        p.confidence_score,
        p.created_at
    FROM predictions p
    JOIN specs s ON p.spec_id = s.id
    JOIN models m ON s.model_id = m.id
    WHERE p.user_id = p_user_id
    ORDER BY p.created_at DESC
    LIMIT p_limit;
END;
$$ LANGUAGE plpgsql;

-- 6. sp_GetScraperStats: Scraper istatistikleri
CREATE OR REPLACE FUNCTION sp_GetScraperStats()
RETURNS TABLE(
    total_listings BIGINT,
    active_listings BIGINT,
    last_scrape_time TIMESTAMP,
    sources_count BIGINT,
    avg_price DECIMAL(10, 2)
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        COUNT(*) AS total_listings,
        COUNT(*) FILTER (WHERE is_active = TRUE) AS active_listings,
        MAX(scraped_at) AS last_scrape_time,
        COUNT(DISTINCT source) AS sources_count,
        AVG(price) FILTER (WHERE is_active = TRUE) AS avg_price
    FROM listings;
END;
$$ LANGUAGE plpgsql;

-- 7. sp_BulkInsertListings: Toplu ilan ekleme (Scraper için optimize)
CREATE OR REPLACE FUNCTION sp_BulkInsertListings(
    listings_data JSON
)
RETURNS INTEGER AS $$
DECLARE
    listing JSON;
    v_inserted_count INTEGER := 0;
    v_result INTEGER;
BEGIN
    FOR listing IN SELECT * FROM json_array_elements(listings_data)
    LOOP
        SELECT sp_InsertListing(
            (listing->>'spec_id')::INTEGER,
            (listing->>'price')::DECIMAL(10, 2),
            listing->>'condition',
            listing->>'source',
            listing->>'url'
        ) INTO v_result;
        
        IF v_result > 0 THEN
            v_inserted_count := v_inserted_count + 1;
        END IF;
    END LOOP;
    
    RETURN v_inserted_count;
END;
$$ LANGUAGE plpgsql;

-- Kullanım örnekleri (Yorum olarak)
/*
-- Tek ilan ekleme
SELECT sp_InsertListing(1, 14500.00, 'İyi', 'N11', 'https://n11.com/...');

-- Model özelliklerini getirme
SELECT * FROM sp_GetModelSpecs(1);

-- Tahmin kaydetme
SELECT sp_RecordPrediction(1, 1, 'Mükemmel', 16250.00, 92.5);

-- Kullanıcı geçmişi
SELECT * FROM sp_GetUserPredictionHistory(1, 5);

-- Eski ilanları pasif yapma
SELECT sp_DeactivateOldListings();

-- Scraper istatistikleri
SELECT * FROM sp_GetScraperStats();

-- Toplu ilan ekleme (JSON ile)
SELECT sp_BulkInsertListings('[
    {"spec_id": 1, "price": 14500, "condition": "İyi", "source": "N11", "url": "https://..."},
    {"spec_id": 2, "price": 16000, "condition": "Mükemmel", "source": "EasyCep", "url": "https://..."}
]'::JSON);
*/

COMMENT ON FUNCTION sp_InsertListing IS 'Mükerrer kontrolü ile yeni ilan ekler';
COMMENT ON FUNCTION sp_GetModelSpecs IS 'Bir modelin tüm özelliklerini ve ortalama fiyatlarını getirir';
COMMENT ON FUNCTION sp_RecordPrediction IS 'Kullanıcının yaptığı tahmini veritabanına kaydeder';
COMMENT ON FUNCTION sp_DeactivateOldListings IS '30 günden eski ilanları pasif duruma getirir';
COMMENT ON FUNCTION sp_GetUserPredictionHistory IS 'Kullanıcının son N tahminini getirir';
COMMENT ON FUNCTION sp_GetScraperStats IS 'Scraper ve veri toplama istatistiklerini döner';
COMMENT ON FUNCTION sp_BulkInsertListings IS 'Scraper için optimize edilmiş toplu ilan ekleme';

