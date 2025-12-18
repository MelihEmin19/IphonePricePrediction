# iPhone Fiyat Tahmin Sistemi - Kurulum Kılavuzu

## Gereksinimler

### Yazılımlar
- **PostgreSQL 15+** (Veritabanı)
- **Python 3.10+** (ML ve Scraper)
- **Node.js 18+** (API Middleware)
- **.NET 7.0+ SDK** (Web Uygulaması)

### Python Kütüphaneleri
```bash
pip install -r scraper/requirements.txt
pip install -r ml_service/requirements.txt
```

### Node.js Paketleri
```bash
cd api_service
npm install
```

## Adım Adım Kurulum

### 1. PostgreSQL Kurulumu ve Veritabanı Oluşturma

```bash
# PostgreSQL'e bağlan
psql -U postgres

# Veritabanını oluştur
\i database/schema.sql
\i database/stored_procedures.sql
\i database/views.sql

# Çıkış
\q
```

### 2. Çevre Değişkenleri (.env)

Her serviste `.env` dosyası oluşturun:

**scraper/.env**
```
DB_HOST=localhost
DB_PORT=5432
DB_NAME=iphone_price_db
DB_USER=postgres
DB_PASSWORD=your_password
```

**ml_service/.env** (aynı)

**api_service/.env**
```
PORT=3000
DB_HOST=localhost
DB_PORT=5432
DB_NAME=iphone_price_db
DB_USER=postgres
DB_PASSWORD=your_password
GRPC_HOST=localhost
GRPC_PORT=50051
```

### 3. Veri Toplama (Scraping)

```bash
cd scraper
python run_scraper.py
```

Bu adım veritabanına mock veri ekler (gerçek scraping için yasal izin gerekir).

### 4. ML Model Eğitimi

```bash
cd ml_service
python train_model.py
```

Bu adım `model.pkl` ve `scaler.pkl` dosyalarını oluşturur.

### 5. gRPC Kodu Üretimi

```bash
cd ml_service
python generate_grpc.py
```

Proto dosyasından Python gRPC kodları üretilir.

### 6. Servisleri Başlatma

**Terminal 1: gRPC Sunucu (Python ML)**
```bash
cd ml_service
python grpc_server.py
```

**Terminal 2: Node.js API**
```bash
cd api_service
npm start
```

**Terminal 3: ASP.NET Core Web**
```bash
cd web_app/IphonePriceWeb
dotnet run
```

### 7. Uygulamayı Kullanma

- Web Uygulaması: http://localhost:5000
- Node.js API: http://localhost:3000
- Admin Paneli: http://localhost:5000/Admin/Panel

## Test Etme

### API Health Check
```bash
curl http://localhost:3000/api/health
```

### Fiyat Tahmini (API)
```bash
curl -X POST http://localhost:3000/api/predict \
  -H "Content-Type: application/json" \
  -d '{
    "model_id": 8,
    "ram_gb": 4,
    "storage_gb": 128,
    "condition": "Mükemmel",
    "release_year": 2021
  }'
```

### SOAP Servisi
```bash
curl "http://localhost:3000/soap/convert?amount_tl=15000"
```

## Sorun Giderme

### PostgreSQL Bağlantı Hatası
- PostgreSQL servisinin çalıştığından emin olun
- `.env` dosyalarındaki şifreleri kontrol edin
- Firewall ayarlarını kontrol edin

### gRPC Bağlantı Hatası
- ML sunucusunun (port 50051) çalıştığından emin olun
- `generate_grpc.py` çalıştırılmış mı kontrol edin
- Proto dosyasının yolu doğru mu kontrol edin

### Model Bulunamadı Hatası
- `train_model.py` çalıştırılmış mı kontrol edin
- `model.pkl` dosyası var mı kontrol edin
- Önce scraper ile veri toplanmış mı kontrol edin

### ASP.NET Port Çakışması
- Port 5000 kullanımda ise `launchSettings.json`'dan değiştirin

## Port Yapısı

| Servis | Port | Açıklama |
|--------|------|----------|
| PostgreSQL | 5432 | Veritabanı |
| Python gRPC | 50051 | ML Tahmin Servisi |
| Node.js API | 3000 | Middleware API |
| ASP.NET Web | 5000 | Web Arayüzü |

## Proje Yapısı

```
IphonePricePrediction/
├── database/           # SQL şemaları
├── scraper/            # Web scraper
├── ml_service/         # ML + gRPC
├── api_service/        # Node.js API
├── web_app/            # ASP.NET MVC
└── docs/               # Dokümantasyon
```

## Önemli Notlar

1. **Scraper:** Mock veri kullanır (yasal koruma). Gerçek scraping için site sahiplerinden izin alın.
2. **Model:** En az 100 kayıtla eğitin, 1000+ kayıt önerilir.
3. **gRPC:** Önce proto kodlarını üretin (generate_grpc.py).
4. **Güvenlik:** Production'da şifreleri environment variable olarak kullanın.

## Destek

Sorun yaşarsanız:
1. Log dosyalarını kontrol edin
2. Her servisin sağlık durumunu kontrol edin
3. Port çakışması olmadığından emin olun

