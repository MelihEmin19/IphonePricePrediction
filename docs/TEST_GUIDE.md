# Test Kılavuzu

## Manuel Test Senaryoları

### 1. Veritabanı Testi

```bash
# PostgreSQL'e bağlan
psql -U postgres -d iphone_price_db

# Tabloları kontrol et
\dt

# Veri sayısını kontrol et
SELECT COUNT(*) FROM listings;
SELECT COUNT(*) FROM predictions;

# View'ları test et
SELECT * FROM vw_BrandAveragePrices;
SELECT * FROM vw_DashboardStats;

# Stored procedure test et
SELECT sp_GetScraperStats();
```

### 2. Python ML Servisi Testi

```bash
cd ml_service

# Model eğitimini test et
python train_model.py

# Predictor'ı test et
python predictor.py

# gRPC sunucuyu başlat
python grpc_server.py
```

### 3. Node.js API Testi

```bash
cd api_service

# Servisi başlat
npm start

# Başka bir terminalde:

# Health check
curl http://localhost:3000/api/health

# Markaları getir
curl http://localhost:3000/api/brands

# Modelleri getir
curl http://localhost:3000/api/models

# Fiyat tahmini
curl -X POST http://localhost:3000/api/predict \
  -H "Content-Type: application/json" \
  -d '{
    "model_id": 8,
    "ram_gb": 4,
    "storage_gb": 128,
    "condition": "Mükemmel",
    "release_year": 2021
  }'

# Dashboard istatistikleri
curl http://localhost:3000/api/stats/dashboard

# SOAP servisi
curl "http://localhost:3000/soap/convert?amount_tl=15000"
```

### 4. ASP.NET Web Uygulaması Testi

```bash
cd web_app/IphonePriceWeb
dotnet run
```

Tarayıcıda:
- http://localhost:5000 - Ana sayfa
- http://localhost:5000/Admin/Panel - Admin paneli

**Test Adımları:**
1. Ana sayfada model seç
2. RAM ve hafıza seç
3. Kozmetik durum seç
4. "Fiyat Tahmin Et" butonuna tıkla
5. Sonuç sayfasını kontrol et
6. Admin paneline git
7. İstatistikleri kontrol et

## Entegrasyon Testi

### Tam Stack Test

1. **Servisleri Sırayla Başlat**

```bash
# Terminal 1: PostgreSQL
# (Zaten çalışıyor olmalı)

# Terminal 2: Python gRPC
cd ml_service
python grpc_server.py

# Terminal 3: Node.js API
cd api_service
npm start

# Terminal 4: ASP.NET Web
cd web_app/IphonePriceWeb
dotnet run
```

2. **Test Senaryosu: Uçtan Uca Fiyat Tahmini**

**Adım 1:** Web arayüzünden tahmin isteği
- http://localhost:5000
- iPhone 13, 128GB, Mükemmel seç
- Tahmin et

**Adım 2:** İstek akışı
```
Browser → ASP.NET (5000) → Node.js API (3000) → gRPC (50051) → Python ML
                                ↓
                          PostgreSQL (5432)
                                ↓
                          SOAP (Döviz)
```

**Adım 3:** Sonuç kontrolü
- Tahmin fiyatı göründü mü?
- USD karşılığı hesaplandı mı?
- Güven skoru gösterildi mi?
- Fiyat aralığı doğru mu?

**Adım 4:** Admin paneli kontrolü
- http://localhost:5000/Admin/Panel
- İstatistikler güncel mi?
- Tahmin sayısı arttı mı?

### 3. Log Kontrolü

Her serviste logları kontrol edin:

```bash
# Python scraper
cat scraper/scraper.log

# Node.js API
# Terminal çıktısını izleyin

# ASP.NET
# Terminal çıktısını izleyin
```

## Performans Testi

### API Yanıt Süreleri

```bash
# Tahmin endpoint'i
time curl -X POST http://localhost:3000/api/predict \
  -H "Content-Type: application/json" \
  -d '{"model_id": 8, "ram_gb": 4, "storage_gb": 128, "condition": "Mükemmel", "release_year": 2021}'

# Beklenen: < 1 saniye
```

### Çoklu İstek Testi

```bash
# 10 paralel istek
for i in {1..10}; do
  curl -X POST http://localhost:3000/api/predict \
    -H "Content-Type: application/json" \
    -d '{"model_id": 8, "ram_gb": 4, "storage_gb": 128, "condition": "Mükemmel", "release_year": 2021}' &
done
wait
```

## Hata Senaryoları

### 1. ML Servisi Çalışmıyor
```bash
# Node.js API yanıtı:
{
  "success": false,
  "error": "gRPC connection failed"
}
```

**Çözüm:** ML servisini başlatın

### 2. Veritabanı Bağlantı Hatası
```bash
# Node.js API yanıtı:
{
  "success": false,
  "error": "Database connection error"
}
```

**Çözüm:** PostgreSQL çalışıyor mu kontrol edin

### 3. Model Dosyası Yok
```bash
# ML servisi hatası:
FileNotFoundError: model.pkl not found
```

**Çözüm:** Model eğitimini çalıştırın

## Test Checklist

### Veritabanı
- [ ] Schema yüklendi
- [ ] Stored procedures çalışıyor
- [ ] View'lar doğru sonuç veriyor
- [ ] Mock veri eklendi

### Python ML
- [ ] Model eğitildi
- [ ] Predictor çalışıyor
- [ ] gRPC sunucu başladı
- [ ] Health check başarılı

### Node.js API
- [ ] Sunucu başladı (port 3000)
- [ ] Health check geçti
- [ ] Database bağlantısı var
- [ ] gRPC bağlantısı var
- [ ] SOAP servisi çalışıyor
- [ ] Tüm endpoint'ler yanıt veriyor

### ASP.NET Web
- [ ] Sunucu başladı (port 5000)
- [ ] Ana sayfa yükleniyor
- [ ] Dropdown'lar dolu
- [ ] Form submit çalışıyor
- [ ] Sonuç sayfası gösteriliyor
- [ ] Admin paneli çalışıyor

### Entegrasyon
- [ ] Uçtan uca tahmin çalışıyor
- [ ] Tüm servisler haberleşiyor
- [ ] Veriler database'e kaydediliyor
- [ ] Loglar düzgün yazılıyor

## Beklenen Sonuçlar

### Başarılı Tahmin Örneği

**Input:**
```json
{
  "model_id": 8,
  "ram_gb": 4,
  "storage_gb": 128,
  "condition": "Mükemmel",
  "release_year": 2021
}
```

**Output:**
```json
{
  "success": true,
  "data": {
    "prediction": {
      "price_tl": 23000.50,
      "price_usd": 707.71,
      "confidence": 89.5,
      "range": {
        "min": 21500.00,
        "max": 24500.00
      }
    },
    "exchange_rate": 32.50
  }
}
```

## Sorun Giderme

### Port Çakışması
```bash
# Port kullanımını kontrol et
netstat -ano | findstr :3000
netstat -ano | findstr :5000
netstat -ano | findstr :50051
```

### Service Status
```bash
# PostgreSQL
pg_isready

# Python processes
ps aux | grep python

# Node.js processes
ps aux | grep node

# .NET processes
ps aux | grep dotnet
```

