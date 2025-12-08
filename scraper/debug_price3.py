import requests
from bs4 import BeautifulSoup
import re
from fake_useragent import UserAgent

ua = UserAgent()
headers = {'User-Agent': ua.random}

url = 'https://easycep.com/kategori/iphone-13-pro-118'
response = requests.get(url, headers=headers, timeout=15)
soup = BeautifulSoup(response.content, 'html.parser')

# Tum a taglari
all_links = soup.find_all('a', href=True)
print(f"Toplam link: {len(all_links)}")

# iphone iceren linkler
iphone_links = [a for a in all_links if 'iphone' in a.get('href', '').lower()]
print(f"iPhone iceren: {len(iphone_links)}")

for link in iphone_links[:5]:
    print(f"  {link.get('href')[:80]}")

# Taksit iceren tum metinler
print("\nTaksit metinleri:")
all_text = soup.get_text()
taksit_matches = re.findall(r'(\d+)\s*Ay\s*x\s*([\d.]+),?(\d{0,2})\s*TL', all_text, re.I)
print(f"Taksit eslesen: {len(taksit_matches)}")

for m in taksit_matches[:5]:
    taksit_sayisi = int(m[0])
    taksit_tutari = float(m[1].replace('.', '') + '.' + (m[2] or '00'))
    toplam = taksit_sayisi * taksit_tutari
    print(f"  {taksit_sayisi} x {taksit_tutari:,.2f} = {toplam:,.0f} TL")

