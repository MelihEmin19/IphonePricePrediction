# 🚀 Hızlı Kurulum Rehberi

## Gereksinimler

- **PostgreSQL 16+** → [İndir](https://www.postgresql.org/download/)
- **Python 3.10+** → [İndir](https://www.python.org/downloads/)
- **Node.js 18+** → [İndir](https://nodejs.org/)
- **.NET 7.0+ SDK** → [İndir](https://dotnet.microsoft.com/download)

---

## Adım 1: Projeyi İndir

```bash
git clone https://github.com/MelihEmin19/IphonePricePrediction.git
cd IphonePricePrediction
```

---

## Adım 2: PostgreSQL Kurulumu

1. PostgreSQL 16 indir ve kur
2. Kurulum sırasında:
   - Şifre: `postgres123`
   - Port: `5432`

---

## Adım 3: Veritabanı Oluştur

```bash
# PowerShell'de (Windows)
$env:PGPASSWORD="postgres123"
& "C:\Program Files\PostgreSQL\16\bin\psql.exe" -U postgres -f database/schema.sql
& "C:\Program Files\PostgreSQL\16\bin\psql.exe" -U postgres -d iphone_price_db -f database/stored_procedures.sql
& "C:\Program Files\PostgreSQL\16\bin\psql.exe" -U postgres -d iphone_price_db -f database/views.sql
```

---

## Adım 4: .env Dosyalarını Oluştur

### scraper/.env
```
DB_HOST=localhost
DB_PORT=5432
DB_NAME=iphone_price_db
DB_USER=postgres
DB_PASSWORD=postgres123
```

### ml_service/.env
```
DB_HOST=localhost
DB_PORT=5432
DB_NAME=iphone_price_db
DB_USER=postgres
DB_PASSWORD=postgres123
```

### api_service/.env
```
PORT=3000
NODE_ENV=development
DB_HOST=localhost
DB_PORT=5432
DB_NAME=iphone_price_db
DB_USER=postgres
DB_PASSWORD=postgres123
GRPC_HOST=localhost
GRPC_PORT=50051
EXCHANGE_RATE_API=https://www.tcmb.gov.tr/kurlar/today.xml
```

---

## Adım 5: Bağımlılıkları Yükle

```bash
# Python paketleri
cd scraper
pip install -r requirements.txt

cd ../ml_service
pip install -r requirements.txt

# Node.js paketleri
cd ../api_service
npm install
```

---

## Adım 6: Veri Topla ve Model Eğit

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

---

## Adım 7: Servisleri Başlat

### Terminal 1 - Python gRPC:
```bash
cd ml_service
python grpc_server.py
```

### Terminal 2 - Node.js API:
```bash
cd api_service
npm start
```

### Terminal 3 - ASP.NET Web:
```bash
cd web_app/IphonePriceWeb
dotnet run --urls=http://localhost:5050
```

---

## Adım 8: Test Et

- **Web Uygulaması:** http://localhost:5050
- **API:** http://localhost:3000
- **Admin Panel:** http://localhost:5050/Admin/Panel

---

## ❓ Sık Karşılaşılan Hatalar

| Hata | Çözüm |
|------|-------|
| `psql: command not found` | PostgreSQL'i PATH'e ekle veya tam path kullan |
| `ModuleNotFoundError` | `pip install -r requirements.txt` |
| `Cannot find module` | `npm install` |
| `Connection refused (5432)` | PostgreSQL servisini başlat |
| `EADDRINUSE` | Port kullanan process'i kapat |
| `model.pkl not found` | `python train_model.py` çalıştır |

---

## 📞 Destek

Sorun yaşarsanız `docs/` klasöründeki dokümanlara bakın:
- `SETUP_GUIDE.md` - Detaylı kurulum
- `TEST_GUIDE.md` - Test senaryoları
- `PROJECT_REPORT.md` - Proje raporu

---

**İyi çalışmalar! 🎉**

