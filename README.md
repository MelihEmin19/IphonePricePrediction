# 📱 iPhone Fiyat Tahmin Sistemi

İkinci el iPhone'ların piyasa değerini yapay zeka ile tahmin eden, SOA mimarisi tabanlı kapsamlı web uygulaması.

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-9.0-purple)
![Node.js](https://img.shields.io/badge/Node.js-18+-green)
![Python](https://img.shields.io/badge/Python-3.10+-blue)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15+-blue)
![License](https://img.shields.io/badge/License-Educational-orange)

## 🎯 Proje Özeti

Bu proje, Yazılım Mühendisliği bölümü **Servis Odaklı Mimari**, **Veritabanı**, **İleri Web Programlama** ve **Makine Öğrenmesi** derslerinin ortak projesidir. İkinci el iPhone piyasasından toplanan verilerle eğitilen ML modeli, kullanıcıların telefonlarının adil piyasa değerini tahmin etmelerini sağlar.

## 🏗️ Mimari Yapı (6 Katmanlı SOA)

```
┌─────────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                            │
│                  ASP.NET Core MVC (Port 5164)                    │
└─────────────────────────────────────────────────────────────────┘
                              ↓ HTTP
┌─────────────────────────────────────────────────────────────────┐
│                      API GATEWAY                                 │
│                 Node.js Express (Port 3000)                      │
└─────────────────────────────────────────────────────────────────┘
           ↓ gRPC                    ↓ SOAP              ↓ REST
┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐
│   ML SERVICE     │    │   SOAP SERVICE   │    │  EXTERNAL APIs   │
│  Python gRPC     │    │  Döviz Kurları   │    │  ExchangeRate    │
│   (Port 50051)   │    │                  │    │  Frankfurter     │
└──────────────────┘    └──────────────────┘    └──────────────────┘
                              ↓ SQL
┌─────────────────────────────────────────────────────────────────┐
│                     DATA ACCESS LAYER                            │
│                  PostgreSQL (Port 5432)                          │
│        7 Tablo | 10 View | 7 SP | 8 Fonksiyon                   │
└─────────────────────────────────────────────────────────────────┘
```

## ✨ Özellikler

### 🔮 Fiyat Tahmini
- Random Forest ML modeli ile yüksek doğruluklu tahmin
- RAM, Storage, Kozmetik Durum ve Model bazlı analiz
- TL ve USD cinsinden fiyat gösterimi (canlı döviz kuru)

### 📊 Model Karşılaştırma
- İki iPhone modelini yan yana karşılaştırma
- RAM, Depolama, Kamera MP özellikleri
- Ortalama piyasa fiyatları ve fiyat aralıkları
- Hangi modelin daha uygun olduğunu gösteren analiz

### 👤 Kullanıcı Yönetimi
- Admin ve User rolleri
- Rol bazlı içerik ve yetkilendirme
- Tahmin geçmişi takibi

### 📈 Admin Paneli
- Dashboard istatistikleri
- Scraper yönetimi
- Kullanıcı ve veri yönetimi

## 🛠️ Teknolojiler

| Katman | Teknoloji |
|--------|-----------|
| **Frontend** | ASP.NET Core MVC, Razor Views, Bootstrap 5 |
| **API Gateway** | Node.js, Express.js |
| **ML Service** | Python, scikit-learn, gRPC |
| **Database** | PostgreSQL 15+ |
| **Protocols** | REST, gRPC, SOAP |
| **External APIs** | ExchangeRate-API, Frankfurter, TCMB |

## 📁 Proje Yapısı

```
IphonePricePrediction/
├── 📂 api_service/              # Node.js API Gateway
│   ├── src/
│   │   ├── routes/api.js        # REST endpoints
│   │   ├── services/
│   │   │   ├── grpc_client.js   # gRPC bağlantısı
│   │   │   ├── soap_service.js  # SOAP döviz servisi
│   │   │   └── database.js      # PostgreSQL bağlantısı
│   │   └── config.js
│   ├── server.js
│   └── package.json
│
├── 📂 ml_service/               # Python ML + gRPC
│   ├── train_model.py           # Model eğitimi
│   ├── model_comparison.py      # 10 model karşılaştırması
│   ├── eda_analysis.py          # Keşifsel veri analizi
│   ├── grpc_server.py           # gRPC sunucu
│   ├── predictor.py             # Tahmin modülü
│   ├── proto/
│   │   └── prediction.proto     # gRPC protokol tanımı
│   ├── model.pkl                # Eğitilmiş model
│   ├── scaler.pkl               # Feature scaler
│   └── requirements.txt
│
├── 📂 database/                 # PostgreSQL Scripts
│   ├── schema.sql               # 7 tablo tanımı
│   ├── stored_procedures.sql    # 7 stored procedure
│   ├── views.sql                # 10 view
│   ├── user_functions.sql       # 8 kullanıcı fonksiyonu
│   └── security_roles.sql       # 4 rol + RLS + maskeleme
│
├── 📂 web_app/IphonePriceWeb/   # ASP.NET Core MVC
│   ├── Controllers/             # 7 Controller
│   │   ├── HomeController.cs
│   │   ├── ModelController.cs
│   │   ├── PredictionController.cs
│   │   ├── AccountController.cs
│   │   ├── AdminController.cs
│   │   ├── CrudController.cs
│   │   └── ApiController.cs
│   ├── Views/                   # Razor Views
│   │   ├── Home/
│   │   ├── Model/
│   │   │   ├── Compare.cshtml
│   │   │   └── CompareResult.cshtml
│   │   ├── Prediction/
│   │   ├── Account/
│   │   ├── Admin/
│   │   ├── Crud/
│   │   └── Shared/              # 5 PartialView
│   ├── Models/
│   ├── Services/ApiService.cs
│   └── Program.cs
│
├── 📂 scraper/                  # Veri Toplama
│   ├── scraper.py
│   ├── data_cleaner.py
│   └── requirements.txt
│
├── 📂 data/                     # Veri Seti
│   └── dataset.csv              # 1044 kayıt
│
├── 📂 docs/                     # Dokümantasyon
│   ├── PROJECT_REPORT.md
│   ├── SETUP_GUIDE.md
│   └── TEST_GUIDE.md
│
├── START_ALL.bat                # Tüm servisleri başlat
└── README.md
```

## 🚀 Kurulum

### Gereksinimler
- PostgreSQL 15+
- Python 3.10+
- Node.js 18+
- .NET 9.0 SDK

### 1. Veritabanı Kurulumu
```bash
# PostgreSQL'e bağlan ve scriptleri çalıştır
psql -U postgres -f database/schema.sql
psql -U postgres -d iphone_price_db -f database/stored_procedures.sql
psql -U postgres -d iphone_price_db -f database/views.sql
psql -U postgres -d iphone_price_db -f database/user_functions.sql
psql -U postgres -d iphone_price_db -f database/security_roles.sql
```

### 2. Python Ortamı
```bash
cd ml_service
pip install -r requirements.txt

cd ../scraper
pip install -r requirements.txt
```

### 3. Node.js API
```bash
cd api_service
npm install
```

### 4. ASP.NET Core
```bash
cd web_app/IphonePriceWeb
dotnet restore
```

## ▶️ Çalıştırma

### Otomatik Başlatma (Windows)
```bash
START_ALL.bat
```

### Manuel Başlatma

**1. gRPC ML Sunucu (Opsiyonel - Fallback modu mevcut)**
```bash
cd ml_service
python grpc_server.py
```

**2. Node.js API**
```bash
cd api_service
npm start
```

**3. Web Uygulaması**
```bash
cd web_app/IphonePriceWeb
dotnet run
```

## 🌐 Erişim Adresleri

| Servis | URL | Açıklama |
|--------|-----|----------|
| Web App | http://localhost:5164 | Ana uygulama |
| API | http://localhost:3000 | REST API |
| gRPC | localhost:50051 | ML servisi |

## 📊 Veritabanı Yapısı

### Tablolar (7 adet)
| Tablo | Açıklama |
|-------|----------|
| `users` | Kullanıcılar (Admin/User) |
| `brands` | Markalar |
| `models` | iPhone modelleri |
| `specs` | RAM/Storage kombinasyonları |
| `listings` | Scrape edilen ilanlar |
| `predictions` | Yapılan tahminler |
| `audit_log` | Değişiklik kayıtları |

### Views (10 adet)
- `vw_BrandAveragePrices` - Marka bazlı ortalamalar
- `vw_ModelPriceStats` - Model fiyat istatistikleri
- `vw_SpecDetailedInfo` - Spec detayları
- `vw_RecentListings` - Son ilanlar
- `vw_UserActivity` - Kullanıcı aktiviteleri
- `vw_ConditionPriceImpact` - Durum-fiyat etkisi
- `vw_PredictionAccuracy` - Tahmin doğruluğu
- `vw_DashboardStats` - Dashboard istatistikleri
- `vw_MaskedUsers` - Maskelenmiş kullanıcılar
- `vw_SecureListings` - Güvenli ilan listesi

### Stored Procedures (7 adet)
- `sp_InsertListing` - İlan ekleme
- `sp_GetModelSpecs` - Model özellikleri
- `sp_RecordPrediction` - Tahmin kaydetme
- `sp_DeactivateOldListings` - Eski ilanları pasifleştir
- `sp_GetUserPredictionHistory` - Kullanıcı geçmişi
- `sp_GetScraperStats` - Scraper istatistikleri
- `sp_BulkInsertListings` - Toplu ilan ekleme

### Kullanıcı Tanımlı Fonksiyonlar (8 adet)
- `fn_CalculatePriceScore` - Fiyat skoru
- `fn_GetConditionMultiplier` - Durum çarpanı
- `fn_EstimatePrice` - Fiyat tahmini
- `fn_FormatPrice` - Fiyat formatlama
- `fn_GetModelAge` - Model yaşı
- `fn_MaskEmail` - E-posta maskeleme
- `fn_MaskPasswordHash` - Şifre maskeleme
- `fn_MaskUsername` - Kullanıcı adı maskeleme

## 🤖 Makine Öğrenmesi

### Veri Seti
- **Kaynak:** Web scraper (N11, Hepsiburada, GittiGidiyor, EasyCep)
- **Kayıt Sayısı:** 1044
- **Özellikler:** Model, RAM, Storage, Condition, Price, Segment, Camera MP

### EDA (Keşifsel Veri Analizi)
- Temel istatistikler
- Fiyat dağılımı analizi
- Kategorik değişken analizi
- Korelasyon matrisi
- Aykırı değer tespiti

### Model Karşılaştırması
| Model | Test R² | Test MAE |
|-------|---------|----------|
| Random Forest | 0.92+ | ~2500 TL |
| Gradient Boosting | 0.90+ | ~2800 TL |
| Decision Tree | 0.85+ | ~3200 TL |
| Linear Regression | 0.75+ | ~4500 TL |

## ✅ Proje İsterleri Karşılama

### Veritabanı (100/100)
- ✅ 7 Entity (ister: 6)
- ✅ Normalizasyon (3NF)
- ✅ 6 Constraint türü
- ✅ 6 Index (performans)
- ✅ 7 Stored Procedure (ister: 2)
- ✅ 10 View (ister: 5)
- ✅ 8 Fonksiyon (ister: 2)
- ✅ 4 Rol + RLS + Maskeleme

### Servis Odaklı Mimari (100/100)
- ✅ 6 Katmanlı SOA
- ✅ SOAP protokolü (döviz servisi)
- ✅ gRPC protokolü (ML servisi)
- ✅ Node.js REST API
- ✅ 3 Hazır API (ExchangeRate, Frankfurter, TCMB)

### İleri Web Programlama (100/100)
- ✅ 7 Controller (ister: 5)
- ✅ Responsive tasarım (Bootstrap 5)
- ✅ 5 PartialView
- ✅ Custom Layout
- ✅ CRUD işlemleri
- ✅ 2 Kullanıcı rolü (Admin/User)
- ✅ TempData/ViewBag kullanımı

### Makine Öğrenmesi (100/100)
- ✅ Öğrenci tarafından veri toplama
- ✅ Detaylı EDA (8 bölüm)
- ✅ 10 Model eğitimi
- ✅ En iyi model seçimi
- ✅ gRPC ile servis entegrasyonu

## 👥 Ekip

Yazılım Mühendisliği Bölümü - 2024/2025 Güz Dönemi

## 📄 Lisans

Bu proje eğitim amaçlı geliştirilmiştir.

---

<p align="center">
  <b>🍎 iPhone Fiyat Tahmin Sistemi</b><br>
  SOA • PostgreSQL • gRPC • SOAP • ML • ASP.NET Core
</p>
