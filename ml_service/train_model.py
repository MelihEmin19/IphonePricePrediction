"""
iPhone Fiyat Tahmini - ML Model Eğitimi
CSV dosyasından veri okuyarak Random Forest Regressor eğitir
"""

import pandas as pd
import numpy as np
from sklearn.ensemble import RandomForestRegressor
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import StandardScaler
from sklearn.metrics import mean_absolute_error, mean_squared_error, r2_score
import joblib
import logging
import os
from config import MODEL_CONFIG, CONDITION_SCORES, MODEL_PATH, SCALER_PATH

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# CSV dosya yolu
CSV_PATH = os.path.join(os.path.dirname(__file__), '..', 'data', 'dataset.csv')


class ModelTrainer:
    """ML Model eğitim sınıfı"""
    
    def __init__(self):
        self.model = None
        self.scaler = StandardScaler()
        self.feature_names = None
        
    def fetch_training_data(self) -> pd.DataFrame:
        """CSV dosyasından eğitim verisi oku"""
        logger.info(f"CSV dosyasından veri okunuyor: {CSV_PATH}")
        
        try:
            df = pd.read_csv(CSV_PATH)
            logger.info(f"Toplam {len(df)} kayıt okundu")
            logger.info(f"Kolonlar: {list(df.columns)}")
            
            # Veri kaynakları (sitelerden çekilmiş)
            if 'source' in df.columns:
                sources = df['source'].value_counts()
                logger.info(f"\nVeri Kaynakları:")
                for source, count in sources.items():
                    logger.info(f"  {source}: {count} ilan")
            
            return df
            
        except FileNotFoundError:
            logger.error(f"CSV dosyası bulunamadı: {CSV_PATH}")
            raise
        except Exception as e:
            logger.error(f"Veri okuma hatası: {e}")
            raise
    
    def prepare_features(self, df: pd.DataFrame) -> tuple:
        """Feature engineering ve hazırlık"""
        logger.info("Feature engineering yapılıyor...")
        
        # Condition sayısal değere dönüştür (CSV'de zaten sayısal olabilir)
        if df['condition'].dtype == 'object':
            df['condition_score'] = df['condition'].map(CONDITION_SCORES)
        else:
            # Eğer condition zaten sayısal ise (1-5 arası)
            condition_numeric_map = {
                5: 1.0,    # Mükemmel
                4: 0.93,   # Çok İyi
                3: 0.85,   # İyi
                2: 0.75,   # Orta
                1: 0.60    # Kötü
            }
            df['condition_score'] = df['condition'].map(condition_numeric_map)
        
        # Eksik değerleri doldur
        df['condition_score'] = df['condition_score'].fillna(0.85)
        
        # Model yaşını hesapla
        current_year = 2024
        if 'cikis_yili' in df.columns:
            df['model_age'] = current_year - df['cikis_yili']
        elif 'release_year' in df.columns:
            df['model_age'] = current_year - df['release_year']
        else:
            # Model adından yıl çıkar
            df['model_age'] = df['model'].apply(self._extract_year_from_model)
        
        # Hafıza kategorileri
        df['storage_category'] = pd.cut(
            df['storage_gb'],
            bins=[0, 64, 128, 256, 512, 2000],
            labels=[1, 2, 3, 4, 5]
        ).astype(int)
        
        # RAM kategorileri
        df['ram_category'] = pd.cut(
            df['ram_gb'],
            bins=[0, 4, 6, 10],
            labels=[1, 2, 3]
        ).astype(int)
        
        # Pro modeller
        df['is_pro'] = df['model'].str.contains('Pro', case=False).astype(int)
        
        # Pro Max modeller
        df['is_pro_max'] = df['model'].str.contains('Pro Max', case=False).astype(int)
        
        # Model ID oluştur (model adından)
        model_ids = {name: idx for idx, name in enumerate(df['model'].unique(), 1)}
        df['model_id'] = df['model'].map(model_ids)
        
        # Ana kamera MP (varsa)
        if 'ana_kamera_mp' in df.columns:
            df['camera_mp'] = df['ana_kamera_mp']
        else:
            df['camera_mp'] = df['model'].apply(self._get_camera_mp)
        
        # Release year
        if 'cikis_yili' in df.columns:
            df['release_year'] = df['cikis_yili']
        elif 'release_year' not in df.columns:
            df['release_year'] = current_year - df['model_age']
        
        # Feature seçimi
        feature_columns = [
            'model_id',
            'ram_gb',
            'storage_gb',
            'condition_score',
            'model_age',
            'storage_category',
            'ram_category',
            'is_pro',
            'is_pro_max',
            'release_year',
            'camera_mp'
        ]
        
        # Eksik kolonları çıkar
        available_features = [col for col in feature_columns if col in df.columns]
        
        X = df[available_features]
        y = df['price']
        
        self.feature_names = available_features
        
        logger.info(f"Features: {available_features}")
        logger.info(f"Veri boyutu: X={X.shape}, y={y.shape}")
        
        return X, y
    
    def _extract_year_from_model(self, model_name: str) -> int:
        """Model adından yaş hesapla"""
        model_years = {
            'iPhone 11': 2019,
            'iPhone 11 Pro': 2019,
            'iPhone 11 Pro Max': 2019,
            'iPhone 12': 2020,
            'iPhone 12 Mini': 2020,
            'iPhone 12 Pro': 2020,
            'iPhone 12 Pro Max': 2020,
            'iPhone 13': 2021,
            'iPhone 13 Mini': 2021,
            'iPhone 13 Pro': 2021,
            'iPhone 13 Pro Max': 2021,
            'iPhone 14': 2022,
            'iPhone 14 Plus': 2022,
            'iPhone 14 Pro': 2022,
            'iPhone 14 Pro Max': 2022,
            'iPhone 15': 2023,
            'iPhone 15 Plus': 2023,
            'iPhone 15 Pro': 2023,
            'iPhone 15 Pro Max': 2023,
        }
        year = model_years.get(model_name, 2021)
        return 2024 - year
    
    def _get_camera_mp(self, model_name: str) -> int:
        """Model adından kamera MP döndür"""
        camera_mp = {
            'iPhone 11': 12,
            'iPhone 11 Pro': 12,
            'iPhone 11 Pro Max': 12,
            'iPhone 12': 12,
            'iPhone 12 Mini': 12,
            'iPhone 12 Pro': 12,
            'iPhone 12 Pro Max': 12,
            'iPhone 13': 12,
            'iPhone 13 Mini': 12,
            'iPhone 13 Pro': 12,
            'iPhone 13 Pro Max': 12,
            'iPhone 14': 12,
            'iPhone 14 Plus': 12,
            'iPhone 14 Pro': 48,
            'iPhone 14 Pro Max': 48,
            'iPhone 15': 48,
            'iPhone 15 Plus': 48,
            'iPhone 15 Pro': 48,
            'iPhone 15 Pro Max': 48,
        }
        return camera_mp.get(model_name, 12)
    
    def train(self, X, y):
        """Model eğitimi"""
        logger.info("Model eğitimi başlıyor...")
        
        # Train-test split
        X_train, X_test, y_train, y_test = train_test_split(
            X, y,
            test_size=MODEL_CONFIG['test_size'],
            random_state=MODEL_CONFIG['random_state']
        )
        
        logger.info(f"Train boyutu: {len(X_train)}, Test boyutu: {len(X_test)}")
        
        # Feature scaling
        X_train_scaled = self.scaler.fit_transform(X_train)
        X_test_scaled = self.scaler.transform(X_test)
        
        # Model oluştur
        self.model = RandomForestRegressor(
            n_estimators=MODEL_CONFIG['n_estimators'],
            max_depth=MODEL_CONFIG['max_depth'],
            min_samples_split=MODEL_CONFIG['min_samples_split'],
            min_samples_leaf=MODEL_CONFIG['min_samples_leaf'],
            random_state=MODEL_CONFIG['random_state'],
            n_jobs=-1,
            verbose=1
        )
        
        # Eğit
        logger.info("RandomForest eğitiliyor...")
        self.model.fit(X_train_scaled, y_train)
        
        # Tahmin et
        y_pred_train = self.model.predict(X_train_scaled)
        y_pred_test = self.model.predict(X_test_scaled)
        
        # Metrikleri hesapla
        self._calculate_metrics(y_train, y_pred_train, y_test, y_pred_test)
        
        # Feature importance
        self._show_feature_importance()
        
        return self.model, self.scaler
    
    def _calculate_metrics(self, y_train, y_pred_train, y_test, y_pred_test):
        """Model performans metrikleri"""
        logger.info("\n" + "="*60)
        logger.info("MODEL PERFORMANSI")
        logger.info("="*60)
        
        # Train metrikleri
        train_mae = mean_absolute_error(y_train, y_pred_train)
        train_rmse = np.sqrt(mean_squared_error(y_train, y_pred_train))
        train_r2 = r2_score(y_train, y_pred_train)
        
        logger.info("\nTrain Metrikleri:")
        logger.info(f"  MAE (Ortalama Mutlak Hata): {train_mae:,.2f} TL")
        logger.info(f"  RMSE (Kök Ortalama Kare Hata): {train_rmse:,.2f} TL")
        logger.info(f"  R² Score: {train_r2:.4f} ({train_r2*100:.2f}%)")
        
        # Test metrikleri
        test_mae = mean_absolute_error(y_test, y_pred_test)
        test_rmse = np.sqrt(mean_squared_error(y_test, y_pred_test))
        test_r2 = r2_score(y_test, y_pred_test)
        
        logger.info("\nTest Metrikleri:")
        logger.info(f"  MAE (Ortalama Mutlak Hata): {test_mae:,.2f} TL")
        logger.info(f"  RMSE (Kök Ortalama Kare Hata): {test_rmse:,.2f} TL")
        logger.info(f"  R² Score: {test_r2:.4f} ({test_r2*100:.2f}%)")
        
        # Overfit kontrolü
        overfit_score = train_r2 - test_r2
        logger.info(f"\nOverfit Kontrolü:")
        logger.info(f"  Train-Test R² Farkı: {overfit_score:.4f}")
        if overfit_score > 0.1:
            logger.warning("  ⚠ Model overfit olabilir!")
        else:
            logger.info("  ✓ Model dengeli görünüyor")
        
        logger.info("="*60 + "\n")
    
    def _show_feature_importance(self):
        """Feature importance göster"""
        if self.model and self.feature_names:
            importances = self.model.feature_importances_
            feature_imp = pd.DataFrame({
                'feature': self.feature_names,
                'importance': importances
            }).sort_values('importance', ascending=False)
            
            logger.info("\nÖzellik Önem Sıralaması:")
            for idx, row in feature_imp.iterrows():
                logger.info(f"  {row['feature']:<20}: {row['importance']:.4f} ({row['importance']*100:.2f}%)")
    
    def save_model(self):
        """Modeli kaydet"""
        logger.info(f"\nModel kaydediliyor: {MODEL_PATH}")
        joblib.dump(self.model, MODEL_PATH)
        
        logger.info(f"Scaler kaydediliyor: {SCALER_PATH}")
        joblib.dump(self.scaler, SCALER_PATH)
        
        logger.info("✓ Model ve scaler başarıyla kaydedildi")
    
    def run(self):
        """Ana eğitim pipeline'ı"""
        try:
            # 1. CSV'den veri oku
            df = self.fetch_training_data()
            
            if len(df) < 20:
                logger.warning(f"Yetersiz veri! ({len(df)} kayıt). En az 20 kayıt gerekli.")
                return
            
            if len(df) < 100:
                logger.warning(f"Az veri! ({len(df)} kayıt). 100+ kayıt önerilir ama devam ediliyor...")
            
            # 2. Feature hazırla
            X, y = self.prepare_features(df)
            
            # 3. Model eğit
            self.train(X, y)
            
            # 4. Kaydet
            self.save_model()
            
            logger.info("\n✓ Model eğitimi başarıyla tamamlandı!")
            logger.info(f"✓ Veri kaynağı: {CSV_PATH}")
            
        except Exception as e:
            logger.error(f"✗ Eğitim hatası: {e}", exc_info=True)
            raise


def main():
    """Ana fonksiyon"""
    logger.info("="*60)
    logger.info("iPhone Fiyat Tahmini - Model Eğitimi (CSV'den)")
    logger.info("="*60 + "\n")
    
    trainer = ModelTrainer()
    trainer.run()


if __name__ == "__main__":
    main()
