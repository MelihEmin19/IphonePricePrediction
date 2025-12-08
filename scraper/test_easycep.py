import requests
from bs4 import BeautifulSoup
from fake_useragent import UserAgent

ua = UserAgent()
headers = {
    'User-Agent': ua.random,
    'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
    'Accept-Language': 'tr-TR,tr;q=0.9',
}

# EasyCep URL'lerini dene
urls = [
    'https://www.easycep.com/yenilenmis-telefonlar/apple',
    'https://www.easycep.com/apple-iphone',
    'https://www.easycep.com/telefonlar/apple',
    'https://easycep.com/yenilenmis-iphone',
]

for url in urls:
    print(f'\nURL: {url}')
    try:
        response = requests.get(url, headers=headers, timeout=15)
        print(f'Status: {response.status_code}')
        
        if response.status_code == 200:
            soup = BeautifulSoup(response.content, 'html.parser')
            title = soup.find('title')
            print(f'Title: {title.get_text() if title else "Yok"}')
            
            # Ürün kartları
            products = soup.find_all(['div', 'a'], class_=lambda x: x and 'product' in x.lower())
            print(f'Urun sayisi: {len(products)}')
            
            if products:
                print('BASARILI!')
                break
                
    except Exception as e:
        print(f'Hata: {e}')

