"""
Ana scraper çalıştırıcı - Scrape et ve veritabanına kaydet
"""

import logging
from scraper import IPhoneScraper
from data_cleaner import DataCleaner

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


def main():
    """Scraping ve veri temizleme pipeline'ı"""
    logger.info("="*60)
    logger.info("iPhone Fiyat Scraper Başlatıldı")
    logger.info("="*60)
    
    try:
        # 1. Veri topla
        logger.info("\n[1/2] Veri toplama başladı...")
        scraper = IPhoneScraper()
        raw_data = scraper.run()
        
        if not raw_data:
            logger.warning("Hiç veri toplanamadı!")
            return
        
        # 2. Temizle ve veritabanına yaz
        logger.info(f"\n[2/2] Veri temizleme ve veritabanına yazma...")
        cleaner = DataCleaner()
        stats = cleaner.process(raw_data)
        
        # Sonuç raporu
        logger.info("\n" + "="*60)
        logger.info("SONUÇ RAPORU")
        logger.info("="*60)
        logger.info(f"Toplam toplanan veri: {stats['total_raw']}")
        logger.info(f"Duplicate temizlenen: {stats['duplicates_removed']}")
        logger.info(f"Geçersiz veri: {stats['invalid_removed']}")
        logger.info(f"Veritabanına eklenen: {stats['inserted']}")
        logger.info("="*60 + "\n")
        
        logger.info("✓ İşlem başarıyla tamamlandı!")
        
    except KeyboardInterrupt:
        logger.warning("\nKullanıcı tarafından durduruldu")
    except Exception as e:
        logger.error(f"\n✗ Hata oluştu: {e}", exc_info=True)
        raise


if __name__ == "__main__":
    main()

