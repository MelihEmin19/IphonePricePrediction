import requests
from bs4 import BeautifulSoup
import re
from fake_useragent import UserAgent

ua = UserAgent()
headers = {
    'User-Agent': ua.random,
    'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
}

url = 'https://easycep.com/kategori/iphone-13-pro-118'
response = requests.get(url, headers=headers, timeout=15)
soup = BeautifulSoup(response.content, 'html.parser')

# Tum TL iceren metinleri bul
tl_texts = soup.find_all(string=re.compile(r'TL'))

print("TL iceren metinler (ilk 20):")
for i, text in enumerate(tl_texts[:20]):
    clean = text.strip()
    if clean and len(clean) < 100:
        print(f"{i}: [{clean}]")
        
        # Fiyat pattern'i dene
        matches = re.findall(r'([\d.]+),?(\d{0,2})\s*TL', clean)
        for m in matches:
            integer = m[0].replace('.', '')
            decimal = m[1] if m[1] else '00'
            price = float(f"{integer}.{decimal}")
            print(f"   -> Parse: {price:,.0f} TL")

