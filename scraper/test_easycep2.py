import requests
from bs4 import BeautifulSoup
from fake_useragent import UserAgent

ua = UserAgent()
headers = {
    'User-Agent': ua.random,
    'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
    'Accept-Language': 'tr-TR,tr;q=0.9',
}

# Ana sayfadan iPhone linklerini bul
url = 'https://easycep.com'
print(f'Ana sayfa: {url}')

response = requests.get(url, headers=headers, timeout=15)
print(f'Status: {response.status_code}')

soup = BeautifulSoup(response.content, 'html.parser')

# iPhone iceren linkleri bul
links = soup.find_all('a', href=True)
iphone_links = []

for link in links:
    href = link.get('href', '')
    text = link.get_text(strip=True).lower()
    
    if 'iphone' in href.lower() or 'iphone' in text:
        full_url = href if href.startswith('http') else f'https://easycep.com{href}'
        if full_url not in iphone_links:
            iphone_links.append(full_url)
            print(f'  {full_url}')

print(f'\nToplam {len(iphone_links)} iPhone linki bulundu')

# Ilk linki dene
if iphone_links:
    test_url = iphone_links[0]
    print(f'\nTest: {test_url}')
    r2 = requests.get(test_url, headers=headers, timeout=15)
    print(f'Status: {r2.status_code}')
    
    if r2.status_code == 200:
        soup2 = BeautifulSoup(r2.content, 'html.parser')
        products = soup2.find_all(['div', 'a', 'article'], class_=lambda x: x and ('product' in x.lower() or 'card' in x.lower()))
        print(f'Urun sayisi: {len(products)}')

