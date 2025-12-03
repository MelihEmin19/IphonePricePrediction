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
from config import GRPC_CONFIG

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


class PricePredictionServicer(prediction_pb2_grpc.PricePredictionServicer):
    """gRPC Servicer implementasyonu"""
    
    def __init__(self):
        self.predictor = PricePredictor()
        self.start_time = datetime.now()
        logger.info("PricePredictionServicer başlatıldı")
    
    def PredictPrice(self, request, context):
        """Fiyat tahmini yap"""
        try:
            logger.info(f"Tahmin isteği alındı: Model ID={request.model_id}, "
                       f"RAM={request.ram_gb}GB, Storage={request.storage_gb}GB, "
                       f"Condition={request.condition}")
            
            # Input'u dict'e çevir
            input_data = {
                'model_id': request.model_id,
                'ram_gb': request.ram_gb,
                'storage_gb': request.storage_gb,
                'condition': request.condition,
                'release_year': request.release_year if request.release_year > 0 else 2020
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
                message=f"Tahmin başarılı: {result['predicted_price']:,.2f} TL"
            )
            
            logger.info(f"Tahmin döndürüldü: {result['predicted_price']:,.2f} TL")
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
        # Basit model mapping (gerçekte DB'den çekilebilir)
        model_info = {
            1: {'name': 'iPhone 11', 'year': 2019, 'storage': [64, 128, 256], 'ram': 4, 'pro': False},
            8: {'name': 'iPhone 13', 'year': 2021, 'storage': [128, 256, 512], 'ram': 4, 'pro': False},
            10: {'name': 'iPhone 13 Pro', 'year': 2021, 'storage': [128, 256, 512, 1024], 'ram': 6, 'pro': True},
            16: {'name': 'iPhone 15', 'year': 2023, 'storage': [128, 256, 512], 'ram': 6, 'pro': False},
            18: {'name': 'iPhone 15 Pro', 'year': 2023, 'storage': [128, 256, 512, 1024], 'ram': 8, 'pro': True},
        }
        
        info = model_info.get(request.model_id, {
            'name': 'Unknown',
            'year': 2020,
            'storage': [128],
            'ram': 4,
            'pro': False
        })
        
        return prediction_pb2.ModelInfoResponse(
            model_name=info['name'],
            release_year=info['year'],
            available_storage=info['storage'],
            ram_gb=info['ram'],
            is_pro=info['pro']
        )
    
    def HealthCheck(self, request, context):
        """Servis sağlık durumu"""
        uptime = datetime.now() - self.start_time
        
        model_loaded = self.predictor.model is not None
        status = 'healthy' if model_loaded else 'unhealthy'
        
        logger.info(f"Health check: {status}")
        
        return prediction_pb2.HealthCheckResponse(
            status=status,
            version='1.0.0',
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
    logger.info(f"Max Workers: {GRPC_CONFIG['max_workers']}")
    logger.info("Servis: PricePrediction")
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

