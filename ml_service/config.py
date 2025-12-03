"""ML Service Konfigürasyonu"""
import os
from dotenv import load_dotenv

load_dotenv()

# Database ayarları
DB_CONFIG = {
    'host': 'localhost',
    'port': '5432',
    'database': 'iphone_price_db',
    'user': 'postgres',
    'password': 'postgres123'
}

# ML Model ayarları
MODEL_CONFIG = {
    'algorithm': 'RandomForest',
    'n_estimators': 100,
    'max_depth': 15,
    'min_samples_split': 5,
    'min_samples_leaf': 2,
    'random_state': 42,
    'test_size': 0.2
}

# gRPC ayarları
GRPC_CONFIG = {
    'host': '0.0.0.0',
    'port': 50051,
    'max_workers': 10
}

# Kozmetik durum skorları
CONDITION_SCORES = {
    'Mükemmel': 1.0,
    'Çok İyi': 0.93,
    'İyi': 0.85,
    'Orta': 0.75
}

# Model dosya yolu
MODEL_PATH = 'model.pkl'
SCALER_PATH = 'scaler.pkl'

