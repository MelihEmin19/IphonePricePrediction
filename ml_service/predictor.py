"""
Tahmin yapma modülü - Eğitilmiş modeli kullanarak fiyat tahmini yapar
"""

import joblib
import numpy as np
import pandas as pd
import logging
from typing import Dict
from config import MODEL_PATH, SCALER_PATH, CONFIG_PATH, CONDITION_SCORES, IPHONE_MODELS

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class PricePredictor:
    """Fiyat tahmin sınıfı"""
    
    def __init__(self):
        self.model = None
        self.scaler = None
        self.config = None
        self.load_model()
    
    def load_model(self):
        """Kaydedilmiş modeli yükle"""
        try:
            self.model = joblib.load(MODEL_PATH)
            self.scaler = joblib.load(SCALER_PATH)
            self.config = joblib.load(CONFIG_PATH)
            logger.info(f"Model başarıyla yüklendi: {self.config['model_name']}")
            logger.info(f"Model R² Score: {self.config['metrics']['r2']:.4f}")
        except FileNotFoundError as e:
            logger.error(f"Model dosyası bulunamadı: {e}")
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
                'model_name': str (örn: 'iPhone 13'),
                'ram_gb': int,
                'storage_gb': int,
                'condition': str (Mükemmel/Çok İyi/İyi/Orta)
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
            features_df = self._prepare_features(input_data)
            
            # Tahmin yap
            if self.config.get('use_scaler', False):
                features_scaled = self.scaler.transform(features_df)
                predicted_price = self.model.predict(features_scaled)[0]
            else:
                predicted_price = self.model.predict(features_df)[0]
            
            # Confidence hesapla (Gradient Boosting için staged_predict kullan)
            confidence = 95.0  # Default yüksek güven
            
            if hasattr(self.model, 'estimators_'):
                # Random Forest veya Gradient Boosting
                predictions = []
                
                if hasattr(self.model, 'staged_predict'):
                    # Gradient Boosting - son birkaç stage'i kullan
                    for pred in list(self.model.staged_predict(features_df))[-10:]:
                        predictions.append(pred[0])
                else:
                    # Random Forest - her ağacın tahminini al
                    for tree in self.model.estimators_[:20]:
                        if self.config.get('use_scaler', False):
                            pred = tree.predict(features_scaled)[0]
                        else:
                            pred = tree.predict(features_df)[0]
                        predictions.append(pred)
                
                if predictions:
                    std = np.std(predictions)
                    confidence = max(70, min(99, 100 - (std / max(predicted_price, 1) * 100)))
            
            # Fiyat aralığı (MAE bazlı)
            mae = self.config['metrics'].get('mae', 1000)
            price_range = {
                'min': max(5000, predicted_price - mae * 2),
                'max': min(150000, predicted_price + mae * 2)
            }
            
            result = {
                'predicted_price': round(predicted_price, 2),
                'confidence_score': round(confidence, 2),
                'price_range': {
                    'min': round(price_range['min'], 2),
                    'max': round(price_range['max'], 2)
                }
            }
            
            logger.info(f"Tahmin: {result['predicted_price']:,.0f} TL (Güven: %{result['confidence_score']:.1f})")
            
            return result
            
        except Exception as e:
            logger.error(f"Tahmin hatası: {e}")
            raise
    
    def _prepare_features(self, input_data: Dict) -> pd.DataFrame:
        """Input'tan feature DataFrame oluştur - Yeni veri seti formatına göre"""
        
        # Model adını normalize et - "Apple " prefix'ini kaldır
        model_name = input_data.get('model_name', 'iPhone 13')
        if model_name.startswith('Apple '):
            model_name = model_name.replace('Apple ', '')
        
        # IPHONE_MODELS'den bilgileri al
        model_info = IPHONE_MODELS.get(model_name, IPHONE_MODELS.get('iPhone 13'))
        
        # Condition skorunu al (0-3 arası)
        condition_str = input_data.get('condition', 'İyi')
        condition_score = CONDITION_SCORES.get(condition_str, 1)
        
        # Yeni veri seti feature sırası:
        # ['segment', 'seri_no', 'ram_gb', 'kamera_mp', 'ekran_boyutu', 
        #  'batarya_mah', 'storage_gb', 'cihaz_durum', 'cikis_yili']
        
        features = {
            'segment': model_info.get('segment', 0),
            'seri_no': model_info.get('seri_no', 13),
            'ram_gb': input_data.get('ram_gb', model_info.get('ram', 4)),
            'kamera_mp': model_info.get('kamera_mp', 12),
            'ekran_boyutu': model_info.get('ekran', 6),
            'batarya_mah': model_info.get('batarya', 3000),
            'storage_gb': input_data.get('storage_gb', 128),
            'cihaz_durum': condition_score,
            'cikis_yili': model_info.get('yil', 2021)
        }
        
        # DataFrame oluştur (config'deki sırayla veya default)
        feature_cols = self.config.get('feature_cols', list(features.keys()))
        df = pd.DataFrame([[features.get(col, 0) for col in feature_cols]], columns=feature_cols)
        
        return df


def main():
    """Test fonksiyonu"""
    predictor = PricePredictor()
    
    # Test tahminleri
    test_cases = [
        {'model_name': 'iPhone 13', 'ram_gb': 4, 'storage_gb': 128, 'condition': 'Mükemmel'},
        {'model_name': 'iPhone 14 Pro', 'ram_gb': 6, 'storage_gb': 256, 'condition': 'Çok İyi'},
        {'model_name': 'iPhone 15 Pro Max', 'ram_gb': 8, 'storage_gb': 512, 'condition': 'Mükemmel'},
        {'model_name': 'iPhone 11', 'ram_gb': 4, 'storage_gb': 64, 'condition': 'İyi'},
    ]
    
    print("\n" + "="*70)
    print("TAHMİN SONUÇLARI")
    print("="*70)
    
    for test in test_cases:
        result = predictor.predict(test)
        print(f"\n{test['model_name']} {test['storage_gb']}GB ({test['condition']})")
        print(f"  Tahmini Fiyat: {result['predicted_price']:,.0f} TL")
        print(f"  Güven: %{result['confidence_score']:.1f}")
        print(f"  Aralık: {result['price_range']['min']:,.0f} - {result['price_range']['max']:,.0f} TL")
    
    print("\n" + "="*70)


if __name__ == "__main__":
    main()
