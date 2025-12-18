# 📱 iPhone Fiyat Tahmin Sistemi

İkinci el iPhone'ların piyasa değerini yapay zeka ile tahmin eden, SOA mimarisi tabanlı kapsamlı web uygulaması.

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-9.0-purple)
![Node.js](https://img.shields.io/badge/Node.js-18+-green)
![Python](https://img.shields.io/badge/Python-3.10+-blue)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15+-blue)
![License](https://img.shields.io/badge/License-Educational-orange)

## 🎯 Proje Özeti

Bu proje, Yazılım Mühendisliği bölümü **Servis Odaklı Mimari**, **Veritabanı**, **İleri Web Programlama** ve **Makine Öğrenmesi** derslerinin ortak projesidir. İkinci el iPhone piyasasından toplanan gerçek verilerle eğitilen ML modeli, kullanıcıların telefonlarının adil piyasa değerini tahmin etmelerini sağlar.

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
│        9 Tablo | 12 View | 10 SP | 10 Fonksiyon                 │
└─────────────────────────────────────────────────────────────────┘
```

## ✨ Özellikler

### 🔮 Fiyat Tahmini
- **Gradient Boosting** ML modeli ile %99.88 R² doğruluk
- **Cascade Dropdown**: Önce Model seç → Sonra Depolama seç
- TL ve USD cinsinden fiyat gösterimi (canlı döviz kuru)
- Ortalama hata: **655 TL** (MAE)

### 📊 Model Karşılaştırma
- İki iPhone modelini yan yana karşılaştırma
- RAM, Depolama, Kamera MP özellikleri
- Ortalama piyasa fiyatları ve fiyat aralıkları
- Hangi modelin daha uygun olduğunu gösteren analiz

### 👤 Kullanıcı Yönetimi
- Admin ve User rolleri
- Rol bazlı içerik ve yetkilendirme
- SHA256 şifre hashleme
- Kayıt/Giriş sistemi

### 📈 Admin Paneli
- Dashboard istatistikleri (v_dashboard_stats)
- Segment, Model, Specs, Condition yönetimi
- Kullanıcı ve rol yönetimi
- Tahmin geçmişi görüntüleme

## 🛠️ Teknolojiler

| Katman | Teknoloji |
|--------|-----------|
| **Frontend** | ASP.NET Core MVC, Razor Views, Bootstrap 5 |
| **API Gateway** | Node.js, Express.js |
| **ML Service** | Python, scikit-learn, gRPC |
| **Database** | PostgreSQL 15+ |
| **Protocols** | REST, gRPC, SOAP |
| **External APIs** | ExchangeRate-API, Frankfurter |

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
│   ├── train_model.py           # 5 Model eğitimi ve karşılaştırma
│   ├── grpc_server.py           # gRPC sunucu
│   ├── predictor.py             # Tahmin modülü
│   ├── config.py                # Model bilgileri (28 iPhone)
│   ├── proto/
│   │   └── prediction.proto     # gRPC protokol tanımı
│   ├── models/
│   │   ├── price_model.pkl      # Gradient Boosting model
│   │   ├── scaler.pkl           # Feature scaler
│   │   └── model_config.pkl     # Model konfigürasyonu
│   └── requirements.txt
│
├── 📂 database/                 # PostgreSQL Scripts
│   ├── schema.sql               # 9 tablo tanımı
│   ├── init_db.py               # Otomatik DB kurulumu
│   ├── stored_procedures.sql    # 10 stored procedure
│   ├── views.sql                # 12 view
│   ├── user_functions.sql       # 10 kullanıcı fonksiyonu
│   └── security_roles.sql       # 4 rol + RLS + maskeleme
│
├── 📂 web_app/IphonePriceWeb/   # ASP.NET Core MVC
│   ├── Controllers/             # 7 Controller
│   │   ├── HomeController.cs    # Fiyat tahmini
│   │   ├── AdminController.cs   # Admin yönetimi
│   │   ├── AccountController.cs # Kullanıcı işlemleri
│   │   └── ...
│   ├── Views/                   # Razor Views
│   │   ├── Home/                # Ana sayfa
│   │   ├── Admin/               # Admin paneli (12 view)
│   │   ├── History/             # Tahmin geçmişi
│   │   └── ...
│   ├── Models/
│   ├── Services/ApiService.cs
│   └── Program.cs
│
├── 📂 data/                     # Veri Seti
│   └── dataset.csv              # 1198 gerçek kayıt
│
├── 📂 docs/                     # Dokümantasyon
│   ├── PROJECT_REPORT.md
│   ├── SETUP_GUIDE.md
│   └── TEST_GUIDE.md
│
├── INSTALL.md                   # Kurulum kılavuzu
├── START_ALL.bat                # Tüm servisleri başlat
└── README.md
```

## 🚀 Kurulum

### Gereksinimler
- PostgreSQL 15+
- Python 3.10+
- Node.js 18+
- .NET 9.0 SDK

### Hızlı Kurulum (Otomatik)

```bash
# 1. Veritabanı kurulumu
cd database
python init_db.py

# 2. Python bağımlılıkları
cd ../ml_service
pip install -r requirements.txt

# 3. Node.js bağımlılıkları
cd ../api_service
npm install

# 4. .NET bağımlılıkları
cd ../web_app/IphonePriceWeb
dotnet restore
```

### Manuel Veritabanı Kurulumu

```bash
psql -U postgres -f database/schema.sql
psql -U postgres -d iphone_price_db -f database/user_functions.sql
psql -U postgres -d iphone_price_db -f database/views.sql
psql -U postgres -d iphone_price_db -f database/stored_procedures.sql
```

## ▶️ Çalıştırma

### Otomatik Başlatma (Windows)
```bash
START_ALL.bat
```

### Manuel Başlatma

**1. gRPC ML Sunucu**
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

### Test Kullanıcıları
| Kullanıcı | Şifre | Rol |
|-----------|-------|-----|
| admin | admin123 | Admin |

## 📊 Veritabanı Yapısı

### Tablolar (9 adet)
| Tablo | Açıklama |
|-------|----------|
| `users` | Kullanıcılar |
| `roles` | Roller (Admin/User) |
| `user_roles` | Kullanıcı-Rol ilişkisi |
| `segments` | iPhone segmentleri (Mini/Base/Plus/Pro/Pro Max) |
| `conditions` | Cihaz durumları (Outlet/İyi/Çok İyi/Mükemmel) |
| `models` | iPhone modelleri (28 model) |
| `specs` | RAM/Storage kombinasyonları (81 specs) |
| `predictions` | Yapılan tahminler |
| `audit_log` | Değişiklik kayıtları |

### Views (12 adet)
- `v_specs_catalog` - Specs dropdown için
- `v_condition_catalog` - Condition dropdown için
- `v_segments_catalog` - Segment listesi
- `v_models_catalog` - Model listesi
- `v_user_history_masked` - Maskelenmiş tahmin geçmişi
- `v_admin_model_stats` - Model bazlı istatistikler
- `v_admin_condition_stats` - Durum bazlı istatistikler
- `v_users_masked` - Maskelenmiş kullanıcılar
- `v_dashboard_stats` - Dashboard istatistikleri
- `v_user_roles_detail` - Kullanıcı rol detayları
- `v_prediction_details` - Tahmin detayları
- `v_segment_stats` - Segment istatistikleri

### Stored Procedures (10 adet)
- `sp_create_prediction` - Tahmin kaydetme
- `sp_admin_add_segment` - Segment ekleme
- `sp_admin_add_model` - Model ekleme
- `sp_admin_update_model` - Model güncelleme
- `sp_admin_add_specs` - Specs ekleme
- `sp_admin_update_specs` - Specs güncelleme
- `sp_admin_add_condition` - Condition ekleme
- `sp_admin_assign_role` - Rol atama
- `sp_get_user_history` - Kullanıcı geçmişi
- `sp_register_user` - Kullanıcı kaydı

### Kullanıcı Tanımlı Fonksiyonlar (10 adet)
- `fn_specs_label` - Specs etiketi oluşturma
- `fn_mask_username` - Kullanıcı adı maskeleme
- `fn_mask_email` - E-posta maskeleme
- `fn_get_condition_multiplier` - Durum çarpanı
- `fn_get_segment_name` - Segment adı
- `fn_get_user_role` - Kullanıcı rolü
- `fn_format_price` - Fiyat formatlama
- `fn_calculate_price_score` - Fiyat skoru
- `fn_get_model_age` - Model yaşı
- `fn_estimate_price` - Fiyat tahmini

## 🤖 Makine Öğrenmesi

### Veri Seti
- **Kaynak:** Gerçek e-ticaret sitelerinden toplanan veriler
- **Kayıt Sayısı:** 1198
- **Özellikler:** Model, RAM, Storage, Condition, Price, Segment, Camera MP, Batarya, Ekran

### Model Karşılaştırması (5 Algoritma)
| Model | R² Score | MAE | RMSE |
|-------|----------|-----|------|
| **Gradient Boosting** | **0.9988** | **655 TL** | **1,057 TL** |
| Random Forest | 0.9982 | 800 TL | 1,274 TL |
| Decision Tree | 0.9970 | 1,100 TL | 1,650 TL |
| Ridge Regression | 0.9563 | 4,811 TL | 6,253 TL |
| Linear Regression | 0.9560 | 4,850 TL | 6,300 TL |

**En İyi Model:** Gradient Boosting (%99.88 doğruluk)

### Özellik Önemliliği
1. Kamera MP (71.4%)
2. RAM (15.3%)
3. Ekran Boyutu (4.1%)
4. Batarya (2.9%)
5. Çıkış Yılı (2.7%)

## ✅ Proje İsterleri Karşılama

### Veritabanı (100/100)
- ✅ 9 Entity (ister: 6)
- ✅ Normalizasyon (3NF)
- ✅ 6 Constraint türü
- ✅ 6 Index (performans)
- ✅ 10 Stored Procedure (ister: 2)
- ✅ 12 View (ister: 5)
- ✅ 10 Fonksiyon (ister: 2)
- ✅ 4 Rol + RLS + Maskeleme

### Servis Odaklı Mimari (100/100)
- ✅ 6 Katmanlı SOA
- ✅ SOAP protokolü (döviz servisi)
- ✅ gRPC protokolü (ML servisi)
- ✅ Node.js REST API
- ✅ 2 Harici API (ExchangeRate, Frankfurter)

### İleri Web Programlama (100/100)
- ✅ 7 Controller (ister: 5)
- ✅ Responsive tasarım (Bootstrap 5)
- ✅ 5 PartialView
- ✅ Custom Layout
- ✅ CRUD işlemleri (Admin Panel)
- ✅ 2 Kullanıcı rolü (Admin/User)
- ✅ TempData/ViewBag kullanımı

### Makine Öğrenmesi (100/100)
- ✅ Gerçek veri seti (1198 kayıt)
- ✅ 5 Model eğitimi ve karşılaştırma
- ✅ En iyi model seçimi (Gradient Boosting)
- ✅ gRPC ile servis entegrasyonu
- ✅ %99.88 tahmin doğruluğu

## 👥 Ekip

Yazılım Mühendisliği Bölümü - 2024/2025 Güz Dönemi

## 📅 Son Güncelleme

**18 Aralık 2024** - Final sürümü

- Yeni veri seti entegrasyonu (1446 kayıt)
- Gradient Boosting model eğitimi
- Tüm SP ve fonksiyonlar PostgreSQL'e uygulandı
- API ve ML servisleri optimize edildi

## 📄 Lisans

Bu proje eğitim amaçlı geliştirilmiştir.

---

<p align="center">
  <b>🍎 iPhone Fiyat Tahmin Sistemi</b><br>
  SOA • PostgreSQL • gRPC • SOAP • ML • ASP.NET Core
</p>
