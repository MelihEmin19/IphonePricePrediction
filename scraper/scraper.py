"""
iPhone Fiyat Tahmin Sistemi - Web Scraper
Hepsiburada, N11 ve GittiGidiyor'dan yenilenmiş iPhone ilanlarını toplar
"""

import requests
from bs4 import BeautifulSoup
import time
import random
import re
import json
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
    """iPhone ilan scraper sınıfı - Gerçek sitelerden veri çeker"""
    
    def __init__(self):
        self.ua = UserAgent()
        self.session = requests.Session()
        self.scraped_data: List[Dict] = []
        
    def get_headers(self) -> Dict[str, str]:
        """Random user agent ile header oluştur"""
        return {
            'User-Agent': self.ua.random,
            'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8',
            'Accept-Language': 'tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7',
            'Accept-Encoding': 'gzip, deflate, br',
            'Connection': 'keep-alive',
            'Upgrade-Insecure-Requests': '1',
            'Cache-Control': 'max-age=0'
        }
    
    def extract_model_from_title(self, title: str) -> Optional[str]:
        """Başlıktan iPhone modelini çıkar"""
        title_lower = title.lower()
        
        # En uzun eşleşmeyi bul
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
        price_text = price_text.replace('TL', '').replace('₺', '').strip()
        price_text = price_text.replace('.', '').replace(',', '.')
        
        try:
            price_clean = re.sub(r'[^\d.]', '', price_text)
            price = float(price_clean)
            
            if 5000 <= price <= 100000:
                return price
        except (ValueError, AttributeError):
            pass
        
        return None
    
    def scrape_hepsiburada(self) -> List[Dict]:
        """Hepsiburada'dan yenilenmiş iPhone ilanlarını çek"""
        logger.info("Hepsiburada'dan veri çekiliyor...")
        listings = []
        
        base_url = "https://www.hepsiburada.com"
        search_urls = [
            "/ara?q=yenilenmiş+iphone",
            "/ara?q=ikinci+el+iphone",
            "/ara?q=refurbished+iphone"
        ]
        
        for search_url in search_urls:
            try:
                url = base_url + search_url
                logger.info(f"  URL: {url}")
                
                response = self.session.get(
                    url, 
                    headers=self.get_headers(),
                    timeout=SCRAPER_CONFIG['timeout']
                )
                
                if response.status_code == 200:
                    soup = BeautifulSoup(response.content, 'html.parser')
                    
                    # Ürün kartlarını bul
                    product_cards = soup.find_all('li', {'class': re.compile(r'productListContent')})
                    
                    if not product_cards:
                        # Alternatif selector
                        product_cards = soup.find_all('div', {'data-test-id': 'product-card'})
                    
                    logger.info(f"    {len(product_cards)} ürün bulundu")
                    
                    for card in product_cards[:50]:  # İlk 50 ürün
                        try:
                            # Başlık
                            title_elem = card.find('h3') or card.find('span', {'class': re.compile(r'title')})
                            title = title_elem.get_text(strip=True) if title_elem else ""
                            
                            # iPhone model kontrolü
                            model = self.extract_model_from_title(title)
                            if not model:
                                continue
                            
                            # Fiyat
                            price_elem = card.find('div', {'data-test-id': 'price-current-price'})
                            if not price_elem:
                                price_elem = card.find('span', {'class': re.compile(r'price')})
                            
                            price_text = price_elem.get_text(strip=True) if price_elem else ""
                            price = self.extract_price(price_text)
                            
                            if not price:
                                continue
                            
                            # Storage
                            storage = self.extract_storage_from_title(title) or 128
                            
                            # RAM (modelden)
                            ram = IPHONE_MODELS_MAP.get(model, {}).get('ram', 4)
                            
                            # Durum
                            condition = self.extract_condition(title)
                            
                            listing = {
                                'title': title,
                                'model': model,
                                'model_id': IPHONE_MODELS_MAP.get(model, {}).get('model_id', 1),
                                'ram_gb': ram,
                                'storage_gb': storage,
                                'condition': condition,
                                'price': price,
                                'source': 'Hepsiburada',
                                'url': url
                            }
                            
                            listings.append(listing)
                            logger.debug(f"    ✓ {model} {storage}GB - {price:,.0f} TL")
                            
                        except Exception as e:
                            logger.debug(f"    Ürün parse hatası: {e}")
                            continue
                
                # Rate limiting
                time.sleep(random.uniform(1, 3))
                
            except requests.RequestException as e:
                logger.warning(f"  Hepsiburada bağlantı hatası: {e}")
                continue
        
        logger.info(f"  Hepsiburada'dan toplam {len(listings)} ilan çekildi")
        return listings
    
    def scrape_n11(self) -> List[Dict]:
        """N11'den yenilenmiş iPhone ilanlarını çek"""
        logger.info("N11'den veri çekiliyor...")
        listings = []
        
        base_url = "https://www.n11.com"
        search_urls = [
            "/arama?q=yenilenmiş+iphone",
            "/arama?q=ikinci+el+iphone"
        ]
        
        for search_url in search_urls:
            try:
                url = base_url + search_url
                logger.info(f"  URL: {url}")
                
                response = self.session.get(
                    url,
                    headers=self.get_headers(),
                    timeout=SCRAPER_CONFIG['timeout']
                )
                
                if response.status_code == 200:
                    soup = BeautifulSoup(response.content, 'html.parser')
                    
                    # Ürün kartlarını bul
                    product_cards = soup.find_all('li', {'class': 'columnContent'})
                    
                    if not product_cards:
                        product_cards = soup.find_all('div', {'class': re.compile(r'product')})
                    
                    logger.info(f"    {len(product_cards)} ürün bulundu")
                    
                    for card in product_cards[:50]:
                        try:
                            # Başlık
                            title_elem = card.find('h3', {'class': 'productName'})
                            if not title_elem:
                                title_elem = card.find('a', {'class': re.compile(r'title')})
                            
                            title = title_elem.get_text(strip=True) if title_elem else ""
                            
                            model = self.extract_model_from_title(title)
                            if not model:
                                continue
                            
                            # Fiyat
                            price_elem = card.find('ins', {'class': 'newPrice'})
                            if not price_elem:
                                price_elem = card.find('span', {'class': re.compile(r'price')})
                            
                            price_text = price_elem.get_text(strip=True) if price_elem else ""
                            price = self.extract_price(price_text)
                            
                            if not price:
                                continue
                            
                            storage = self.extract_storage_from_title(title) or 128
                            ram = IPHONE_MODELS_MAP.get(model, {}).get('ram', 4)
                            condition = self.extract_condition(title)
                            
                            listing = {
                                'title': title,
                                'model': model,
                                'model_id': IPHONE_MODELS_MAP.get(model, {}).get('model_id', 1),
                                'ram_gb': ram,
                                'storage_gb': storage,
                                'condition': condition,
                                'price': price,
                                'source': 'N11',
                                'url': url
                            }
                            
                            listings.append(listing)
                            
                        except Exception as e:
                            logger.debug(f"    Ürün parse hatası: {e}")
                            continue
                
                time.sleep(random.uniform(1, 3))
                
            except requests.RequestException as e:
                logger.warning(f"  N11 bağlantı hatası: {e}")
                continue
        
        logger.info(f"  N11'den toplam {len(listings)} ilan çekildi")
        return listings
    
    def scrape_gittigidiyor(self) -> List[Dict]:
        """GittiGidiyor'dan (artık Hepsiburada'ya yönleniyor) veri çek"""
        logger.info("GittiGidiyor'dan veri çekiliyor...")
        listings = []
        
        # GittiGidiyor artık Hepsiburada'ya yönleniyor
        base_url = "https://www.gittigidiyor.com"
        search_url = "/arama/?k=yenilenmiş+iphone"
        
        try:
            url = base_url + search_url
            logger.info(f"  URL: {url}")
            
            response = self.session.get(
                url,
                headers=self.get_headers(),
                timeout=SCRAPER_CONFIG['timeout'],
                allow_redirects=True
            )
            
            if response.status_code == 200:
                soup = BeautifulSoup(response.content, 'html.parser')
                
                product_cards = soup.find_all('li', {'class': re.compile(r'product')})
                logger.info(f"    {len(product_cards)} ürün bulundu")
                
                for card in product_cards[:50]:
                    try:
                        title_elem = card.find('a', {'class': re.compile(r'title')})
                        title = title_elem.get_text(strip=True) if title_elem else ""
                        
                        model = self.extract_model_from_title(title)
                        if not model:
                            continue
                        
                        price_elem = card.find('p', {'class': re.compile(r'price')})
                        price_text = price_elem.get_text(strip=True) if price_elem else ""
                        price = self.extract_price(price_text)
                        
                        if not price:
                            continue
                        
                        storage = self.extract_storage_from_title(title) or 128
                        ram = IPHONE_MODELS_MAP.get(model, {}).get('ram', 4)
                        condition = self.extract_condition(title)
                        
                        listing = {
                            'title': title,
                            'model': model,
                            'model_id': IPHONE_MODELS_MAP.get(model, {}).get('model_id', 1),
                            'ram_gb': ram,
                            'storage_gb': storage,
                            'condition': condition,
                            'price': price,
                            'source': 'GittiGidiyor',
                            'url': url
                        }
                        
                        listings.append(listing)
                        
                    except Exception as e:
                        logger.debug(f"    Ürün parse hatası: {e}")
                        continue
            
            time.sleep(random.uniform(1, 3))
            
        except requests.RequestException as e:
            logger.warning(f"  GittiGidiyor bağlantı hatası: {e}")
        
        logger.info(f"  GittiGidiyor'dan toplam {len(listings)} ilan çekildi")
        return listings
    
    def scrape_trendyol(self) -> List[Dict]:
        """Trendyol'dan yenilenmiş iPhone ilanlarını çek"""
        logger.info("Trendyol'dan veri çekiliyor...")
        listings = []
        
        base_url = "https://www.trendyol.com"
        search_url = "/sr?q=yenilenmiş+iphone"
        
        try:
            url = base_url + search_url
            logger.info(f"  URL: {url}")
            
            response = self.session.get(
                url,
                headers=self.get_headers(),
                timeout=SCRAPER_CONFIG['timeout']
            )
            
            if response.status_code == 200:
                soup = BeautifulSoup(response.content, 'html.parser')
                
                # Trendyol'un ürün kartları
                product_cards = soup.find_all('div', {'class': re.compile(r'p-card')})
                logger.info(f"    {len(product_cards)} ürün bulundu")
                
                for card in product_cards[:50]:
                    try:
                        # Başlık
                        title_elem = card.find('span', {'class': re.compile(r'prdct-desc')})
                        title = title_elem.get_text(strip=True) if title_elem else ""
                        
                        model = self.extract_model_from_title(title)
                        if not model:
                            continue
                        
                        # Fiyat
                        price_elem = card.find('div', {'class': re.compile(r'prc-box')})
                        price_text = price_elem.get_text(strip=True) if price_elem else ""
                        price = self.extract_price(price_text)
                        
                        if not price:
                            continue
                        
                        storage = self.extract_storage_from_title(title) or 128
                        ram = IPHONE_MODELS_MAP.get(model, {}).get('ram', 4)
                        condition = self.extract_condition(title)
                        
                        listing = {
                            'title': title,
                            'model': model,
                            'model_id': IPHONE_MODELS_MAP.get(model, {}).get('model_id', 1),
                            'ram_gb': ram,
                            'storage_gb': storage,
                            'condition': condition,
                            'price': price,
                            'source': 'Trendyol',
                            'url': url
                        }
                        
                        listings.append(listing)
                        
                    except Exception as e:
                        logger.debug(f"    Ürün parse hatası: {e}")
                        continue
            
            time.sleep(random.uniform(1, 3))
            
        except requests.RequestException as e:
            logger.warning(f"  Trendyol bağlantı hatası: {e}")
        
        logger.info(f"  Trendyol'dan toplam {len(listings)} ilan çekildi")
        return listings
    
    def run(self) -> List[Dict]:
        """Ana scraping fonksiyonu - Tüm sitelerden veri toplar"""
        logger.info("="*60)
        logger.info("Web Scraping başlatıldı...")
        logger.info("="*60)
        
        all_listings = []
        
        # Hepsiburada
        try:
            hb_data = self.scrape_hepsiburada()
            all_listings.extend(hb_data)
        except Exception as e:
            logger.error(f"Hepsiburada scraping hatası: {e}")
        
        # N11
        try:
            n11_data = self.scrape_n11()
            all_listings.extend(n11_data)
        except Exception as e:
            logger.error(f"N11 scraping hatası: {e}")
        
        # GittiGidiyor
        try:
            gg_data = self.scrape_gittigidiyor()
            all_listings.extend(gg_data)
        except Exception as e:
            logger.error(f"GittiGidiyor scraping hatası: {e}")
        
        # Trendyol
        try:
            ty_data = self.scrape_trendyol()
            all_listings.extend(ty_data)
        except Exception as e:
            logger.error(f"Trendyol scraping hatası: {e}")
        
        self.scraped_data = all_listings
        
        logger.info("="*60)
        logger.info(f"Scraping tamamlandı. Toplam {len(all_listings)} ilan bulundu")
        logger.info("="*60)
        
        return all_listings


def main():
    """Ana fonksiyon"""
    scraper = IPhoneScraper()
    data = scraper.run()
    
    logger.info(f"\n{'='*60}")
    logger.info(f"ÖZET İSTATİSTİKLER")
    logger.info(f"{'='*60}")
    logger.info(f"Toplam ilan sayısı: {len(data)}")
    
    if data:
        # Kaynak dağılımı
        sources = {}
        for item in data:
            source = item['source']
            sources[source] = sources.get(source, 0) + 1
        
        logger.info(f"\nKaynak Dağılımı:")
        for source, count in sorted(sources.items(), key=lambda x: x[1], reverse=True):
            logger.info(f"  {source}: {count} ilan")
        
        # Model dağılımı
        models = {}
        for item in data:
            model = item['model']
            models[model] = models.get(model, 0) + 1
        
        logger.info(f"\nModel Dağılımı (İlk 10):")
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
