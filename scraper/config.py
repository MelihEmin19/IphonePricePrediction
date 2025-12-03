"""Scraper konfigürasyonu"""
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

# Scraper ayarları
SCRAPER_CONFIG = {
    'delay_between_requests': 2,  # Saniye cinsinden
    'max_retries': 3,
    'timeout': 30,
    'max_pages': 5  # Her site için maksimum sayfa sayısı
}

# Hedef siteler
TARGET_SITES = {
    'n11': {
        'enabled': True,
        'base_url': 'https://www.n11.com',
        'search_path': '/arama?q=yenilenmiş+iphone',
        'name': 'N11'
    },
    'easycep': {
        'enabled': True,
        'base_url': 'https://www.easycep.com',
        'search_path': '/yenilenmiş-iphone',
        'name': 'EasyCep'
    }
}

# İPhone modelleri ve RAM mapping
IPHONE_MODELS_MAP = {
    'iPhone 11': {'ram': 4, 'model_id': 1},
    'iPhone 11 Pro': {'ram': 4, 'model_id': 2},
    'iPhone 11 Pro Max': {'ram': 4, 'model_id': 3},
    'iPhone 12': {'ram': 4, 'model_id': 4},
    'iPhone 12 Mini': {'ram': 4, 'model_id': 5},
    'iPhone 12 Pro': {'ram': 6, 'model_id': 6},
    'iPhone 12 Pro Max': {'ram': 6, 'model_id': 7},
    'iPhone 13': {'ram': 4, 'model_id': 8},
    'iPhone 13 Mini': {'ram': 4, 'model_id': 9},
    'iPhone 13 Pro': {'ram': 6, 'model_id': 10},
    'iPhone 13 Pro Max': {'ram': 6, 'model_id': 11},
    'iPhone 14': {'ram': 6, 'model_id': 12},
    'iPhone 14 Plus': {'ram': 6, 'model_id': 13},
    'iPhone 14 Pro': {'ram': 6, 'model_id': 14},
    'iPhone 14 Pro Max': {'ram': 6, 'model_id': 15},
    'iPhone 15': {'ram': 6, 'model_id': 16},
    'iPhone 15 Plus': {'ram': 6, 'model_id': 17},
    'iPhone 15 Pro': {'ram': 8, 'model_id': 18},
    'iPhone 15 Pro Max': {'ram': 8, 'model_id': 19},
}

# Hafıza değerleri (GB)
STORAGE_VALUES = [64, 128, 256, 512, 1024]

# Kozmetik durum mapping
CONDITION_MAP = {
    'mükemmel': 'Mükemmel',
    'mukemmel': 'Mükemmel',
    'perfect': 'Mükemmel',
    'çok iyi': 'Çok İyi',
    'cok iyi': 'Çok İyi',
    'very good': 'Çok İyi',
    'iyi': 'İyi',
    'good': 'İyi',
    'orta': 'Orta',
    'fair': 'Orta',
    'sıfır': 'Mükemmel',
    'sifir': 'Mükemmel',
    's sınıf': 'Mükemmel',
    's sinif': 'Mükemmel',
    'a sınıf': 'Çok İyi',
    'a sinif': 'Çok İyi',
    'b sınıf': 'İyi',
    'b sinif': 'İyi',
    'c sınıf': 'Orta',
    'c sinif': 'Orta'
}

