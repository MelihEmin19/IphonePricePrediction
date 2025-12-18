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

# Cihaz durumu skorları (0-3 arası)
# Yeni veri setine göre: Outlet=0, İyi=1, Çok İyi=2, Mükemmel=3
CONDITION_SCORES = {
    'Mükemmel': 3,
    'Çok İyi': 2,
    'İyi': 1,
    'Outlet': 0,
    'Orta': 0
}

# Segment değerleri (0-4 arası)
# Base=0, Mini=0, Plus=0, Pro=0, Pro Max=0 şeklinde segment değerleri var
SEGMENT_VALUES = {
    'Base': 0,
    'Mini': 0,
    'Plus': 0,
    'Pro': 0,
    'Pro Max': 0
}

# iPhone Model Bilgileri - Yeni veri setine göre güncellendi
# seri_no: 8, 9, 10, 11, 12, 13, 14, 15, 16
IPHONE_MODELS = {
    'iPhone 8': {'kamera_mp': 12, 'ekran': 4, 'batarya': 1821, 'yil': 2017, 'segment': 0, 'seri_no': 8, 'ram': 2},
    'iPhone 8 Plus': {'kamera_mp': 12, 'ekran': 5, 'batarya': 2691, 'yil': 2017, 'segment': 0, 'seri_no': 8, 'ram': 3},
    'iPhone SE 2020': {'kamera_mp': 12, 'ekran': 4, 'batarya': 1821, 'yil': 2020, 'segment': 0, 'seri_no': 9, 'ram': 3},
    'iPhone X': {'kamera_mp': 12, 'ekran': 5, 'batarya': 2716, 'yil': 2017, 'segment': 0, 'seri_no': 10, 'ram': 3},
    'iPhone XR': {'kamera_mp': 12, 'ekran': 5, 'batarya': 2716, 'yil': 2017, 'segment': 0, 'seri_no': 10, 'ram': 3},
    'iPhone XS': {'kamera_mp': 12, 'ekran': 5, 'batarya': 2716, 'yil': 2017, 'segment': 0, 'seri_no': 10, 'ram': 4},
    'iPhone 11': {'kamera_mp': 12, 'ekran': 6, 'batarya': 3110, 'yil': 2019, 'segment': 0, 'seri_no': 11, 'ram': 4},
    'iPhone 11 Pro': {'kamera_mp': 12, 'ekran': 5, 'batarya': 3046, 'yil': 2019, 'segment': 0, 'seri_no': 11, 'ram': 4},
    'iPhone 11 Pro Max': {'kamera_mp': 12, 'ekran': 6, 'batarya': 3969, 'yil': 2019, 'segment': 0, 'seri_no': 11, 'ram': 4},
    'iPhone 12': {'kamera_mp': 12, 'ekran': 6, 'batarya': 2815, 'yil': 2020, 'segment': 0, 'seri_no': 12, 'ram': 4},
    'iPhone 12 Mini': {'kamera_mp': 12, 'ekran': 5, 'batarya': 2227, 'yil': 2020, 'segment': 0, 'seri_no': 12, 'ram': 4},
    'iPhone 12 Pro': {'kamera_mp': 12, 'ekran': 6, 'batarya': 2815, 'yil': 2020, 'segment': 0, 'seri_no': 12, 'ram': 6},
    'iPhone 12 Pro Max': {'kamera_mp': 12, 'ekran': 6, 'batarya': 3687, 'yil': 2020, 'segment': 0, 'seri_no': 12, 'ram': 6},
    'iPhone 13': {'kamera_mp': 12, 'ekran': 6, 'batarya': 3227, 'yil': 2021, 'segment': 0, 'seri_no': 13, 'ram': 4},
    'iPhone 13 Mini': {'kamera_mp': 12, 'ekran': 5, 'batarya': 2406, 'yil': 2021, 'segment': 0, 'seri_no': 13, 'ram': 4},
    'iPhone 13 Pro': {'kamera_mp': 12, 'ekran': 6, 'batarya': 3095, 'yil': 2021, 'segment': 0, 'seri_no': 13, 'ram': 6},
    'iPhone 13 Pro Max': {'kamera_mp': 12, 'ekran': 6, 'batarya': 4352, 'yil': 2021, 'segment': 0, 'seri_no': 13, 'ram': 6},
    'iPhone 14': {'kamera_mp': 12, 'ekran': 6, 'batarya': 3279, 'yil': 2022, 'segment': 0, 'seri_no': 14, 'ram': 6},
    'iPhone 14 Plus': {'kamera_mp': 12, 'ekran': 6, 'batarya': 4325, 'yil': 2022, 'segment': 0, 'seri_no': 14, 'ram': 6},
    'iPhone 14 Pro': {'kamera_mp': 48, 'ekran': 6, 'batarya': 3200, 'yil': 2022, 'segment': 0, 'seri_no': 14, 'ram': 6},
    'iPhone 14 Pro Max': {'kamera_mp': 48, 'ekran': 6, 'batarya': 4323, 'yil': 2022, 'segment': 0, 'seri_no': 14, 'ram': 6},
    'iPhone 15': {'kamera_mp': 48, 'ekran': 6, 'batarya': 3349, 'yil': 2023, 'segment': 0, 'seri_no': 15, 'ram': 6},
    'iPhone 15 Plus': {'kamera_mp': 48, 'ekran': 6, 'batarya': 4383, 'yil': 2023, 'segment': 0, 'seri_no': 15, 'ram': 6},
    'iPhone 15 Pro': {'kamera_mp': 48, 'ekran': 6, 'batarya': 3274, 'yil': 2023, 'segment': 0, 'seri_no': 15, 'ram': 8},
    'iPhone 15 Pro Max': {'kamera_mp': 48, 'ekran': 6, 'batarya': 4422, 'yil': 2023, 'segment': 0, 'seri_no': 15, 'ram': 8},
    'iPhone 16': {'kamera_mp': 48, 'ekran': 6, 'batarya': 3561, 'yil': 2024, 'segment': 0, 'seri_no': 16, 'ram': 8},
    'iPhone 16 Plus': {'kamera_mp': 48, 'ekran': 6, 'batarya': 4676, 'yil': 2024, 'segment': 0, 'seri_no': 16, 'ram': 8},
    'iPhone 16 Pro': {'kamera_mp': 48, 'ekran': 6, 'batarya': 3582, 'yil': 2024, 'segment': 0, 'seri_no': 16, 'ram': 8},
    'iPhone 16 Pro Max': {'kamera_mp': 48, 'ekran': 6, 'batarya': 4676, 'yil': 2024, 'segment': 0, 'seri_no': 16, 'ram': 8},
}
