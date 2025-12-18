"""
Veritabanı Başlatma Script'i
CSV'den verileri okuyarak PostgreSQL veritabanını oluşturur ve doldurur.
"""

import os
import sys
import pandas as pd
import psycopg

# Veritabanı bağlantı bilgileri
DB_HOST = 'localhost'
DB_PORT = 5432
DB_NAME = 'iphone_price_db'
DB_USER = 'postgres'
DB_PASSWORD = 'postgres123'

# Dosya yolları
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
CSV_PATH = os.path.join(BASE_DIR, '..', 'data', 'dataset.csv')

def get_connection(database='postgres'):
    """Veritabanı bağlantısı oluşturur"""
    return psycopg.connect(
        f"postgresql://{DB_USER}:{DB_PASSWORD}@{DB_HOST}:{DB_PORT}/{database}",
        autocommit=True
    )

def create_database():
    """Veritabanını oluşturur (yoksa)"""
    print("\n[0] Veritabani kontrol ediliyor...")
    conn = get_connection('postgres')
    cursor = conn.cursor()
    
    # Veritabanı var mı kontrol et
    cursor.execute("SELECT 1 FROM pg_database WHERE datname = %s", (DB_NAME,))
    exists = cursor.fetchone()
    
    if not exists:
        print(f"  Veritabani olusturuluyor: {DB_NAME}")
        cursor.execute(f'CREATE DATABASE {DB_NAME}')
        print("  [OK] Veritabani olusturuldu")
    else:
        print(f"  [OK] Veritabani mevcut: {DB_NAME}")
    
    conn.close()

def execute_sql_file(cursor, filepath):
    """SQL dosyasını çalıştırır"""
    print(f"  Calistiriliyor: {os.path.basename(filepath)}")
    with open(filepath, 'r', encoding='utf-8') as f:
        sql = f.read()
        cursor.execute(sql)

def load_csv_data(cursor):
    """CSV'den verileri yükler"""
    print("\n[5] CSV verileri yukleniyor...")
    
    # CSV'yi oku
    df = pd.read_csv(CSV_PATH)
    print(f"  CSV'den {len(df)} satir okundu")
    
    # 1. Benzersiz modelleri ekle
    print("  Modeller ekleniyor...")
    models = df[['cihaz_isim', 'segment_puan', 'cikis_yili', 'model_kodu']].drop_duplicates()
    
    model_count = 0
    for _, row in models.iterrows():
        try:
            cursor.execute("""
                INSERT INTO models (model_name, segment_id, release_year, model_kodu)
                VALUES (%s, %s, %s, %s)
                ON CONFLICT (model_name, segment_id) DO NOTHING
            """, (row['cihaz_isim'], int(row['segment_puan']), int(row['cikis_yili']), int(row['model_kodu'])))
            model_count += 1
        except Exception as e:
            print(f"    Model hatasi: {row['cihaz_isim']} - {e}")
    
    print(f"    {model_count} model islendi")
    
    # 2. Benzersiz specs'leri ekle
    print("  Specs ekleniyor...")
    specs = df[['cihaz_isim', 'segment_puan', 'ram_gb', 'storage_gb', 'kamera_mp', 'ekran_boyutu', 'batarya_mah']].drop_duplicates()
    
    specs_count = 0
    for _, row in specs.iterrows():
        try:
            # Model ID'yi bul
            cursor.execute("""
                SELECT model_id FROM models 
                WHERE model_name = %s AND segment_id = %s
            """, (row['cihaz_isim'], int(row['segment_puan'])))
            
            result = cursor.fetchone()
            if result:
                model_id = result[0]
                cursor.execute("""
                    INSERT INTO specs (model_id, ram_gb, storage_gb, kamera_mp, ekran_boyutu, batarya_mah)
                    VALUES (%s, %s, %s, %s, %s, %s)
                    ON CONFLICT (model_id, ram_gb, storage_gb) DO NOTHING
                """, (model_id, int(row['ram_gb']), int(row['storage_gb']), int(row['kamera_mp']), float(row['ekran_boyutu']), int(row['batarya_mah'])))
                specs_count += 1
        except Exception as e:
            print(f"    Specs hatasi: {e}")
    
    print(f"    {specs_count} specs islendi")

def init_database():
    """Ana fonksiyon - veritabanını başlatır"""
    print("=" * 50)
    print("iPhone Fiyat Tahmin - Veritabani Baslatiyor")
    print("=" * 50)
    
    try:
        # 0. Veritabanını oluştur
        create_database()
        
        # Ana veritabanına bağlan
        conn = get_connection(DB_NAME)
        cursor = conn.cursor()
        
        # 1. Schema oluştur
        print("\n[1] Schema olusturuluyor...")
        execute_sql_file(cursor, os.path.join(BASE_DIR, 'schema.sql'))
        print("  [OK] Schema olusturuldu")
        
        # 2. Fonksiyonları oluştur
        print("\n[2] Fonksiyonlar olusturuluyor...")
        execute_sql_file(cursor, os.path.join(BASE_DIR, 'user_functions.sql'))
        print("  [OK] Fonksiyonlar olusturuldu")
        
        # 3. View'ları oluştur
        print("\n[3] View'lar olusturuluyor...")
        execute_sql_file(cursor, os.path.join(BASE_DIR, 'views.sql'))
        print("  [OK] View'lar olusturuldu")
        
        # 4. Stored Procedure'ları oluştur
        print("\n[4] Stored Procedure'lar olusturuluyor...")
        execute_sql_file(cursor, os.path.join(BASE_DIR, 'stored_procedures.sql'))
        print("  [OK] Stored Procedure'lar olusturuldu")
        
        # 5. CSV verilerini yükle
        load_csv_data(cursor)
        
        print("\n" + "=" * 50)
        print("[OK] Veritabani basariyla olusturuldu!")
        print("=" * 50)
        
        # İstatistikleri göster
        cursor.execute("SELECT COUNT(*) FROM segments")
        print(f"  Segments: {cursor.fetchone()[0]}")
        
        cursor.execute("SELECT COUNT(*) FROM conditions")
        print(f"  Conditions: {cursor.fetchone()[0]}")
        
        cursor.execute("SELECT COUNT(*) FROM models")
        print(f"  Models: {cursor.fetchone()[0]}")
        
        cursor.execute("SELECT COUNT(*) FROM specs")
        print(f"  Specs: {cursor.fetchone()[0]}")
        
        cursor.execute("SELECT COUNT(*) FROM users")
        print(f"  Users: {cursor.fetchone()[0]}")
        
        cursor.execute("SELECT COUNT(*) FROM roles")
        print(f"  Roles: {cursor.fetchone()[0]}")
        
    except psycopg.Error as e:
        print(f"\n[HATA] Veritabani hatasi: {e}")
        sys.exit(1)
    except FileNotFoundError as e:
        print(f"\n[HATA] Dosya bulunamadi: {e}")
        sys.exit(1)
    finally:
        if 'conn' in locals():
            conn.close()

if __name__ == '__main__':
    init_database()
