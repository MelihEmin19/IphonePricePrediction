# iPhone Fiyat Tahmin Sistemi

İkinci el iPhone'ların piyasa değerini yapay zeka ile tahmin eden SOA mimarisi tabanlı web uygulaması.

## Mimari

- **Database:** PostgreSQL (6 entity, stored procedures, views)
- **ML Service:** Python + gRPC (Random Forest Regressor)
- **API Service:** Node.js (gRPC client + SOAP)
- **Web App:** ASP.NET Core MVC
- **Data Source:** Web scraper (N11, EasyCep)

## Kurulum

### 1. PostgreSQL Kurulumu
```bash
# PostgreSQL 15+ kurulumu gereklidir
psql -U postgres -f database/schema.sql
psql -U postgres -f database/stored_procedures.sql
psql -U postgres -f database/views.sql
```

### 2. Python Ortamı
```bash
cd scraper
pip install -r requirements.txt

cd ../ml_service
pip install -r requirements.txt
```

### 3. Node.js API
```bash
cd api_service
npm install
```

### 4. ASP.NET Core
```bash
cd web_app
dotnet restore
```

## Çalıştırma

1. **Veri Toplama:**
```bash
cd scraper
python scraper.py
```

2. **Model Eğitimi:**
```bash
cd ml_service
python train_model.py
```

3. **gRPC Sunucu:**
```bash
cd ml_service
python grpc_server.py
```

4. **Node.js API:**
```bash
cd api_service
npm start
```

5. **Web Uygulaması:**
```bash
cd web_app
dotnet run
```

## Port Yapılandırması

- PostgreSQL: 5432
- Python gRPC: 50051
- Node.js API: 3000
- ASP.NET MVC: 5000

## Kullanım

1. Tarayıcıda http://localhost:5000 adresine gidin
2. Telefon özelliklerini seçin (Model, RAM, Hafıza, Durum)
3. "Fiyat Tahmin Et" butonuna tıklayın
4. Sistem size adil piyasa değerini gösterir

## Admin Paneli

- URL: http://localhost:5000/Admin/Panel
- Özellikler:
  - Yeni veri çekme (scraper tetikleme)
  - İstatistikler görüntüleme
  - Kullanıcı yönetimi

## Proje Yapısı

```
IphonePricePrediction/
├── scraper/              # Web scraper
│   ├── scraper.py
│   ├── data_cleaner.py
│   └── requirements.txt
├── ml_service/           # ML + gRPC
│   ├── train_model.py
│   ├── grpc_server.py
│   ├── proto/
│   │   └── prediction.proto
│   ├── model.pkl
│   └── requirements.txt
├── database/             # PostgreSQL
│   ├── schema.sql
│   ├── stored_procedures.sql
│   └── views.sql
├── api_service/          # Node.js API
│   ├── src/
│   │   ├── grpc_client.js
│   │   ├── soap_service.js
│   │   └── routes/
│   ├── package.json
│   └── server.js
├── web_app/              # ASP.NET MVC
│   ├── Controllers/
│   ├── Views/
│   ├── Models/
│   └── Program.cs
└── docs/                 # Dokümantasyon
```

## PDF İsterleri

| İster | Uygulama |
|-------|----------|
| Veri Toplama | Python scraper (N11, EasyCep) |
| ML Modeli | Random Forest Regressor |
| 6 Entity | Users, Brands, Models, Specs, Listings, Predictions |
| Stored Procedure | sp_InsertListing() |
| View | vw_BrandAveragePrices |
| gRPC | Python ML ↔ Node.js |
| SOAP | Döviz kuru servisi |
| Node.js API | Middleware katman |
| User Roles | Admin/User |
| MVC | ASP.NET Core MVC |

## Lisans

Eğitim projesi

