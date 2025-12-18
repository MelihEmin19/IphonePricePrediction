-- =====================================================
-- SEED DATA - CSV'den Veritabanına Veri Yükleme
-- Bu script CSV verilerinden models ve specs tablolarını doldurur
-- =====================================================

-- NOT: Bu script schema.sql'den SONRA çalıştırılmalıdır.
-- CSV'deki veriler şu kolonlara sahip:
-- id,cihaz_isim,ram_gb,kamera_mp,ekran_boyutu,batarya_mah,storage_gb,cihaz_fiyat,cikis_yili,cihaz_durum_puan,segment_puan,model_kodu

-- =====================================================
-- 1. CSV'den DISTINCT modelleri çıkar ve ekle
-- =====================================================

-- Önce temp tablo ile CSV'yi yükle
CREATE TEMP TABLE temp_csv_data (
    id INTEGER,
    cihaz_isim VARCHAR(100),
    ram_gb INTEGER,
    kamera_mp INTEGER,
    ekran_boyutu DECIMAL(3,1),
    batarya_mah INTEGER,
    storage_gb INTEGER,
    cihaz_fiyat DECIMAL(10,2),
    cikis_yili INTEGER,
    cihaz_durum_puan INTEGER,
    segment_puan INTEGER,
    model_kodu INTEGER
);

-- CSV'yi yükle (Python veya psql \copy ile yapılacak)
-- \copy temp_csv_data FROM '../data/dataset.csv' WITH (FORMAT csv, HEADER true);

-- =====================================================
-- 2. Modelleri ekle (segment_puan'a göre segment_id belirle)
-- =====================================================
INSERT INTO models (model_name, segment_id, release_year, model_kodu)
SELECT DISTINCT 
    t.cihaz_isim,
    t.segment_puan, -- segment_puan = segment_id (1-5)
    t.cikis_yili,
    t.model_kodu
FROM temp_csv_data t
WHERE NOT EXISTS (
    SELECT 1 FROM models m 
    WHERE m.model_name = t.cihaz_isim AND m.segment_id = t.segment_puan
)
ON CONFLICT DO NOTHING;

-- =====================================================
-- 3. Specs'leri ekle (her model için benzersiz kombinasyonlar)
-- =====================================================
INSERT INTO specs (model_id, ram_gb, storage_gb, kamera_mp, ekran_boyutu, batarya_mah)
SELECT DISTINCT 
    m.model_id,
    t.ram_gb,
    t.storage_gb,
    t.kamera_mp,
    t.ekran_boyutu,
    t.batarya_mah
FROM temp_csv_data t
JOIN models m ON m.model_name = t.cihaz_isim AND m.segment_id = t.segment_puan
WHERE NOT EXISTS (
    SELECT 1 FROM specs s 
    WHERE s.model_id = m.model_id 
    AND s.ram_gb = t.ram_gb 
    AND s.storage_gb = t.storage_gb
)
ON CONFLICT DO NOTHING;

-- Temp tabloyu temizle
DROP TABLE IF EXISTS temp_csv_data;

-- =====================================================
-- ÖZET
-- =====================================================
/*
Bu script CSV'deki verileri:
1. Models tablosuna benzersiz model isimlerini ekler
2. Specs tablosuna her model için RAM/Storage kombinasyonlarını ekler

Segment eşleştirmesi:
  segment_puan 1 = Mini (segment_id: 1)
  segment_puan 2 = Base (segment_id: 2)
  segment_puan 3 = Plus (segment_id: 3)
  segment_puan 4 = Pro (segment_id: 4)
  segment_puan 5 = Pro Max (segment_id: 5)

Condition eşleştirmesi:
  cihaz_durum_puan 1 = Outlet
  cihaz_durum_puan 2 = İyi
  cihaz_durum_puan 3 = Çok İyi
  cihaz_durum_puan 4 = Mükemmel
*/

