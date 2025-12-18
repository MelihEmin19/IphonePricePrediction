-- =====================================================
-- KULLANICI TANIMLI FONKSİYONLAR
-- İster: En az 8 kullanıcı tanımlı fonksiyon
-- =====================================================

-- =====================================================
-- 1. fn_specs_label: Specs için okunabilir etiket oluşturur
-- Kullanım: Dropdown menülerde gösterim için
-- =====================================================
CREATE OR REPLACE FUNCTION fn_specs_label(p_specs_id INTEGER)
RETURNS VARCHAR(200) AS $$
DECLARE
    v_label VARCHAR(200);
BEGIN
    SELECT 
        m.model_name || ' ' || 
        s.storage_gb || 'GB ' || 
        s.ram_gb || 'GB RAM'
    INTO v_label
    FROM specs s
    JOIN models m ON s.model_id = m.model_id
    WHERE s.specs_id = p_specs_id;
    
    RETURN COALESCE(v_label, 'Bilinmeyen Model');
END;
$$ LANGUAGE plpgsql STABLE;

COMMENT ON FUNCTION fn_specs_label IS 'Specs için okunabilir etiket oluşturur (örn: iPhone 14 Pro 256GB 6GB RAM)';

-- =====================================================
-- 2. fn_mask_username: Kullanıcı adı maskeleme
-- Güvenlik için - ilk ve son harf gösterilir
-- =====================================================
CREATE OR REPLACE FUNCTION fn_mask_username(p_username VARCHAR)
RETURNS VARCHAR AS $$
BEGIN
    IF p_username IS NULL THEN RETURN NULL; END IF;
    IF LENGTH(p_username) <= 2 THEN RETURN REPEAT('*', LENGTH(p_username)); END IF;
    
    RETURN LEFT(p_username, 1) || REPEAT('*', LENGTH(p_username) - 2) || RIGHT(p_username, 1);
END;
$$ LANGUAGE plpgsql IMMUTABLE SECURITY DEFINER;

COMMENT ON FUNCTION fn_mask_username IS 'Kullanıcı adını maskeler (a***n formatında)';

-- =====================================================
-- 3. fn_mask_email: E-posta maskeleme
-- =====================================================
CREATE OR REPLACE FUNCTION fn_mask_email(p_email VARCHAR)
RETURNS VARCHAR AS $$
DECLARE
    v_at_pos INTEGER;
    v_local_part VARCHAR;
    v_domain_part VARCHAR;
BEGIN
    IF p_email IS NULL THEN RETURN NULL; END IF;
    
    v_at_pos := POSITION('@' IN p_email);
    IF v_at_pos = 0 THEN RETURN p_email; END IF;
    
    v_local_part := LEFT(p_email, v_at_pos - 1);
    v_domain_part := SUBSTRING(p_email FROM v_at_pos);
    
    RETURN LEFT(v_local_part, 2) || REPEAT('*', GREATEST(LENGTH(v_local_part) - 2, 0)) || v_domain_part;
END;
$$ LANGUAGE plpgsql IMMUTABLE SECURITY DEFINER;

COMMENT ON FUNCTION fn_mask_email IS 'E-posta adresini maskeler (ab***@domain.com formatında)';

-- =====================================================
-- 4. fn_get_condition_multiplier: Durum çarpanı
-- =====================================================
CREATE OR REPLACE FUNCTION fn_get_condition_multiplier(p_condition_id INTEGER)
RETURNS DECIMAL(4,2) AS $$
DECLARE
    v_multiplier DECIMAL(4,2);
BEGIN
    SELECT multiplier INTO v_multiplier
    FROM conditions
    WHERE condition_id = p_condition_id;
    
    RETURN COALESCE(v_multiplier, 1.00);
END;
$$ LANGUAGE plpgsql STABLE;

COMMENT ON FUNCTION fn_get_condition_multiplier IS 'Cihaz durumuna göre fiyat çarpanını döndürür';

-- =====================================================
-- 5. fn_get_segment_name: Segment ID'den isim döndürür
-- =====================================================
CREATE OR REPLACE FUNCTION fn_get_segment_name(p_segment_id INTEGER)
RETURNS VARCHAR(20) AS $$
DECLARE
    v_name VARCHAR(20);
BEGIN
    SELECT segment_name INTO v_name
    FROM segments
    WHERE segment_id = p_segment_id;
    
    RETURN COALESCE(v_name, 'Bilinmiyor');
END;
$$ LANGUAGE plpgsql STABLE;

COMMENT ON FUNCTION fn_get_segment_name IS 'Segment ID için isim döndürür';

-- =====================================================
-- 6. fn_get_user_role: Kullanıcının ana rolünü döndürür
-- =====================================================
CREATE OR REPLACE FUNCTION fn_get_user_role(p_user_id INTEGER)
RETURNS VARCHAR(20) AS $$
DECLARE
    v_role VARCHAR(20);
BEGIN
    SELECT r.role_name INTO v_role
    FROM user_roles ur
    JOIN roles r ON ur.role_id = r.role_id
    WHERE ur.user_id = p_user_id
    ORDER BY 
        CASE r.role_name 
            WHEN 'Admin' THEN 1 
            ELSE 2 
        END
    LIMIT 1;
    
    RETURN COALESCE(v_role, 'User');
END;
$$ LANGUAGE plpgsql STABLE;

COMMENT ON FUNCTION fn_get_user_role IS 'Kullanıcının en yüksek öncelikli rolünü döndürür';

-- =====================================================
-- 7. fn_format_price: Fiyat formatlama (TL)
-- =====================================================
CREATE OR REPLACE FUNCTION fn_format_price(p_price DECIMAL(10,2))
RETURNS VARCHAR(50) AS $$
BEGIN
    IF p_price IS NULL THEN RETURN '₺0,00'; END IF;
    RETURN '₺' || TO_CHAR(p_price, 'FM999G999G990D00');
END;
$$ LANGUAGE plpgsql IMMUTABLE;

COMMENT ON FUNCTION fn_format_price IS 'Fiyatı Türk Lirası formatında döndürür';

-- =====================================================
-- 8. fn_calculate_model_age: Model yaşını hesaplar
-- =====================================================
CREATE OR REPLACE FUNCTION fn_calculate_model_age(p_model_id INTEGER)
RETURNS INTEGER AS $$
DECLARE
    v_release_year INTEGER;
BEGIN
    SELECT release_year INTO v_release_year
    FROM models
    WHERE model_id = p_model_id;
    
    RETURN EXTRACT(YEAR FROM CURRENT_DATE)::INTEGER - COALESCE(v_release_year, 2020);
END;
$$ LANGUAGE plpgsql STABLE;

COMMENT ON FUNCTION fn_calculate_model_age IS 'Modelin yaşını yıl olarak hesaplar';

-- =====================================================
-- 9. fn_get_prediction_count: Kullanıcının tahmin sayısı
-- =====================================================
CREATE OR REPLACE FUNCTION fn_get_prediction_count(p_user_id INTEGER)
RETURNS INTEGER AS $$
DECLARE
    v_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_count
    FROM predictions
    WHERE user_id = p_user_id;
    
    RETURN COALESCE(v_count, 0);
END;
$$ LANGUAGE plpgsql STABLE;

COMMENT ON FUNCTION fn_get_prediction_count IS 'Kullanıcının toplam tahmin sayısını döndürür';

-- =====================================================
-- 10. fn_is_admin: Kullanıcının admin olup olmadığını kontrol eder
-- =====================================================
CREATE OR REPLACE FUNCTION fn_is_admin(p_user_id INTEGER)
RETURNS BOOLEAN AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1 
        FROM user_roles ur
        JOIN roles r ON ur.role_id = r.role_id
        WHERE ur.user_id = p_user_id 
        AND r.role_name = 'Admin'
    );
END;
$$ LANGUAGE plpgsql STABLE;

COMMENT ON FUNCTION fn_is_admin IS 'Kullanıcının Admin rolüne sahip olup olmadığını kontrol eder';

-- =====================================================
-- KULLANIM ÖRNEKLERİ
-- =====================================================
/*
-- Specs etiketi
SELECT fn_specs_label(1);

-- Kullanıcı adı maskeleme
SELECT fn_mask_username('ahmet');  -- a***t

-- E-posta maskeleme  
SELECT fn_mask_email('test@example.com');  -- te**@example.com

-- Durum çarpanı
SELECT fn_get_condition_multiplier(4);  -- 1.00 (Mükemmel)

-- Segment adı
SELECT fn_get_segment_name(5);  -- Pro Max

-- Kullanıcı rolü
SELECT fn_get_user_role(1);

-- Fiyat formatlama
SELECT fn_format_price(25000.50);  -- ₺25.000,50

-- Model yaşı
SELECT fn_calculate_model_age(1);

-- Tahmin sayısı
SELECT fn_get_prediction_count(1);

-- Admin kontrolü
SELECT fn_is_admin(1);
*/
