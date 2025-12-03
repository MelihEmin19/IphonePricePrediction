"""
iPhone Fiyat Tahmin Sistemi - Web Scraper
N11 ve EasyCep'ten yenilenmiş iPhone ilanlarını toplar
"""

import requests
from bs4 import BeautifulSoup
import time
import random
import re
from fake_useragent import UserAgent
from typing import List, Dict, Optional
import logging
from config import SCRAPER_CONFIG, TARGET_SITES, IPHONE_MODELS_MAP, STORAGE_VALUES, CONDITION_MAP

# Logging yapılandırması
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('scraper.log', encoding='utf-8'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)


class IPhoneScraper:
    """iPhone ilan scraper sınıfı"""
    
    def __init__(self):
        self.ua = UserAgent()
        self.session = requests.Session()
        self.scraped_data: List[Dict] = []
        
    def get_headers(self) -> Dict[str, str]:
        """Random user agent ile header oluştur"""
        return {
            'User-Agent': self.ua.random,
            'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
            'Accept-Language': 'tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7',
            'Accept-Encoding': 'gzip, deflate, br',
            'Connection': 'keep-alive',
            'Upgrade-Insecure-Requests': '1'
        }
    
    def extract_model_from_title(self, title: str) -> Optional[str]:
        """Başlıktan iPhone modelini çıkar"""
        title_lower = title.lower()
        
        # En uzun eşleşmeyi bul (örn: iPhone 13 Pro Max yerine iPhone 13 eşleşmesin)
        best_match = None
        best_match_length = 0
        
        for model_name in IPHONE_MODELS_MAP.keys():
            model_lower = model_name.lower()
            if model_lower in title_lower:
                if len(model_lower) > best_match_length:
                    best_match = model_name
                    best_match_length = len(model_lower)
        
        return best_match
    
    def extract_storage_from_title(self, title: str) -> Optional[int]:
        """Başlıktan hafıza bilgisini çıkar"""
        # 64GB, 128 GB, 256gb gibi formatları yakala
        pattern = r'(\d+)\s*gb'
        match = re.search(pattern, title.lower())
        
        if match:
            storage = int(match.group(1))
            if storage in STORAGE_VALUES:
                return storage
        
        return None
    
    def extract_condition(self, text: str) -> str:
        """Metinden kozmetik durumu çıkar"""
        text_lower = text.lower()
        
        for key, value in CONDITION_MAP.items():
            if key in text_lower:
                return value
        
        return 'İyi'  # Default
    
    def extract_price(self, price_text: str) -> Optional[float]:
        """Fiyat metninden sayısal değer çıkar"""
        # "16.500,00 TL" -> 16500.0
        # "14500 TL" -> 14500.0
        price_text = price_text.replace('TL', '').replace('₺', '').strip()
        price_text = price_text.replace('.', '').replace(',', '.')
        
        try:
            # Sadece rakamları ve noktayı al
            price_clean = re.sub(r'[^\d.]', '', price_text)
            price = float(price_clean)
            
            # Mantıklı fiyat kontrolü (5000 - 100000 TL arası)
            if 5000 <= price <= 100000:
                return price
        except (ValueError, AttributeError):
            pass
        
        return None
    
    def scrape_mock_data(self) -> List[Dict]:
        """
        Gerçek scraping yerine simüle edilmiş veri üretir.
        Not: Gerçek scraping yasal sorunlara yol açabilir, bu yüzden
        proje için gerçekçi mock veri kullanıyoruz.
        """
        logger.info("Mock veri oluşturuluyor...")
        
        mock_listings = []
        sources = ['N11', 'EasyCep', 'Hepsiburada', 'GittiGidiyor']
        
        # Her model için veri üret
        for model_name, model_info in IPHONE_MODELS_MAP.items():
            model_id = model_info['model_id']
            ram_gb = model_info['ram']
            
            # Bu modelin hangi hafıza seçenekleri var?
            if 'Pro Max' in model_name or 'Pro' in model_name:
                storages = [128, 256, 512, 1024]
            elif '11' in model_name and 'Pro' not in model_name:
                storages = [64, 128, 256]
            else:
                storages = [64, 128, 256, 512]
            
            # Her hafıza için
            for storage_gb in storages:
                # Base fiyat hesapla (model ve hafızaya göre)
                base_price = self._calculate_base_price(model_name, storage_gb)
                
                # Her durum için 2-5 ilan oluştur
                for condition in ['Mükemmel', 'Çok İyi', 'İyi', 'Orta']:
                    num_listings = random.randint(2, 5)
                    
                    for _ in range(num_listings):
                        # Duruma göre fiyat ayarla
                        condition_multiplier = {
                            'Mükemmel': 1.0,
                            'Çok İyi': 0.93,
                            'İyi': 0.85,
                            'Orta': 0.75
                        }[condition]
                        
                        # +/- %10 varyans ekle
                        variance = random.uniform(0.9, 1.1)
                        final_price = base_price * condition_multiplier * variance
                        
                        listing = {
                            'title': f"{model_name} {storage_gb}GB {condition}",
                            'model': model_name,
                            'model_id': model_id,
                            'ram_gb': ram_gb,
                            'storage_gb': storage_gb,
                            'condition': condition,
                            'price': round(final_price, 2),
                            'source': random.choice(sources),
                            'url': f"https://example.com/listing/{random.randint(10000, 99999)}"
                        }
                        
                        mock_listings.append(listing)
                        
                        logger.debug(f"Mock ilan: {listing['title']} - {listing['price']} TL")
        
        logger.info(f"Toplam {len(mock_listings)} mock ilan oluşturuldu")
        return mock_listings
    
    def _calculate_base_price(self, model_name: str, storage_gb: int) -> float:
        """Model ve hafızaya göre baz fiyat hesapla"""
        # Model bazlı fiyatlar (2024 Aralık piyasa fiyatları)
        model_base_prices = {
            'iPhone 11': 12000,
            'iPhone 11 Pro': 15000,
            'iPhone 11 Pro Max': 17000,
            'iPhone 12': 16000,
            'iPhone 12 Mini': 14000,
            'iPhone 12 Pro': 20000,
            'iPhone 12 Pro Max': 22000,
            'iPhone 13': 20000,
            'iPhone 13 Mini': 18000,
            'iPhone 13 Pro': 26000,
            'iPhone 13 Pro Max': 28000,
            'iPhone 14': 28000,
            'iPhone 14 Plus': 30000,
            'iPhone 14 Pro': 38000,
            'iPhone 14 Pro Max': 42000,
            'iPhone 15': 42000,
            'iPhone 15 Plus': 45000,
            'iPhone 15 Pro': 55000,
            'iPhone 15 Pro Max': 62000,
        }
        
        base = model_base_prices.get(model_name, 15000)
        
        # Hafıza artışı (her katlama +%20-30)
        storage_multiplier = {
            64: 1.0,
            128: 1.15,
            256: 1.35,
            512: 1.60,
            1024: 1.90
        }
        
        multiplier = storage_multiplier.get(storage_gb, 1.0)
        return base * multiplier
    
    def scrape_n11(self) -> List[Dict]:
        """
        N11'den veri çek (Placeholder - gerçek implementasyon yasal izin gerektirir)
        Şimdilik mock veri döner
        """
        logger.warning("N11 scraping gerçek değil, mock veri kullanılıyor (yasal koruma)")
        return []
    
    def scrape_easycep(self) -> List[Dict]:
        """
        EasyCep'ten veri çek (Placeholder - gerçek implementasyon yasal izin gerektirir)
        Şimdilik mock veri döner
        """
        logger.warning("EasyCep scraping gerçek değil, mock veri kullanılıyor (yasal koruma)")
        return []
    
    def run(self) -> List[Dict]:
        """Ana scraping fonksiyonu"""
        logger.info("Scraping başlatıldı...")
        
        # Mock veri kullan (gerçek scraping için izin gerekli)
        self.scraped_data = self.scrape_mock_data()
        
        # Gerçek scraping için (yasal izin alındığında):
        # if TARGET_SITES['n11']['enabled']:
        #     self.scraped_data.extend(self.scrape_n11())
        # if TARGET_SITES['easycep']['enabled']:
        #     self.scraped_data.extend(self.scrape_easycep())
        
        logger.info(f"Scraping tamamlandı. Toplam {len(self.scraped_data)} ilan bulundu")
        return self.scraped_data


def main():
    """Ana fonksiyon"""
    scraper = IPhoneScraper()
    data = scraper.run()
    
    logger.info(f"\n{'='*60}")
    logger.info(f"ÖZET İSTATİSTİKLER")
    logger.info(f"{'='*60}")
    logger.info(f"Toplam ilan sayısı: {len(data)}")
    
    if data:
        # Model dağılımı
        models = {}
        for item in data:
            model = item['model']
            models[model] = models.get(model, 0) + 1
        
        logger.info(f"\nModel Dağılımı:")
        for model, count in sorted(models.items(), key=lambda x: x[1], reverse=True)[:10]:
            logger.info(f"  {model}: {count} ilan")
        
        # Fiyat aralığı
        prices = [item['price'] for item in data]
        logger.info(f"\nFiyat Aralığı:")
        logger.info(f"  En düşük: {min(prices):,.2f} TL")
        logger.info(f"  En yüksek: {max(prices):,.2f} TL")
        logger.info(f"  Ortalama: {sum(prices)/len(prices):,.2f} TL")
    
    logger.info(f"{'='*60}\n")
    
    return data


if __name__ == "__main__":
    main()

