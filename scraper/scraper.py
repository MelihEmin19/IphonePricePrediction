"""
iPhone Fiyat Tahmin Sistemi - Web Scraper
EasyCep'ten yenilenmiş iPhone ilanlarını çeker
"""

import requests
from bs4 import BeautifulSoup
import time
import random
import re
import csv
import os
from fake_useragent import UserAgent
from typing import List, Dict, Optional
import logging
from datetime import datetime

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

# iPhone model bilgileri
IPHONE_MODELS = {
    'iPhone 11': {'ram': 4, 'release_year': 2019, 'camera_mp': 12, 'segment': 'Base'},
    'iPhone 11 Pro': {'ram': 4, 'release_year': 2019, 'camera_mp': 12, 'segment': 'Pro'},
    'iPhone 11 Pro Max': {'ram': 4, 'release_year': 2019, 'camera_mp': 12, 'segment': 'Pro Max'},
    'iPhone 12': {'ram': 4, 'release_year': 2020, 'camera_mp': 12, 'segment': 'Base'},
    'iPhone 12 Mini': {'ram': 4, 'release_year': 2020, 'camera_mp': 12, 'segment': 'Mini'},
    'iPhone 12 Pro': {'ram': 6, 'release_year': 2020, 'camera_mp': 12, 'segment': 'Pro'},
    'iPhone 12 Pro Max': {'ram': 6, 'release_year': 2020, 'camera_mp': 12, 'segment': 'Pro Max'},
    'iPhone 13': {'ram': 4, 'release_year': 2021, 'camera_mp': 12, 'segment': 'Base'},
    'iPhone 13 Mini': {'ram': 4, 'release_year': 2021, 'camera_mp': 12, 'segment': 'Mini'},
    'iPhone 13 Pro': {'ram': 6, 'release_year': 2021, 'camera_mp': 12, 'segment': 'Pro'},
    'iPhone 13 Pro Max': {'ram': 6, 'release_year': 2021, 'camera_mp': 12, 'segment': 'Pro Max'},
    'iPhone 14': {'ram': 6, 'release_year': 2022, 'camera_mp': 12, 'segment': 'Base'},
    'iPhone 14 Plus': {'ram': 6, 'release_year': 2022, 'camera_mp': 12, 'segment': 'Plus'},
    'iPhone 14 Pro': {'ram': 6, 'release_year': 2022, 'camera_mp': 48, 'segment': 'Pro'},
    'iPhone 14 Pro Max': {'ram': 6, 'release_year': 2022, 'camera_mp': 48, 'segment': 'Pro Max'},
    'iPhone 15': {'ram': 6, 'release_year': 2023, 'camera_mp': 48, 'segment': 'Base'},
    'iPhone 15 Plus': {'ram': 6, 'release_year': 2023, 'camera_mp': 48, 'segment': 'Plus'},
    'iPhone 15 Pro': {'ram': 8, 'release_year': 2023, 'camera_mp': 48, 'segment': 'Pro'},
    'iPhone 15 Pro Max': {'ram': 8, 'release_year': 2023, 'camera_mp': 48, 'segment': 'Pro Max'},
    'iPhone 16': {'ram': 8, 'release_year': 2024, 'camera_mp': 48, 'segment': 'Base'},
    'iPhone 16 Pro': {'ram': 8, 'release_year': 2024, 'camera_mp': 48, 'segment': 'Pro'},
    'iPhone 16 Pro Max': {'ram': 8, 'release_year': 2024, 'camera_mp': 48, 'segment': 'Pro Max'},
}

# Durum skorları
CONDITION_MAP = {
    'mükemmel': 5,
    'mukemmel': 5,
    'çok iyi': 4,
    'cok iyi': 4,
    'iyi': 3,
    'orta': 2,
    'kötü': 1,
    'kotu': 1,
}

# EasyCep Kategori URL'leri
EASYCEP_CATEGORIES = [
    ('iPhone 11', 'https://easycep.com/kategori/iphone-11-64'),
    ('iPhone 11 Pro', 'https://easycep.com/kategori/iphone-11-pro-65'),
    ('iPhone 11 Pro Max', 'https://easycep.com/kategori/iphone-11-pro-max-66'),
    ('iPhone 12', 'https://easycep.com/kategori/iphone-12-67'),
    ('iPhone 12 Mini', 'https://easycep.com/kategori/iphone-12-mini-68'),
    ('iPhone 12 Pro', 'https://easycep.com/kategori/iphone-12-pro-69'),
    ('iPhone 12 Pro Max', 'https://easycep.com/kategori/iphone-12-pro-max-70'),
    ('iPhone 13', 'https://easycep.com/kategori/iphone-13-116'),
    ('iPhone 13 Mini', 'https://easycep.com/kategori/iphone-13-mini-117'),
    ('iPhone 13 Pro', 'https://easycep.com/kategori/iphone-13-pro-118'),
    ('iPhone 13 Pro Max', 'https://easycep.com/kategori/iphone-13-pro-max-119'),
    ('iPhone 14', 'https://easycep.com/kategori/iphone-14-738'),
    ('iPhone 14 Plus', 'https://easycep.com/kategori/iphone-14-plus-739'),
    ('iPhone 14 Pro', 'https://easycep.com/kategori/iphone-14-pro-769'),
    ('iPhone 14 Pro Max', 'https://easycep.com/kategori/iphone-14-pro-max-772'),
    ('iPhone 15', 'https://easycep.com/kategori/iphone-15-935'),
    ('iPhone 15 Plus', 'https://easycep.com/kategori/iphone-15-plus-936'),
    ('iPhone 15 Pro', 'https://easycep.com/kategori/iphone-15-pro-937'),
    ('iPhone 15 Pro Max', 'https://easycep.com/kategori/iphone-15-pro-max-938'),
    ('iPhone 16', 'https://easycep.com/kategori/iphone-16-1107'),
    ('iPhone 16 Pro', 'https://easycep.com/kategori/iphone-16-pro-1109'),
    ('iPhone 16 Pro Max', 'https://easycep.com/kategori/iphone-16-pro-max-1110'),
]


class EasyCepScraper:
    """EasyCep'ten iPhone verisi çeken scraper"""
    
    def __init__(self):
        self.ua = UserAgent()
        self.session = requests.Session()
        self.scraped_data: List[Dict] = []
        self.base_url = 'https://easycep.com'
        
    def get_headers(self) -> Dict[str, str]:
        return {
            'User-Agent': self.ua.random,
            'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
            'Accept-Language': 'tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7',
            'Connection': 'keep-alive',
        }
    
    def extract_storage(self, title: str) -> Optional[int]:
        """Başlıktan depolama kapasitesini çıkar"""
        match = re.search(r'(\d+)\s*gb', title.lower())
        if match:
            storage = int(match.group(1))
            if storage in [64, 128, 256, 512, 1024]:
                return storage
        return None
    
    def extract_condition(self, text: str) -> int:
        """Metinden durum skorunu çıkar"""
        text_lower = text.lower()
        for key, score in CONDITION_MAP.items():
            if key in text_lower:
                return score
        return 3
    
    def extract_taksit_prices(self, soup: BeautifulSoup) -> List[float]:
        """Sayfadaki tüm taksit fiyatlarını çıkar ve toplam fiyata çevir"""
        prices = []
        page_text = soup.get_text()
        
        # Taksit pattern: "12 Ay x 5.763,23TL"
        taksit_matches = re.findall(r'(\d+)\s*Ay\s*x\s*([\d.]+),?(\d{0,2})\s*TL', page_text, re.I)
        
        for match in taksit_matches:
            try:
                taksit_sayisi = int(match[0])
                # Türk formatı: 5.763,23 -> 5763.23
                integer_part = match[1].replace('.', '')
                decimal_part = match[2] if match[2] else '00'
                taksit_tutari = float(f"{integer_part}.{decimal_part}")
                
                toplam_fiyat = taksit_sayisi * taksit_tutari
                
                if 5000 <= toplam_fiyat <= 200000:
                    prices.append(toplam_fiyat)
            except:
                continue
        
        return prices
    
    def extract_product_links(self, soup: BeautifulSoup) -> List[str]:
        """Sayfadaki ürün linklerini çıkar"""
        links = []
        all_links = soup.find_all('a', href=True)
        
        for link in all_links:
            href = link.get('href', '')
            # Ürün linki mi? (apple-iphone içeren)
            if '/apple-iphone' in href.lower() and href not in links:
                full_url = href if href.startswith('http') else f'{self.base_url}{href}'
                links.append(full_url)
        
        return links
    
    def scrape_category(self, model_name: str, url: str) -> List[Dict]:
        """Bir kategoriden ürünleri çek"""
        listings = []
        page = 1
        max_pages = 10
        
        model_info = IPHONE_MODELS.get(model_name, {})
        
        while page <= max_pages:
            page_url = f"{url}?page={page}" if page > 1 else url
            
            try:
                response = self.session.get(page_url, headers=self.get_headers(), timeout=15)
                
                if response.status_code != 200:
                    break
                
                soup = BeautifulSoup(response.content, 'html.parser')
                
                # Sayfadaki fiyatları çek
                prices = self.extract_taksit_prices(soup)
                
                # Ürün linklerini çek
                product_links = self.extract_product_links(soup)
                
                if not prices:
                    break
                
                logger.info(f"    Sayfa {page}: {len(prices)} fiyat, {len(product_links)} link")
                
                # Her fiyat için bir ürün oluştur
                for i, price in enumerate(prices):
                    # Storage bilgisini linklerden çıkarmaya çalış
                    storage = 128  # Default
                    condition = 3  # Default: İyi
                    
                    if i < len(product_links):
                        link_text = product_links[i]
                        extracted_storage = self.extract_storage(link_text)
                        if extracted_storage:
                            storage = extracted_storage
                    
                    listing = {
                        'model': model_name,
                        'ram_gb': model_info.get('ram', 4),
                        'storage_gb': storage,
                        'condition': condition,
                        'price': price,
                        'source': 'EasyCep',
                        'cikis_yili': model_info.get('release_year', 2021),
                        'segment': model_info.get('segment', 'Base'),
                        'ana_kamera_mp': model_info.get('camera_mp', 12),
                    }
                    
                    listings.append(listing)
                
                page += 1
                time.sleep(random.uniform(0.5, 1))
                
            except Exception as e:
                logger.warning(f"    Hata: {e}")
                break
        
        return listings
    
    def run(self) -> List[Dict]:
        """Ana scraping fonksiyonu"""
        logger.info("="*60)
        logger.info("EasyCep Web Scraping Başlatıldı")
        logger.info(f"Tarih: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        logger.info("="*60)
        
        all_listings = []
        
        for model_name, url in EASYCEP_CATEGORIES:
            logger.info(f"\nKategori: {model_name}")
            
            listings = self.scrape_category(model_name, url)
            all_listings.extend(listings)
            
            logger.info(f"  Toplam: {len(listings)} ürün")
            time.sleep(random.uniform(1, 2))
        
        # Duplicate temizle (aynı model, storage, fiyat)
        unique_listings = []
        seen = set()
        
        for item in all_listings:
            key = (item['model'], item['storage_gb'], round(item['price']))
            if key not in seen:
                seen.add(key)
                unique_listings.append(item)
        
        self.scraped_data = unique_listings
        
        logger.info("\n" + "="*60)
        logger.info("Scraping Tamamlandı!")
        logger.info(f"Ham veri: {len(all_listings)}")
        logger.info(f"Benzersiz: {len(unique_listings)}")
        logger.info("="*60)
        
        return unique_listings
    
    def save_to_csv(self, filename: str = None):
        """Verileri CSV'ye kaydet"""
        if not self.scraped_data:
            logger.warning("Kaydedilecek veri yok!")
            return
        
        if not filename:
            filename = '../data/dataset.csv'
        
        os.makedirs(os.path.dirname(filename), exist_ok=True)
        
        fieldnames = ['id', 'model', 'ram_gb', 'storage_gb', 'condition', 'price', 
                      'source', 'seri_no', 'segment', 'cikis_yili', 'teknoloji_yasi', 'ana_kamera_mp']
        
        with open(filename, 'w', newline='', encoding='utf-8') as f:
            writer = csv.DictWriter(f, fieldnames=fieldnames)
            writer.writeheader()
            
            for i, item in enumerate(self.scraped_data, 1):
                model_num = ''.join(filter(str.isdigit, item['model']))[:2] or '11'
                teknoloji_yasi = 2024 - item.get('cikis_yili', 2021)
                
                row = {
                    'id': i,
                    'model': item['model'],
                    'ram_gb': item['ram_gb'],
                    'storage_gb': item['storage_gb'],
                    'condition': item['condition'],
                    'price': round(item['price'], 2),
                    'source': item['source'],
                    'seri_no': model_num,
                    'segment': item.get('segment', 'Base'),
                    'cikis_yili': item.get('cikis_yili', 2021),
                    'teknoloji_yasi': teknoloji_yasi,
                    'ana_kamera_mp': item.get('ana_kamera_mp', 12),
                }
                writer.writerow(row)
        
        logger.info(f"\n✓ CSV kaydedildi: {filename}")
        logger.info(f"  Toplam {len(self.scraped_data)} kayıt")


def main():
    """Ana fonksiyon"""
    scraper = EasyCepScraper()
    data = scraper.run()
    
    if data:
        logger.info(f"\n{'='*60}")
        logger.info("ÖZET İSTATİSTİKLER")
        logger.info(f"{'='*60}")
        
        # Model dağılımı
        models = {}
        for item in data:
            model = item['model']
            models[model] = models.get(model, 0) + 1
        
        logger.info("\nModel Dağılımı:")
        for model, count in sorted(models.items(), key=lambda x: x[1], reverse=True):
            logger.info(f"  {model}: {count} ilan")
        
        # Fiyat aralığı
        prices = [item['price'] for item in data]
        logger.info(f"\nFiyat Aralığı:")
        logger.info(f"  En düşük: {min(prices):,.0f} TL")
        logger.info(f"  En yüksek: {max(prices):,.0f} TL")
        logger.info(f"  Ortalama: {sum(prices)/len(prices):,.0f} TL")
        
        # CSV'ye kaydet
        scraper.save_to_csv('../data/dataset.csv')
    else:
        logger.warning("Hiç veri çekilemedi!")
    
    return data


if __name__ == "__main__":
    main()
