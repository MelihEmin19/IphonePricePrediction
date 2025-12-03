"""
Tahmin yapma modülü - Eğitilmiş modeli kullanarak fiyat tahmini yapar
"""

import joblib
import numpy as np
import logging
from typing import Dict
from config import MODEL_PATH, SCALER_PATH, CONDITION_SCORES

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class PricePredictor:
    """Fiyat tahmin sınıfı"""
    
    def __init__(self):
        self.model = None
        self.scaler = None
        self.load_model()
    
    def load_model(self):
        """Kaydedilmiş modeli yükle"""
        try:
            self.model = joblib.load(MODEL_PATH)
            self.scaler = joblib.load(SCALER_PATH)
            logger.info("Model başarıyla yüklendi")
        except FileNotFoundError:
            logger.error(f"Model dosyası bulunamadı: {MODEL_PATH}")
            logger.info("Önce modeli eğitin: python train_model.py")
            raise
        except Exception as e:
            logger.error(f"Model yükleme hatası: {e}")
            raise
    
    def predict(self, input_data: Dict) -> Dict:
        """
        Fiyat tahmini yap
        
        Args:
            input_data: {
                'model_id': int,
                'ram_gb': int,
                'storage_gb': int,
                'condition': str,
                'release_year': int
            }
        
        Returns:
            {
                'predicted_price': float,
                'confidence_score': float,
                'price_range': {'min': float, 'max': float}
            }
        """
        try:
            # Feature'ları hazırla
            features = self._prepare_features(input_data)
            
            # Scale et
            features_scaled = self.scaler.transform([features])
            
            # Tahmin yap
            predicted_price = self.model.predict(features_scaled)[0]
            
            # Confidence score hesapla (estimator'ların std'sine göre)
            predictions = []
            for estimator in self.model.estimators_:
                pred = estimator.predict(features_scaled)[0]
                predictions.append(pred)
            
            std = np.std(predictions)
            confidence = max(0, min(100, 100 - (std / predicted_price * 100)))
            
            # Fiyat aralığı
            price_range = {
                'min': max(5000, predicted_price - std * 1.5),
                'max': min(100000, predicted_price + std * 1.5)
            }
            
            result = {
                'predicted_price': round(predicted_price, 2),
                'confidence_score': round(confidence, 2),
                'price_range': {
                    'min': round(price_range['min'], 2),
                    'max': round(price_range['max'], 2)
                }
            }
            
            logger.info(f"Tahmin: {result['predicted_price']} TL (Güven: %{result['confidence_score']})")
            
            return result
            
        except Exception as e:
            logger.error(f"Tahmin hatası: {e}")
            raise
    
    def _prepare_features(self, input_data: Dict) -> list:
        """Input'tan feature vektörü oluştur"""
        # Condition score
        condition_score = CONDITION_SCORES.get(input_data['condition'], 0.85)
        
        # Model age
        current_year = 2024
        model_age = current_year - input_data.get('release_year', 2020)
        
        # Storage category
        storage_gb = input_data['storage_gb']
        if storage_gb <= 64:
            storage_category = 1
        elif storage_gb <= 128:
            storage_category = 2
        elif storage_gb <= 256:
            storage_category = 3
        elif storage_gb <= 512:
            storage_category = 4
        else:
            storage_category = 5
        
        # RAM category
        ram_gb = input_data['ram_gb']
        if ram_gb <= 4:
            ram_category = 1
        elif ram_gb <= 6:
            ram_category = 2
        else:
            ram_category = 3
        
        # Pro model flags (model_id'den tahmin ediyoruz)
        model_id = input_data['model_id']
        # Model ID'lerine göre (schema'daki sıralamaya göre)
        pro_models = [2, 3, 6, 7, 10, 11, 14, 15, 18, 19]
        pro_max_models = [3, 7, 11, 15, 19]
        
        is_pro = 1 if model_id in pro_models else 0
        is_pro_max = 1 if model_id in pro_max_models else 0
        
        # Feature vektörü (train_model.py'deki sırayla aynı olmalı)
        features = [
            input_data['model_id'],
            ram_gb,
            storage_gb,
            condition_score,
            model_age,
            storage_category,
            ram_category,
            is_pro,
            is_pro_max,
            input_data.get('release_year', 2020)
        ]
        
        return features


def main():
    """Test fonksiyonu"""
    predictor = PricePredictor()
    
    # Örnek tahmin
    test_input = {
        'model_id': 8,  # iPhone 13
        'ram_gb': 4,
        'storage_gb': 128,
        'condition': 'Mükemmel',
        'release_year': 2021
    }
    
    result = predictor.predict(test_input)
    
    print("\n" + "="*60)
    print("TAHMİN SONUCU")
    print("="*60)
    print(f"Model: iPhone 13, 128GB, Mükemmel")
    print(f"Tahmini Fiyat: {result['predicted_price']:,.2f} TL")
    print(f"Güven Skoru: %{result['confidence_score']:.2f}")
    print(f"Fiyat Aralığı: {result['price_range']['min']:,.2f} - {result['price_range']['max']:,.2f} TL")
    print("="*60)


if __name__ == "__main__":
    main()

