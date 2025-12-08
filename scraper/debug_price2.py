import requests
from bs4 import BeautifulSoup
import re
from fake_useragent import UserAgent

ua = UserAgent()
headers = {'User-Agent': ua.random}

url = 'https://easycep.com/kategori/iphone-13-pro-118'
response = requests.get(url, headers=headers, timeout=15)
soup = BeautifulSoup(response.content, 'html.parser')

# Urun kartlarini bul
print("HTML yapisini incele:")

# a tag'leri ile iphone linklerini bul
links = soup.find_all('a', href=lambda x: x and 'apple-iphone' in x.lower())
print(f"\niPhone linkleri: {len(links)}")

for i, link in enumerate(links[:3]):
    print(f"\n--- Link {i+1} ---")
    print(f"HREF: {link.get('href')[:60]}...")
    
    # Parent'a cik
    parent = link.find_parent()
    for _ in range(5):  # 5 seviye yukari cik
        if parent:
            parent_text = parent.get_text()
            # Taksit pattern'i ara
            taksit = re.search(r'(\d+)\s*Ay\s*x\s*([\d.]+),?(\d{0,2})\s*TL', parent_text, re.I)
            if taksit:
                taksit_sayisi = int(taksit.group(1))
                taksit_tutari = float(taksit.group(2).replace('.', '') + '.' + (taksit.group(3) or '00'))
                toplam = taksit_sayisi * taksit_tutari
                print(f"BULUNDU! {taksit_sayisi} x {taksit_tutari:,.0f} = {toplam:,.0f} TL")
                break
            parent = parent.find_parent()
    else:
        print("Fiyat bulunamadi")

