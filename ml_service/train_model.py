"""
iPhone Fiyat Tahmini - ML Model Eğitimi
Random Forest Regressor kullanarak model eğitir
"""

import pandas as pd
import numpy as np
from sklearn.ensemble import RandomForestRegressor
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import StandardScaler
from sklearn.metrics import mean_absolute_error, mean_squared_error, r2_score
import psycopg2
import joblib
import logging
from config import DB_CONFIG, MODEL_CONFIG, CONDITION_SCORES, MODEL_PATH, SCALER_PATH

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


class ModelTrainer:
    """ML Model eğitim sınıfı"""
    
    def __init__(self):
        self.model = None
        self.scaler = StandardScaler()
        self.feature_names = None
        
    def fetch_training_data(self) -> pd.DataFrame:
        """Veritabanından eğitim verisi çek"""
        logger.info("Veritabanından veri çekiliyor...")
        
        query = """
        SELECT 
            b.id as brand_id,
            b.name as brand_name,
            m.id as model_id,
            m.name as model_name,
            m.release_year,
            s.id as spec_id,
            s.ram_gb,
            s.storage_gb,
            l.condition,
            l.price,
            l.source
        FROM listings l
        JOIN specs s ON l.spec_id = s.id
        JOIN models m ON s.model_id = m.id
        JOIN brands b ON m.brand_id = b.id
        WHERE l.is_active = TRUE
            AND l.price IS NOT NULL
            AND l.price BETWEEN 5000 AND 100000
        ORDER BY l.scraped_at DESC
        """
        
        try:
            conn = psycopg2.connect(**DB_CONFIG)
            df = pd.read_sql(query, conn)
            conn.close()
            
            logger.info(f"Toplam {len(df)} kayıt çekildi")
            return df
            
        except Exception as e:
            logger.error(f"Veri çekme hatası: {e}")
            raise
    
    def prepare_features(self, df: pd.DataFrame) -> tuple:
        """Feature engineering ve hazırlık"""
        logger.info("Feature engineering yapılıyor...")
        
        # Kozmetik durum skorunu ekle
        df['condition_score'] = df['condition'].map(CONDITION_SCORES)
        
        # Model yaşını hesapla
        current_year = 2024
        df['model_age'] = current_year - df['release_year']
        
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
        df['is_pro'] = df['model_name'].str.contains('Pro', case=False).astype(int)
        
        # Pro Max modeller
        df['is_pro_max'] = df['model_name'].str.contains('Pro Max', case=False).astype(int)
        
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
            'release_year'
        ]
        
        X = df[feature_columns]
        y = df['price']
        
        self.feature_names = feature_columns
        
        logger.info(f"Features: {feature_columns}")
        logger.info(f"Veri boyutu: X={X.shape}, y={y.shape}")
        
        return X, y
    
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
            # 1. Veri çek
            df = self.fetch_training_data()
            
            if len(df) < 20:
                logger.warning(f"Yetersiz veri! ({len(df)} kayıt). En az 20 kayıt gerekli.")
                logger.info("Önce scraper'ı çalıştırın: python scraper/run_scraper.py")
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
            
        except Exception as e:
            logger.error(f"✗ Eğitim hatası: {e}", exc_info=True)
            raise


def main():
    """Ana fonksiyon"""
    logger.info("="*60)
    logger.info("iPhone Fiyat Tahmini - Model Eğitimi")
    logger.info("="*60 + "\n")
    
    trainer = ModelTrainer()
    trainer.run()


if __name__ == "__main__":
    main()

