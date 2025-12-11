# iPhone Fiyat Tahmin Sistemi - Hızlı Başlangıç

## 🚀 5 Dakikada Çalıştırın

### Gereksinimler
- PostgreSQL 15+
- Python 3.10+
- Node.js 18+
- .NET 9.0 SDK

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
cd ml_service
pip install -r requirements.txt
python train_model.py  # Model eğit
```

### 3. Node.js Bağımlılıkları
```bash
cd api_service
npm install
```

### 4. Tüm Servisleri Başlat

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

### 5. Tarayıcıda Aç
- **Ana Sayfa:** http://localhost:5164
- **Admin Panel:** http://localhost:5164/Admin/Panel
- **API:** http://localhost:3000

## 📝 Test Et

### Web Arayüzünden
1. http://localhost:5164 aç
2. Giriş yap (admin/admin123)
3. Model: iPhone 13
4. RAM: 4 GB, Hafıza: 128 GB
5. Durum: Mükemmel
6. "Fiyat Tahmin Et" tıkla

### API'den
```bash
curl -X POST http://localhost:3000/api/predict \
  -H "Content-Type: application/json" \
  -d '{
    "model_id": 8,
    "ram_gb": 4,
    "storage_gb": 128,
    "condition": "Mükemmel"
  }'
```

## 📊 Beklenen Sonuç
```json
{
  "success": true,
  "data": {
    "prediction": {
      "price_tl": 32574.00,
      "price_usd": 945.50,
      "confidence": 99.0,
      "range": {"min": 31264, "max": 33883}
    }
  }
}
```

## 🔧 Sorun Giderme

### Port zaten kullanımda
```bash
# Windows
netstat -ano | findstr :3000
netstat -ano | findstr :5164
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

## 📚 Detaylı Dokümantasyon
- **Kurulum:** `docs/SETUP_GUIDE.md`
- **Test:** `docs/TEST_GUIDE.md`
- **Proje Raporu:** `docs/PROJECT_REPORT.md`

## 🎯 Özellikler
✅ Gradient Boosting ile %99.88 doğruluk  
✅ gRPC + SOAP + REST API  
✅ PostgreSQL (7 tablo, 7 SP, 10 view)  
✅ Admin paneli + istatistikler  
✅ Gerçek zamanlı döviz dönüşümü  
✅ Modern web arayüzü (Bootstrap 5)  
