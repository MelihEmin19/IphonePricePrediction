# iPhone Fiyat Tahmin Sistemi - Hızlı Başlangıç

## 🚀 5 Dakikada Çalıştırın

### Gereksinimler
- PostgreSQL 15+
- Python 3.10+
- Node.js 18+
- .NET 7.0+ SDK

### 1. Veritabanı Kurulumu
```bash
psql -U postgres
\i database/schema.sql
\i database/stored_procedures.sql
\i database/views.sql
\q
```

### 2. Python Bağımlılıkları
```bash
cd scraper
pip install -r requirements.txt

cd ../ml_service
pip install -r requirements.txt
```

### 3. Node.js Bağımlılıkları
```bash
cd api_service
npm install
```

### 4. Çevre Değişkenleri
Her klasörde `.env` dosyası oluşturun (`.env.example` şablonundan):
```bash
# scraper/.env, ml_service/.env, api_service/.env
DB_HOST=localhost
DB_PORT=5432
DB_NAME=iphone_price_db
DB_USER=postgres
DB_PASSWORD=your_password
```

### 5. Veri ve Model Hazırlama
```bash
# Veri topla
cd scraper
python run_scraper.py

# Model eğit
cd ../ml_service
python train_model.py

# gRPC kodları üret
python generate_grpc.py
```

### 6. Tüm Servisleri Başlat

**Windows:**
```bash
START_ALL.bat
```

**Manuel (3 farklı terminal):**
```bash
# Terminal 1
cd ml_service
python grpc_server.py

# Terminal 2
cd api_service
npm start

# Terminal 3
cd web_app/IphonePriceWeb
dotnet run
```

### 7. Tarayıcıda Aç
- **Ana Sayfa:** http://localhost:5000
- **Admin Panel:** http://localhost:5000/Admin/Panel
- **API:** http://localhost:3000

## 📝 Test Et

### Web Arayüzünden
1. http://localhost:5000 aç
2. Model: iPhone 13
3. RAM: 4 GB, Hafıza: 128 GB
4. Durum: Mükemmel
5. "Fiyat Tahmin Et" tıkla

### API'den
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

## 📊 Beklenen Sonuç
```json
{
  "success": true,
  "data": {
    "prediction": {
      "price_tl": 23000.50,
      "price_usd": 707.71,
      "confidence": 89.5,
      "range": {"min": 21500, "max": 24500}
    }
  }
}
```

## 🔧 Sorun Giderme

### Port zaten kullanımda
```bash
# Windows
netstat -ano | findstr :3000
netstat -ano | findstr :5000
netstat -ano | findstr :50051

# Process'i öldür
taskkill /PID <PID> /F
```

### Model bulunamadı
```bash
cd ml_service
python train_model.py
```

### gRPC bağlantı hatası
```bash
cd ml_service
python generate_grpc.py
python grpc_server.py
```

### Database bağlantı hatası
- PostgreSQL çalışıyor mu? `pg_isready`
- Şifreler doğru mu? `.env` kontrol et

## 📚 Detaylı Dokümantasyon
- **Kurulum:** `docs/SETUP_GUIDE.md`
- **Test:** `docs/TEST_GUIDE.md`
- **Proje Raporu:** `docs/PROJECT_REPORT.md`

## 🎯 Özellikler
✅ ML ile akıllı fiyat tahmini  
✅ gRPC + SOAP + REST API  
✅ PostgreSQL (6 entity, 7 SP, 8 view)  
✅ Admin paneli + istatistikler  
✅ Gerçek zamanlı döviz dönüşümü  
✅ Modern web arayüzü (Bootstrap 5)  

## 📞 Destek
Sorun mu yaşıyorsunuz? `docs/TEST_GUIDE.md` dosyasına bakın.

