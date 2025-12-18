-- =====================================================
-- STORED PROCEDURES (PostgreSQL 11+ PROCEDURE Syntax)
-- İster: En az 7 stored procedure
-- CALL ile çağrılır, OUT parametrelerle değer döndürür
-- =====================================================

-- =====================================================
-- 1. sp_create_prediction: Tahmin kaydı oluştur
-- Kullanım: CALL sp_create_prediction(1, 1, 4, 25000.00, 92.5, NULL);
-- =====================================================
CREATE OR REPLACE PROCEDURE sp_create_prediction(
    IN p_user_id INTEGER,
    IN p_specs_id INTEGER,
    IN p_condition_id INTEGER,
    IN p_predicted_price DECIMAL(10,2),
    IN p_confidence_score DECIMAL(5,2),
    INOUT p_prediction_id INTEGER DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    -- Validasyon
    IF NOT EXISTS (SELECT 1 FROM users WHERE user_id = p_user_id) THEN
        RAISE EXCEPTION 'Geçersiz kullanıcı ID: %', p_user_id;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM specs WHERE specs_id = p_specs_id) THEN
        RAISE EXCEPTION 'Geçersiz specs ID: %', p_specs_id;
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM conditions WHERE condition_id = p_condition_id) THEN
        RAISE EXCEPTION 'Geçersiz durum ID: %', p_condition_id;
    END IF;
    
    -- Tahmin ekle
    INSERT INTO predictions (user_id, specs_id, condition_id, predicted_price, confidence_score, created_at)
    VALUES (p_user_id, p_specs_id, p_condition_id, p_predicted_price, p_confidence_score, CURRENT_TIMESTAMP)
    RETURNING prediction_id INTO p_prediction_id;
    
    -- Audit log
    INSERT INTO audit_log (table_name, operation, record_id, new_data)
    VALUES ('predictions', 'INSERT', p_prediction_id,
            jsonb_build_object('user_id', p_user_id, 'specs_id', p_specs_id, 'predicted_price', p_predicted_price));
END;
$$;

COMMENT ON PROCEDURE sp_create_prediction IS 'Yeni tahmin kaydı oluşturur - User tahmin formu için';

-- =====================================================
-- 2. sp_admin_add_segment: Segment ekle
-- Kullanım: CALL sp_admin_add_segment('Ultra', NULL);
-- =====================================================
CREATE OR REPLACE PROCEDURE sp_admin_add_segment(
    IN p_segment_name VARCHAR(20),
    INOUT p_segment_id INTEGER DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_max_puan INTEGER;
BEGIN
    -- İsim kontrolü
    IF EXISTS (SELECT 1 FROM segments WHERE LOWER(segment_name) = LOWER(p_segment_name)) THEN
        RAISE EXCEPTION 'Bu segment zaten mevcut: %', p_segment_name;
    END IF;
    
    -- Yeni puan hesapla (en yüksek + 1)
    SELECT COALESCE(MAX(segment_puan), 0) + 1 INTO v_max_puan FROM segments;
    
    -- Segment ekle
    INSERT INTO segments (segment_name, segment_puan, created_at)
    VALUES (p_segment_name, v_max_puan, CURRENT_TIMESTAMP)
    RETURNING segment_id INTO p_segment_id;
    
    -- Audit log
    INSERT INTO audit_log (table_name, operation, record_id, new_data)
    VALUES ('segments', 'INSERT', p_segment_id, jsonb_build_object('segment_name', p_segment_name));
END;
$$;

COMMENT ON PROCEDURE sp_admin_add_segment IS 'Yeni segment ekler - Admin paneli için';

-- =====================================================
-- 3. sp_admin_add_model: Model ekle
-- Kullanım: CALL sp_admin_add_model('iPhone 16', 'Base', 2024, NULL);
-- =====================================================
CREATE OR REPLACE PROCEDURE sp_admin_add_model(
    IN p_model_name VARCHAR(100),
    IN p_segment_name VARCHAR(20),
    IN p_release_year INTEGER,
    INOUT p_model_id INTEGER DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_segment_id INTEGER;
BEGIN
    -- Segment bul
    SELECT segment_id INTO v_segment_id 
    FROM segments 
    WHERE LOWER(segment_name) = LOWER(p_segment_name);
    
    IF v_segment_id IS NULL THEN
        RAISE EXCEPTION 'Segment bulunamadı: %', p_segment_name;
    END IF;
    
    -- Duplicate kontrolü
    IF EXISTS (SELECT 1 FROM models WHERE model_name = p_model_name AND segment_id = v_segment_id) THEN
        RAISE EXCEPTION 'Bu model-segment kombinasyonu zaten mevcut';
    END IF;
    
    -- Model ekle
    INSERT INTO models (model_name, segment_id, release_year, created_at)
    VALUES (p_model_name, v_segment_id, p_release_year, CURRENT_TIMESTAMP)
    RETURNING model_id INTO p_model_id;
    
    -- Audit log
    INSERT INTO audit_log (table_name, operation, record_id, new_data)
    VALUES ('models', 'INSERT', p_model_id, 
            jsonb_build_object('model_name', p_model_name, 'segment_name', p_segment_name, 'release_year', p_release_year));
END;
$$;

COMMENT ON PROCEDURE sp_admin_add_model IS 'Yeni model ekler - Admin paneli için';

-- =====================================================
-- 4. sp_admin_update_model: Model güncelle
-- Kullanım: CALL sp_admin_update_model(1, 'iPhone 16 Pro', 'Pro', 2024, NULL);
-- =====================================================
CREATE OR REPLACE PROCEDURE sp_admin_update_model(
    IN p_model_id INTEGER,
    IN p_model_name VARCHAR(100),
    IN p_segment_name VARCHAR(20),
    IN p_release_year INTEGER,
    INOUT p_success BOOLEAN DEFAULT FALSE
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_segment_id INTEGER;
    v_old_data JSONB;
BEGIN
    -- Eski veriyi al
    SELECT jsonb_build_object(
        'model_name', model_name, 
        'segment_id', segment_id, 
        'release_year', release_year
    ) INTO v_old_data
    FROM models WHERE model_id = p_model_id;
    
    IF v_old_data IS NULL THEN
        RAISE EXCEPTION 'Model bulunamadı: %', p_model_id;
    END IF;
    
    -- Segment bul
    SELECT segment_id INTO v_segment_id 
    FROM segments 
    WHERE LOWER(segment_name) = LOWER(p_segment_name);
    
    IF v_segment_id IS NULL THEN
        RAISE EXCEPTION 'Segment bulunamadı: %', p_segment_name;
    END IF;
    
    -- Güncelle
    UPDATE models 
    SET model_name = p_model_name,
        segment_id = v_segment_id,
        release_year = p_release_year
    WHERE model_id = p_model_id;
    
    -- Audit log
    INSERT INTO audit_log (table_name, operation, record_id, old_data, new_data)
    VALUES ('models', 'UPDATE', p_model_id, v_old_data,
            jsonb_build_object('model_name', p_model_name, 'segment_name', p_segment_name, 'release_year', p_release_year));
    
    p_success := TRUE;
END;
$$;

COMMENT ON PROCEDURE sp_admin_update_model IS 'Model günceller - Admin paneli için';

-- =====================================================
-- 5. sp_admin_add_specs: Specs ekle
-- Kullanım: CALL sp_admin_add_specs(1, 8, 256, 48, 6.1, 3200, NULL);
-- =====================================================
CREATE OR REPLACE PROCEDURE sp_admin_add_specs(
    IN p_model_id INTEGER,
    IN p_ram_gb INTEGER,
    IN p_storage_gb INTEGER,
    IN p_kamera_mp INTEGER,
    IN p_ekran_boyutu DECIMAL(3,1),
    IN p_batarya_mah INTEGER,
    INOUT p_specs_id INTEGER DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    -- Model kontrolü
    IF NOT EXISTS (SELECT 1 FROM models WHERE model_id = p_model_id) THEN
        RAISE EXCEPTION 'Model bulunamadı: %', p_model_id;
    END IF;
    
    -- Duplicate kontrolü
    IF EXISTS (SELECT 1 FROM specs WHERE model_id = p_model_id AND ram_gb = p_ram_gb AND storage_gb = p_storage_gb) THEN
        RAISE EXCEPTION 'Bu model için bu RAM/Storage kombinasyonu zaten mevcut';
    END IF;
    
    -- Specs ekle
    INSERT INTO specs (model_id, ram_gb, storage_gb, kamera_mp, ekran_boyutu, batarya_mah, created_at)
    VALUES (p_model_id, p_ram_gb, p_storage_gb, p_kamera_mp, p_ekran_boyutu, p_batarya_mah, CURRENT_TIMESTAMP)
    RETURNING specs_id INTO p_specs_id;
    
    -- Audit log
    INSERT INTO audit_log (table_name, operation, record_id, new_data)
    VALUES ('specs', 'INSERT', p_specs_id,
            jsonb_build_object('model_id', p_model_id, 'ram_gb', p_ram_gb, 'storage_gb', p_storage_gb));
END;
$$;

COMMENT ON PROCEDURE sp_admin_add_specs IS 'Yeni specs ekler - Admin paneli için';

-- =====================================================
-- 6. sp_admin_update_specs: Specs güncelle
-- Kullanım: CALL sp_admin_update_specs(1, 8, 512, 48, 6.1, 3200, NULL);
-- =====================================================
CREATE OR REPLACE PROCEDURE sp_admin_update_specs(
    IN p_specs_id INTEGER,
    IN p_ram_gb INTEGER,
    IN p_storage_gb INTEGER,
    IN p_kamera_mp INTEGER,
    IN p_ekran_boyutu DECIMAL(3,1),
    IN p_batarya_mah INTEGER,
    INOUT p_success BOOLEAN DEFAULT FALSE
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_old_data JSONB;
BEGIN
    -- Eski veriyi al
    SELECT jsonb_build_object(
        'ram_gb', ram_gb, 
        'storage_gb', storage_gb,
        'kamera_mp', kamera_mp,
        'ekran_boyutu', ekran_boyutu,
        'batarya_mah', batarya_mah
    ) INTO v_old_data
    FROM specs WHERE specs_id = p_specs_id;
    
    IF v_old_data IS NULL THEN
        RAISE EXCEPTION 'Specs bulunamadı: %', p_specs_id;
    END IF;
    
    -- Güncelle
    UPDATE specs 
    SET ram_gb = p_ram_gb,
        storage_gb = p_storage_gb,
        kamera_mp = p_kamera_mp,
        ekran_boyutu = p_ekran_boyutu,
        batarya_mah = p_batarya_mah
    WHERE specs_id = p_specs_id;
    
    -- Audit log
    INSERT INTO audit_log (table_name, operation, record_id, old_data, new_data)
    VALUES ('specs', 'UPDATE', p_specs_id, v_old_data,
            jsonb_build_object('ram_gb', p_ram_gb, 'storage_gb', p_storage_gb, 'kamera_mp', p_kamera_mp));
    
    p_success := TRUE;
END;
$$;

COMMENT ON PROCEDURE sp_admin_update_specs IS 'Specs günceller - Admin paneli için';

-- =====================================================
-- 7. sp_admin_add_condition: Durum ekle
-- Kullanım: CALL sp_admin_add_condition('Yeni', 1.10, NULL);
-- =====================================================
CREATE OR REPLACE PROCEDURE sp_admin_add_condition(
    IN p_condition_name VARCHAR(20),
    IN p_multiplier DECIMAL(4,2),
    INOUT p_condition_id INTEGER DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_max_puan INTEGER;
BEGIN
    -- İsim kontrolü
    IF EXISTS (SELECT 1 FROM conditions WHERE LOWER(condition_name) = LOWER(p_condition_name)) THEN
        RAISE EXCEPTION 'Bu durum zaten mevcut: %', p_condition_name;
    END IF;
    
    -- Yeni puan hesapla
    SELECT COALESCE(MAX(condition_puan), 0) + 1 INTO v_max_puan FROM conditions;
    
    -- Durum ekle
    INSERT INTO conditions (condition_name, condition_puan, multiplier, created_at)
    VALUES (p_condition_name, v_max_puan, p_multiplier, CURRENT_TIMESTAMP)
    RETURNING condition_id INTO p_condition_id;
    
    -- Audit log
    INSERT INTO audit_log (table_name, operation, record_id, new_data)
    VALUES ('conditions', 'INSERT', p_condition_id,
            jsonb_build_object('condition_name', p_condition_name, 'multiplier', p_multiplier));
END;
$$;

COMMENT ON PROCEDURE sp_admin_add_condition IS 'Yeni cihaz durumu ekler - Admin paneli için';

-- =====================================================
-- 8. sp_admin_assign_role: Kullanıcıya rol ata
-- Kullanım: CALL sp_admin_assign_role(1, 'Admin', NULL);
-- =====================================================
CREATE OR REPLACE PROCEDURE sp_admin_assign_role(
    IN p_user_id INTEGER,
    IN p_role_name VARCHAR(20),
    INOUT p_success BOOLEAN DEFAULT FALSE
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_role_id INTEGER;
BEGIN
    -- Kullanıcı kontrolü
    IF NOT EXISTS (SELECT 1 FROM users WHERE user_id = p_user_id) THEN
        RAISE EXCEPTION 'Kullanıcı bulunamadı: %', p_user_id;
    END IF;
    
    -- Rol bul
    SELECT role_id INTO v_role_id 
    FROM roles 
    WHERE LOWER(role_name) = LOWER(p_role_name);
    
    IF v_role_id IS NULL THEN
        RAISE EXCEPTION 'Rol bulunamadı: %', p_role_name;
    END IF;
    
    -- Varsa güncelleme yapma (zaten atanmış)
    IF EXISTS (SELECT 1 FROM user_roles WHERE user_id = p_user_id AND role_id = v_role_id) THEN
        p_success := TRUE;
        RETURN;
    END IF;
    
    -- Rol ata
    INSERT INTO user_roles (user_id, role_id, assigned_at)
    VALUES (p_user_id, v_role_id, CURRENT_TIMESTAMP);
    
    -- Audit log
    INSERT INTO audit_log (table_name, operation, record_id, new_data)
    VALUES ('user_roles', 'INSERT', p_user_id,
            jsonb_build_object('user_id', p_user_id, 'role_name', p_role_name));
    
    p_success := TRUE;
END;
$$;

COMMENT ON PROCEDURE sp_admin_assign_role IS 'Kullanıcıya rol atar - Admin paneli için';

-- =====================================================
-- 9. sp_register_user: Yeni kullanıcı kaydı
-- Kullanım: CALL sp_register_user('yeniuser', 'hash123', 'email@test.com', NULL);
-- =====================================================
CREATE OR REPLACE PROCEDURE sp_register_user(
    IN p_username VARCHAR(50),
    IN p_password_hash VARCHAR(255),
    IN p_email VARCHAR(100),
    INOUT p_user_id INTEGER DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_user_role_id INTEGER;
BEGIN
    -- Kullanıcı adı kontrolü
    IF EXISTS (SELECT 1 FROM users WHERE LOWER(username) = LOWER(p_username)) THEN
        RAISE EXCEPTION 'Bu kullanıcı adı zaten mevcut: %', p_username;
    END IF;
    
    -- E-posta kontrolü (eğer verilmişse)
    IF p_email IS NOT NULL AND EXISTS (SELECT 1 FROM users WHERE LOWER(email) = LOWER(p_email)) THEN
        RAISE EXCEPTION 'Bu e-posta zaten kullanılıyor';
    END IF;
    
    -- Kullanıcı ekle
    INSERT INTO users (username, password_hash, email, is_active, created_at, updated_at)
    VALUES (p_username, p_password_hash, p_email, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
    RETURNING user_id INTO p_user_id;
    
    -- Varsayılan olarak "User" rolü ata
    SELECT role_id INTO v_user_role_id FROM roles WHERE role_name = 'User';
    
    INSERT INTO user_roles (user_id, role_id, assigned_at)
    VALUES (p_user_id, v_user_role_id, CURRENT_TIMESTAMP);
    
    -- Audit log
    INSERT INTO audit_log (table_name, operation, record_id, new_data)
    VALUES ('users', 'INSERT', p_user_id,
            jsonb_build_object('username', p_username));
END;
$$;

COMMENT ON PROCEDURE sp_register_user IS 'Yeni kullanıcı kaydeder ve User rolü atar';

-- =====================================================
-- 10. sp_delete_prediction: Tahmin sil
-- Kullanım: CALL sp_delete_prediction(1, 1, NULL);
-- =====================================================
CREATE OR REPLACE PROCEDURE sp_delete_prediction(
    IN p_prediction_id INTEGER,
    IN p_user_id INTEGER,
    INOUT p_success BOOLEAN DEFAULT FALSE
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_old_data JSONB;
BEGIN
    -- Tahmin kontrolü (kullanıcıya ait mi)
    SELECT jsonb_build_object(
        'prediction_id', prediction_id,
        'specs_id', specs_id,
        'predicted_price', predicted_price
    ) INTO v_old_data
    FROM predictions 
    WHERE prediction_id = p_prediction_id AND user_id = p_user_id;
    
    IF v_old_data IS NULL THEN
        RAISE EXCEPTION 'Tahmin bulunamadı veya size ait değil: %', p_prediction_id;
    END IF;
    
    -- Sil
    DELETE FROM predictions WHERE prediction_id = p_prediction_id;
    
    -- Audit log
    INSERT INTO audit_log (table_name, operation, record_id, old_data)
    VALUES ('predictions', 'DELETE', p_prediction_id, v_old_data);
    
    p_success := TRUE;
END;
$$;

COMMENT ON PROCEDURE sp_delete_prediction IS 'Tahmin kaydını siler';

-- =====================================================
-- ÖZET VE KULLANIM
-- =====================================================
/*
STORED PROCEDURES (10 adet) - CALL ile çağrılır:

USER TARAFINDA:
  1. sp_create_prediction    - Tahmin kaydet
  9. sp_register_user        - Kullanıcı kaydı
  10. sp_delete_prediction   - Tahmin sil

ADMIN TARAFINDA:
  2. sp_admin_add_segment    - Segment ekle
  3. sp_admin_add_model      - Model ekle
  4. sp_admin_update_model   - Model güncelle
  5. sp_admin_add_specs      - Specs ekle
  6. sp_admin_update_specs   - Specs güncelle
  7. sp_admin_add_condition  - Durum ekle
  8. sp_admin_assign_role    - Rol ata

ÖRNEK KULLANIMLAR:

  -- Tahmin kaydet (OUT parametre ile ID döner)
  DO $$
  DECLARE
    v_id INTEGER;
  BEGIN
    CALL sp_create_prediction(1, 1, 4, 25000.00, 92.5, v_id);
    RAISE NOTICE 'Oluşturulan tahmin ID: %', v_id;
  END $$;
  
  -- Segment ekle
  CALL sp_admin_add_segment('Ultra', NULL);
  
  -- Model ekle
  CALL sp_admin_add_model('iPhone 17', 'Base', 2025, NULL);
  
  -- Kullanıcı kaydet
  CALL sp_register_user('yeniuser', 'hash123', 'email@test.com', NULL);
  
  -- Model güncelle
  CALL sp_admin_update_model(1, 'iPhone 16 Pro Max', 'Pro Max', 2024, NULL);

BACKEND ENTEGRASYONU (ASP.NET Core + EF Core):

  // Tahmin kaydet
  var predictionId = new NpgsqlParameter("p_prediction_id", DbType.Int32) { Direction = ParameterDirection.InputOutput, Value = DBNull.Value };
  await _context.Database.ExecuteSqlRawAsync(
      "CALL sp_create_prediction(@p_user_id, @p_specs_id, @p_condition_id, @p_predicted_price, @p_confidence_score, @p_prediction_id)",
      new NpgsqlParameter("p_user_id", userId),
      new NpgsqlParameter("p_specs_id", specsId),
      new NpgsqlParameter("p_condition_id", conditionId),
      new NpgsqlParameter("p_predicted_price", predictedPrice),
      new NpgsqlParameter("p_confidence_score", confidenceScore),
      predictionId
  );
  var newId = (int)predictionId.Value;

  // Segment ekle
  await _context.Database.ExecuteSqlRawAsync(
      "CALL sp_admin_add_segment(@p_segment_name, NULL)",
      new NpgsqlParameter("p_segment_name", segmentName)
  );
*/
