import requests
from bs4 import BeautifulSoup
from fake_useragent import UserAgent
import re
import time

ua = UserAgent()
headers = {
    'User-Agent': ua.random,
    'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
    'Accept-Language': 'tr-TR,tr;q=0.9',
}

# EasyCep kategori sayfalari
kategori_urls = [
    'https://easycep.com/kategori/iphone-11-64',
    'https://easycep.com/kategori/iphone-12-67',
    'https://easycep.com/kategori/iphone-13-116',
    'https://easycep.com/kategori/iphone-13-pro-118',
    'https://easycep.com/kategori/iphone-14-738',
    'https://easycep.com/kategori/iphone-14-pro-769',
    'https://easycep.com/kategori/iphone-14-pro-max-772',
    'https://easycep.com/kategori/iphone-15-935',
    'https://easycep.com/kategori/iphone-15-pro-937',
    'https://easycep.com/kategori/iphone-15-pro-max-938',
]

all_products = []

for url in kategori_urls[:3]:  # Ilk 3 kategoriyi test et
    print(f'\n{url}')
    
    try:
        response = requests.get(url, headers=headers, timeout=15)
        
        if response.status_code == 200:
            soup = BeautifulSoup(response.content, 'html.parser')
            
            # Urun kartlarini bul
            products = soup.find_all('a', class_=lambda x: x and 'ProductCard' in str(x))
            if not products:
                products = soup.find_all('div', class_=lambda x: x and 'product' in str(x).lower())
            if not products:
                products = soup.find_all('article')
            
            print(f'  Bulunan: {len(products)} urun')
            
            for prod in products[:5]:
                # Baslik
                title_elem = prod.find(['h2', 'h3', 'span'], class_=lambda x: x and 'title' in str(x).lower())
                if not title_elem:
                    title_elem = prod.find(['h2', 'h3', 'p'])
                
                title = title_elem.get_text(strip=True) if title_elem else prod.get_text(strip=True)[:50]
                
                # Fiyat
                price_elem = prod.find(['span', 'div', 'p'], class_=lambda x: x and 'price' in str(x).lower())
                price_text = price_elem.get_text(strip=True) if price_elem else 'N/A'
                
                # Fiyati parse et
                price_match = re.search(r'[\d.,]+', price_text.replace('.', '').replace(',', '.'))
                price = float(price_match.group()) if price_match else 0
                
                print(f'    {title[:40]:40} - {price_text}')
                
                if price > 5000:
                    all_products.append({
                        'title': title,
                        'price': price,
                        'source': 'EasyCep'
                    })
        
        time.sleep(1)
        
    except Exception as e:
        print(f'  Hata: {e}')

print(f'\n\n=== SONUC ===')
print(f'Toplam {len(all_products)} urun cekildi')

for p in all_products[:10]:
    print(f"  {p['title'][:40]} - {p['price']:,.0f} TL")

