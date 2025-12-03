-- =====================================================
-- KULLANICI TANIMLI FONKSİYONLAR
-- İster: En az 2 kullanıcı tanımlı fonksiyon (10 puan)
-- =====================================================

-- 1. fn_CalculatePriceScore: Fiyat skoru hesaplama fonksiyonu
-- Bir ürünün fiyatının piyasa ortalamasına göre skorunu hesaplar
CREATE OR REPLACE FUNCTION fn_CalculatePriceScore(
    p_price DECIMAL(10, 2),
    p_model_id INTEGER
)
RETURNS DECIMAL(5, 2) AS $$
DECLARE
    v_avg_price DECIMAL(10, 2);
    v_min_price DECIMAL(10, 2);
    v_max_price DECIMAL(10, 2);
    v_score DECIMAL(5, 2);
BEGIN
    -- Model için ortalama, min ve max fiyatları al
    SELECT 
        AVG(l.price),
        MIN(l.price),
        MAX(l.price)
    INTO v_avg_price, v_min_price, v_max_price
    FROM listings l
    JOIN specs s ON l.spec_id = s.id
    WHERE s.model_id = p_model_id
    AND l.is_active = TRUE;
    
    -- Fiyat aralığına göre skor hesapla (0-100)
    IF v_max_price = v_min_price THEN
        v_score := 50; -- Tek fiyat varsa ortalam skor
    ELSE
        -- Düşük fiyat = yüksek skor (alıcı için iyi fırsat)
        v_score := ((v_max_price - p_price) / (v_max_price - v_min_price)) * 100;
    END IF;
    
    -- Sınırlar içinde tut
    IF v_score > 100 THEN v_score := 100; END IF;
    IF v_score < 0 THEN v_score := 0; END IF;
    
    RETURN ROUND(v_score, 2);
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION fn_CalculatePriceScore IS 'Bir ürünün fiyatının model ortalamasına göre skorunu hesaplar (0-100)';


-- 2. fn_GetConditionMultiplier: Kozmetik durum çarpanı
-- Duruma göre fiyat çarpanını döndürür
CREATE OR REPLACE FUNCTION fn_GetConditionMultiplier(
    p_condition VARCHAR(20)
)
RETURNS DECIMAL(4, 2) AS $$
BEGIN
    RETURN CASE p_condition
        WHEN 'Mükemmel' THEN 1.00
        WHEN 'Çok İyi' THEN 0.93
        WHEN 'İyi' THEN 0.85
        WHEN 'Orta' THEN 0.75
        ELSE 0.80
    END;
END;
$$ LANGUAGE plpgsql IMMUTABLE;

COMMENT ON FUNCTION fn_GetConditionMultiplier IS 'Kozmetik duruma göre fiyat çarpanını döndürür';


-- 3. fn_EstimatePrice: Basit fiyat tahmini fonksiyonu
-- Model, storage ve duruma göre tahmini fiyat hesaplar
CREATE OR REPLACE FUNCTION fn_EstimatePrice(
    p_model_id INTEGER,
    p_storage_gb INTEGER,
    p_condition VARCHAR(20)
)
RETURNS DECIMAL(10, 2) AS $$
DECLARE
    v_base_price DECIMAL(10, 2);
    v_condition_mult DECIMAL(4, 2);
    v_storage_mult DECIMAL(4, 2);
    v_estimated_price DECIMAL(10, 2);
BEGIN
    -- Modelin ortalama fiyatını al
    SELECT AVG(l.price)
    INTO v_base_price
    FROM listings l
    JOIN specs s ON l.spec_id = s.id
    WHERE s.model_id = p_model_id
    AND l.is_active = TRUE;
    
    -- Eğer veri yoksa varsayılan değer
    IF v_base_price IS NULL THEN
        v_base_price := 25000;
    END IF;
    
    -- Durum çarpanı
    v_condition_mult := fn_GetConditionMultiplier(p_condition);
    
    -- Storage çarpanı
    v_storage_mult := CASE 
        WHEN p_storage_gb <= 64 THEN 0.90
        WHEN p_storage_gb <= 128 THEN 1.00
        WHEN p_storage_gb <= 256 THEN 1.15
        WHEN p_storage_gb <= 512 THEN 1.35
        ELSE 1.50
    END;
    
    -- Tahmini fiyat
    v_estimated_price := v_base_price * v_condition_mult * v_storage_mult;
    
    RETURN ROUND(v_estimated_price, 2);
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION fn_EstimatePrice IS 'Model, storage ve duruma göre tahmini fiyat hesaplar';


-- 4. fn_FormatPrice: Fiyat formatlama fonksiyonu
-- Fiyatı TL formatında string olarak döndürür
CREATE OR REPLACE FUNCTION fn_FormatPrice(
    p_price DECIMAL(10, 2)
)
RETURNS VARCHAR(50) AS $$
BEGIN
    RETURN '₺' || TO_CHAR(p_price, 'FM999,999,990.00');
END;
$$ LANGUAGE plpgsql IMMUTABLE;

COMMENT ON FUNCTION fn_FormatPrice IS 'Fiyatı Türk Lirası formatında döndürür';


-- 5. fn_GetModelAge: Model yaşını hesapla
CREATE OR REPLACE FUNCTION fn_GetModelAge(
    p_model_id INTEGER
)
RETURNS INTEGER AS $$
DECLARE
    v_release_year INTEGER;
BEGIN
    SELECT release_year INTO v_release_year
    FROM models
    WHERE id = p_model_id;
    
    RETURN EXTRACT(YEAR FROM CURRENT_DATE)::INTEGER - COALESCE(v_release_year, 2020);
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION fn_GetModelAge IS 'Modelin yaşını yıl olarak hesaplar';


-- =====================================================
-- KULLANIM ÖRNEKLERİ
-- =====================================================
/*
-- Fiyat skoru hesapla
SELECT fn_CalculatePriceScore(35000.00, 1);

-- Durum çarpanı al
SELECT fn_GetConditionMultiplier('Mükemmel');

-- Tahmini fiyat al
SELECT fn_EstimatePrice(1, 128, 'İyi');

-- Fiyat formatla
SELECT fn_FormatPrice(35000.50);

-- Model yaşı al
SELECT fn_GetModelAge(1);

-- Listing'lerle birlikte kullan
SELECT 
    m.name,
    l.price,
    fn_FormatPrice(l.price) as formatted_price,
    fn_CalculatePriceScore(l.price, s.model_id) as price_score,
    fn_GetConditionMultiplier(l.condition) as condition_mult
FROM listings l
JOIN specs s ON l.spec_id = s.id
JOIN models m ON s.model_id = m.id
WHERE l.is_active = TRUE
LIMIT 10;
*/

