# 🚀 Hızlı Kurulum Rehberi

## Gereksinimler

- **PostgreSQL 15+** → [İndir](https://www.postgresql.org/download/)
- **Python 3.10+** → [İndir](https://www.python.org/downloads/)
- **Node.js 18+** → [İndir](https://nodejs.org/)
- **.NET 9.0 SDK** → [İndir](https://dotnet.microsoft.com/download)

---

## Adım 1: Projeyi İndir

```bash
git clone https://github.com/MelihEmin19/IphonePricePrediction.git
cd IphonePricePrediction
```

---

## Adım 2: PostgreSQL Kurulumu

1. PostgreSQL 15+ indir ve kur
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

## Adım 4: Bağımlılıkları Yükle

```bash
# Python paketleri
cd ml_service
pip install -r requirements.txt

# Node.js paketleri
cd ../api_service
npm install
```

---

## Adım 5: Model Eğit

```bash
cd ml_service
python train_model.py
```

Bu işlem 3 algoritma test eder ve en iyisini seçer (Gradient Boosting, R² = 0.9988).

---

## Adım 6: Servisleri Başlat

### Otomatik (Windows):
```bash
START_ALL.bat
```

### Manuel:

**Terminal 1 - Python gRPC:**
```bash
cd ml_service
python grpc_server.py
```

**Terminal 2 - Node.js API:**
```bash
cd api_service
npm start
```

**Terminal 3 - ASP.NET Web:**
```bash
cd web_app/IphonePriceWeb
dotnet run
```

---

## Adım 7: Test Et

- **Web Uygulaması:** http://localhost:5164
- **API:** http://localhost:3000
- **Admin Panel:** http://localhost:5164/Admin/Panel

### Giriş Bilgileri
- **Admin:** admin / admin123
- **Kullanıcı:** Kayıt ol

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
