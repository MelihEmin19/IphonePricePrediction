"""ML Service Konfigürasyonu"""
import os

# gRPC ayarları
GRPC_CONFIG = {
    'host': '0.0.0.0',
    'port': 50051,
    'max_workers': 10
}

# Model dosya yolları
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
MODEL_DIR = os.path.join(BASE_DIR, 'models')
MODEL_PATH = os.path.join(MODEL_DIR, 'price_model.pkl')
SCALER_PATH = os.path.join(MODEL_DIR, 'scaler.pkl')
CONFIG_PATH = os.path.join(MODEL_DIR, 'model_config.pkl')

# Kozmetik durum skorları (1-4 arası puan)
CONDITION_SCORES = {
    'Mükemmel': 4,
    'Çok İyi': 3,
    'İyi': 2,
    'Orta': 1
}

# iPhone Model Bilgileri
IPHONE_MODELS = {
    'iPhone 8': {'kamera_mp': 12, 'ekran': 4.7, 'batarya': 1821, 'yil': 2017, 'segment': 2, 'kod': 7, 'ram': 2},
    'iPhone SE 2020': {'kamera_mp': 12, 'ekran': 4.7, 'batarya': 1821, 'yil': 2020, 'segment': 2, 'kod': 9, 'ram': 3},
    'iPhone X': {'kamera_mp': 12, 'ekran': 5.8, 'batarya': 2716, 'yil': 2017, 'segment': 2, 'kod': 10, 'ram': 3},
    'iPhone XR': {'kamera_mp': 12, 'ekran': 5.8, 'batarya': 2716, 'yil': 2017, 'segment': 2, 'kod': 10, 'ram': 3},
    'iPhone XS': {'kamera_mp': 12, 'ekran': 5.8, 'batarya': 2716, 'yil': 2017, 'segment': 2, 'kod': 10, 'ram': 4},
    'iPhone 11': {'kamera_mp': 12, 'ekran': 6.1, 'batarya': 3110, 'yil': 2019, 'segment': 2, 'kod': 1, 'ram': 4},
    'iPhone 11 Pro': {'kamera_mp': 12, 'ekran': 5.8, 'batarya': 3046, 'yil': 2019, 'segment': 4, 'kod': 1, 'ram': 4},
    'iPhone 11 Pro Max': {'kamera_mp': 12, 'ekran': 6.5, 'batarya': 3969, 'yil': 2019, 'segment': 5, 'kod': 1, 'ram': 4},
    'iPhone 12': {'kamera_mp': 12, 'ekran': 6.1, 'batarya': 2815, 'yil': 2020, 'segment': 2, 'kod': 2, 'ram': 4},
    'iPhone 12 Mini': {'kamera_mp': 12, 'ekran': 5.4, 'batarya': 2227, 'yil': 2020, 'segment': 1, 'kod': 2, 'ram': 4},
    'iPhone 12 Pro': {'kamera_mp': 12, 'ekran': 6.1, 'batarya': 2815, 'yil': 2020, 'segment': 4, 'kod': 2, 'ram': 6},
    'iPhone 12 Pro Max': {'kamera_mp': 12, 'ekran': 6.7, 'batarya': 3687, 'yil': 2020, 'segment': 5, 'kod': 2, 'ram': 6},
    'iPhone 13': {'kamera_mp': 12, 'ekran': 6.1, 'batarya': 3227, 'yil': 2021, 'segment': 2, 'kod': 3, 'ram': 4},
    'iPhone 13 Mini': {'kamera_mp': 12, 'ekran': 5.4, 'batarya': 2406, 'yil': 2021, 'segment': 1, 'kod': 3, 'ram': 4},
    'iPhone 13 Pro': {'kamera_mp': 12, 'ekran': 6.1, 'batarya': 3095, 'yil': 2021, 'segment': 4, 'kod': 3, 'ram': 6},
    'iPhone 13 Pro Max': {'kamera_mp': 12, 'ekran': 6.7, 'batarya': 4352, 'yil': 2021, 'segment': 5, 'kod': 3, 'ram': 6},
    'iPhone 14': {'kamera_mp': 12, 'ekran': 6.1, 'batarya': 3279, 'yil': 2022, 'segment': 2, 'kod': 4, 'ram': 6},
    'iPhone 14 Plus': {'kamera_mp': 12, 'ekran': 6.7, 'batarya': 4325, 'yil': 2022, 'segment': 3, 'kod': 4, 'ram': 6},
    'iPhone 14 Pro': {'kamera_mp': 48, 'ekran': 6.1, 'batarya': 3200, 'yil': 2022, 'segment': 4, 'kod': 4, 'ram': 6},
    'iPhone 14 Pro Max': {'kamera_mp': 48, 'ekran': 6.7, 'batarya': 4323, 'yil': 2022, 'segment': 5, 'kod': 4, 'ram': 6},
    'iPhone 15': {'kamera_mp': 48, 'ekran': 6.1, 'batarya': 3349, 'yil': 2023, 'segment': 2, 'kod': 5, 'ram': 6},
    'iPhone 15 Plus': {'kamera_mp': 48, 'ekran': 6.7, 'batarya': 4383, 'yil': 2023, 'segment': 3, 'kod': 5, 'ram': 6},
    'iPhone 15 Pro': {'kamera_mp': 48, 'ekran': 6.1, 'batarya': 3274, 'yil': 2023, 'segment': 4, 'kod': 5, 'ram': 8},
    'iPhone 15 Pro Max': {'kamera_mp': 48, 'ekran': 6.7, 'batarya': 4422, 'yil': 2023, 'segment': 5, 'kod': 5, 'ram': 8},
    'iPhone 16': {'kamera_mp': 48, 'ekran': 6.1, 'batarya': 3561, 'yil': 2024, 'segment': 2, 'kod': 6, 'ram': 8},
    'iPhone 16 Plus': {'kamera_mp': 48, 'ekran': 6.7, 'batarya': 4676, 'yil': 2024, 'segment': 3, 'kod': 6, 'ram': 8},
    'iPhone 16 Pro': {'kamera_mp': 48, 'ekran': 6.3, 'batarya': 3582, 'yil': 2024, 'segment': 4, 'kod': 6, 'ram': 8},
    'iPhone 16 Pro Max': {'kamera_mp': 48, 'ekran': 6.9, 'batarya': 4676, 'yil': 2024, 'segment': 5, 'kod': 6, 'ram': 8},
}
