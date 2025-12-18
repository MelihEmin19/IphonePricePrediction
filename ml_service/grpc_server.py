"""
gRPC Sunucu - Fiyat tahmin servisi
Port 50051'de çalışır ve Node.js API'den gelen istekleri karşılar
"""

import grpc
from concurrent import futures
import time
import logging
from datetime import datetime

# Proto import (önce generate_grpc.py çalıştırılmalı)
try:
    from proto import prediction_pb2
    from proto import prediction_pb2_grpc
except ImportError:
    print("✗ gRPC kodları bulunamadı!")
    print("Önce şunu çalıştırın: python generate_grpc.py")
    exit(1)

from predictor import PricePredictor
from config import GRPC_CONFIG, IPHONE_MODELS

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# Model ID -> Model Name mapping (Veritabanındaki ID'lerle eşleşik)
MODEL_ID_MAP = {
    1: 'iPhone 8', 2: 'iPhone SE 2020', 3: 'iPhone 8 Plus',
    4: 'iPhone X', 5: 'iPhone XR', 6: 'iPhone XS',
    7: 'iPhone 11', 8: 'iPhone 12 Mini', 9: 'iPhone 12',
    10: 'iPhone 11 Pro Max', 11: 'iPhone 11 Pro',
    12: 'iPhone 13', 13: 'iPhone 12 Pro', 14: 'iPhone 13 Mini',
    15: 'iPhone 12 Pro Max', 16: 'iPhone 14 Plus', 17: 'iPhone 14',
    18: 'iPhone 13 Pro', 19: 'iPhone 15', 20: 'iPhone 13 Pro Max',
    21: 'iPhone 14 Pro', 22: 'iPhone 15 Plus', 23: 'iPhone 14 Pro Max',
    24: 'iPhone 15 Pro', 25: 'iPhone 15 Pro Max',
    26: 'iPhone 16 Pro Max', 27: 'iPhone 16 Pro', 28: 'iPhone 16'
}


class PricePredictionServicer(prediction_pb2_grpc.PricePredictionServicer):
    """gRPC Servicer implementasyonu"""
    
    def __init__(self):
        self.predictor = PricePredictor()
        self.start_time = datetime.now()
        logger.info("PricePredictionServicer başlatıldı")
    
    def PredictPrice(self, request, context):
        """Fiyat tahmini yap"""
        try:
            # Model ID'den model adını bul
            model_name = MODEL_ID_MAP.get(request.model_id, 'iPhone 13')
            
            # "Apple " prefix'ini kaldır
            if model_name.startswith('Apple '):
                model_name = model_name.replace('Apple ', '')
            
            logger.info(f"Tahmin isteği: {model_name}, "
                       f"RAM={request.ram_gb}GB, Storage={request.storage_gb}GB, "
                       f"Condition={request.condition}")
            
            # Input'u dict'e çevir (yeni format)
            input_data = {
                'model_name': model_name,
                'ram_gb': request.ram_gb,
                'storage_gb': request.storage_gb,
                'condition': request.condition
            }
            
            # Tahmin yap
            result = self.predictor.predict(input_data)
            
            # Response oluştur
            response = prediction_pb2.PriceResponse(
                predicted_price=result['predicted_price'],
                confidence_score=result['confidence_score'],
                price_range=prediction_pb2.PriceRange(
                    min_price=result['price_range']['min'],
                    max_price=result['price_range']['max']
                ),
                status='success',
                message=f"Tahmin başarılı: {result['predicted_price']:,.0f} TL"
            )
            
            logger.info(f"Tahmin döndürüldü: {result['predicted_price']:,.0f} TL")
            return response
            
        except Exception as e:
            logger.error(f"Tahmin hatası: {e}", exc_info=True)
            
            # Hata response'u
            return prediction_pb2.PriceResponse(
                predicted_price=0,
                confidence_score=0,
                price_range=prediction_pb2.PriceRange(min_price=0, max_price=0),
                status='error',
                message=str(e)
            )
    
    def GetModelInfo(self, request, context):
        """Model bilgilerini döndür"""
        model_name = MODEL_ID_MAP.get(request.model_id, 'iPhone 13')
        info = IPHONE_MODELS.get(model_name, IPHONE_MODELS.get('iPhone 13'))
        
        # Storage seçenekleri segment'e göre
        storage_options = [64, 128, 256] if info.get('segment', 2) <= 2 else [128, 256, 512, 1024]
        
        return prediction_pb2.ModelInfoResponse(
            model_name=model_name,
            release_year=info.get('yil', 2021),
            available_storage=storage_options,
            ram_gb=info.get('ram', 4),
            is_pro='Pro' in model_name
        )
    
    def HealthCheck(self, request, context):
        """Servis sağlık durumu"""
        uptime = datetime.now() - self.start_time
        
        model_loaded = self.predictor.model is not None
        status = 'healthy' if model_loaded else 'unhealthy'
        
        logger.info(f"Health check: {status}")
        
        return prediction_pb2.HealthCheckResponse(
            status=status,
            version='2.0.0',
            model_loaded=model_loaded,
            uptime=str(uptime)
        )


def serve():
    """gRPC sunucusunu başlat"""
    server = grpc.server(
        futures.ThreadPoolExecutor(max_workers=GRPC_CONFIG['max_workers'])
    )
    
    prediction_pb2_grpc.add_PricePredictionServicer_to_server(
        PricePredictionServicer(), server
    )
    
    address = f"{GRPC_CONFIG['host']}:{GRPC_CONFIG['port']}"
    server.add_insecure_port(address)
    
    server.start()
    
    logger.info("="*60)
    logger.info("gRPC SUNUCU BAŞLATILDI")
    logger.info("="*60)
    logger.info(f"Adres: {address}")
    logger.info(f"Model: Gradient Boosting (R² = 0.9988)")
    logger.info(f"Veri Seti: 1198 kayıt")
    logger.info("="*60)
    logger.info("\nSunucu çalışıyor... (Durdurmak için Ctrl+C)")
    
    try:
        while True:
            time.sleep(86400)  # 24 saat
    except KeyboardInterrupt:
        logger.info("\n\nSunucu durduruluyor...")
        server.stop(0)
        logger.info("Sunucu durduruldu")


if __name__ == "__main__":
    serve()
