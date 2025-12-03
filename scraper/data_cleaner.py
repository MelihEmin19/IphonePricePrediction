"""
Data Cleaner - Scrape edilen verileri temizler ve veritabanına yazar
"""

import psycopg2
from psycopg2.extras import execute_values
import logging
from typing import List, Dict, Tuple
from config import DB_CONFIG

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class DataCleaner:
    """Veri temizleme ve veritabanına yazma sınıfı"""
    
    def __init__(self):
        self.conn = None
        self.cursor = None
        
    def connect_db(self):
        """Veritabanı bağlantısı kur"""
        try:
            self.conn = psycopg2.connect(**DB_CONFIG)
            self.cursor = self.conn.cursor()
            logger.info("Veritabanı bağlantısı başarılı")
        except Exception as e:
            logger.error(f"Veritabanı bağlantı hatası: {e}")
            raise
    
    def close_db(self):
        """Veritabanı bağlantısını kapat"""
        if self.cursor:
            self.cursor.close()
        if self.conn:
            self.conn.close()
        logger.info("Veritabanı bağlantısı kapatıldı")
    
    def get_or_create_spec(self, model_id: int, ram_gb: int, storage_gb: int) -> int:
        """Spec var mı kontrol et, yoksa oluştur, ID'yi döndür"""
        # Önce var mı bak
        self.cursor.execute("""
            SELECT id FROM specs
            WHERE model_id = %s AND ram_gb = %s AND storage_gb = %s
        """, (model_id, ram_gb, storage_gb))
        
        result = self.cursor.fetchone()
        if result:
            return result[0]
        
        # Yoksa oluştur
        self.cursor.execute("""
            INSERT INTO specs (model_id, ram_gb, storage_gb)
            VALUES (%s, %s, %s)
            RETURNING id
        """, (model_id, ram_gb, storage_gb))
        
        self.conn.commit()
        return self.cursor.fetchone()[0]
    
    def clean_duplicates(self, data: List[Dict]) -> List[Dict]:
        """Duplicate kayıtları temizle"""
        seen = set()
        cleaned_data = []
        
        for item in data:
            # Benzersiz bir key oluştur
            key = (
                item['model_id'],
                item['ram_gb'],
                item['storage_gb'],
                item['condition'],
                item['price'],
                item['source']
            )
            
            if key not in seen:
                seen.add(key)
                cleaned_data.append(item)
        
        removed = len(data) - len(cleaned_data)
        if removed > 0:
            logger.info(f"{removed} duplicate kayıt temizlendi")
        
        return cleaned_data
    
    def validate_data(self, data: List[Dict]) -> Tuple[List[Dict], List[Dict]]:
        """Verileri doğrula, geçersizleri ayır"""
        valid_data = []
        invalid_data = []
        
        for item in data:
            try:
                # Zorunlu alanlar kontrolü
                assert 'model_id' in item and item['model_id'] > 0
                assert 'ram_gb' in item and item['ram_gb'] > 0
                assert 'storage_gb' in item and item['storage_gb'] > 0
                assert 'price' in item and 5000 <= item['price'] <= 100000
                assert 'condition' in item and item['condition'] in ['Mükemmel', 'Çok İyi', 'İyi', 'Orta']
                assert 'source' in item and len(item['source']) > 0
                
                valid_data.append(item)
            except (AssertionError, KeyError) as e:
                invalid_data.append(item)
                logger.warning(f"Geçersiz veri: {item} - Hata: {e}")
        
        logger.info(f"Doğrulama: {len(valid_data)} geçerli, {len(invalid_data)} geçersiz")
        return valid_data, invalid_data
    
    def bulk_insert_listings(self, data: List[Dict]) -> int:
        """Toplu ilan ekleme (Stored Procedure kullanarak)"""
        inserted_count = 0
        skipped_count = 0
        
        for item in data:
            try:
                # Önce spec_id'yi al veya oluştur
                spec_id = self.get_or_create_spec(
                    item['model_id'],
                    item['ram_gb'],
                    item['storage_gb']
                )
                
                # Stored procedure ile ekle
                self.cursor.execute("""
                    SELECT sp_InsertListing(%s, %s, %s, %s, %s)
                """, (
                    spec_id,
                    item['price'],
                    item['condition'],
                    item['source'],
                    item.get('url', '')
                ))
                
                result = self.cursor.fetchone()[0]
                
                if result > 0:
                    inserted_count += 1
                else:
                    skipped_count += 1
                
            except Exception as e:
                logger.error(f"Ekleme hatası: {item} - {e}")
                self.conn.rollback()
                continue
        
        self.conn.commit()
        logger.info(f"Ekleme tamamlandı: {inserted_count} yeni, {skipped_count} duplicate")
        
        return inserted_count
    
    def process(self, raw_data: List[Dict]) -> Dict[str, int]:
        """Ana işleme fonksiyonu"""
        logger.info(f"Veri işleme başladı: {len(raw_data)} kayıt")
        
        stats = {
            'total_raw': len(raw_data),
            'duplicates_removed': 0,
            'invalid_removed': 0,
            'inserted': 0
        }
        
        try:
            self.connect_db()
            
            # 1. Duplicate'leri temizle
            cleaned_data = self.clean_duplicates(raw_data)
            stats['duplicates_removed'] = stats['total_raw'] - len(cleaned_data)
            
            # 2. Doğrulama
            valid_data, invalid_data = self.validate_data(cleaned_data)
            stats['invalid_removed'] = len(invalid_data)
            
            # 3. Veritabanına ekle
            if valid_data:
                stats['inserted'] = self.bulk_insert_listings(valid_data)
            
            logger.info(f"İşlem tamamlandı: {stats}")
            
        except Exception as e:
            logger.error(f"İşlem hatası: {e}")
            raise
        finally:
            self.close_db()
        
        return stats


def main():
    """Test fonksiyonu"""
    # Örnek veri
    test_data = [
        {
            'model': 'iPhone 13',
            'model_id': 8,
            'ram_gb': 4,
            'storage_gb': 128,
            'condition': 'Mükemmel',
            'price': 20000.00,
            'source': 'Test',
            'url': 'https://test.com/1'
        },
        {
            'model': 'iPhone 13',
            'model_id': 8,
            'ram_gb': 4,
            'storage_gb': 256,
            'condition': 'İyi',
            'price': 22000.00,
            'source': 'Test',
            'url': 'https://test.com/2'
        }
    ]
    
    cleaner = DataCleaner()
    stats = cleaner.process(test_data)
    
    logger.info(f"Test tamamlandı: {stats}")


if __name__ == "__main__":
    main()

