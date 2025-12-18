"""
iPhone Fiyat Tahmin - ML Model Eğitimi
3 farklı algoritma ile eğitip en iyisini seçer
"""

import pandas as pd
import numpy as np
from sklearn.model_selection import train_test_split, cross_val_score
from sklearn.preprocessing import StandardScaler, LabelEncoder
from sklearn.ensemble import RandomForestRegressor, GradientBoostingRegressor
from sklearn.tree import DecisionTreeRegressor
from sklearn.linear_model import Ridge, LinearRegression
from sklearn.metrics import mean_absolute_error, mean_squared_error, r2_score
import joblib
import os
import warnings
warnings.filterwarnings('ignore')

# Paths
DATA_PATH = os.path.join(os.path.dirname(__file__), '..', 'data', 'dataset.csv')
MODEL_PATH = os.path.join(os.path.dirname(__file__), 'models')

def load_and_prepare_data():
    """Veriyi yükle ve hazırla - Yeni veri seti formatı"""
    print("="*60)
    print("VERİ YÜKLEME")
    print("="*60)
    
    df = pd.read_csv(DATA_PATH)
    print(f"Toplam veri: {len(df)} satır")
    print(f"Kolonlar: {list(df.columns)}")
    
    # Yeni veri seti feature sırası:
    # segment, seri_no, ram_gb, kamera_mp, ekran_boyutu, batarya_mah, storage_gb, cihaz_durum, cikis_yili
    feature_cols = ['segment', 'seri_no', 'ram_gb', 'kamera_mp', 'ekran_boyutu', 
                    'batarya_mah', 'storage_gb', 'cihaz_durum', 'cikis_yili']
    
    # Eksik kolonları kontrol et
    available_cols = [col for col in feature_cols if col in df.columns]
    print(f"Kullanılan özellikler: {available_cols}")
    
    X = df[available_cols].copy()
    y = df['cihaz_fiyat'].copy()
    
    # Eksik değerleri doldur
    X = X.fillna(X.median())
    
    print(f"\nÖzellik matrisi: {X.shape}")
    print(f"Hedef değişken: {y.shape}")
    print(f"Fiyat aralığı: {y.min():,.0f} - {y.max():,.0f} TL")
    
    return X, y, available_cols


def train_and_evaluate():
    """3 farklı algoritma ile eğit ve karşılaştır"""
    X, y, feature_cols = load_and_prepare_data()
    
    # Train-test split
    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=0.2, random_state=42
    )
    
    # Scaler
    scaler = StandardScaler()
    X_train_scaled = scaler.fit_transform(X_train)
    X_test_scaled = scaler.transform(X_test)
    
    print("\n" + "="*60)
    print("MODEL EĞİTİMİ VE KARŞILAŞTIRMA")
    print("="*60)
    
    # 5 farklı algoritma
    models = {
        'Random Forest': RandomForestRegressor(
            n_estimators=200,
            max_depth=15,
            min_samples_split=5,
            min_samples_leaf=2,
            random_state=42,
            n_jobs=-1
        ),
        'Decision Tree': DecisionTreeRegressor(
            max_depth=15,
            min_samples_split=5,
            min_samples_leaf=2,
            random_state=42
        ),
        'Gradient Boosting': GradientBoostingRegressor(
            n_estimators=200,
            max_depth=8,
            learning_rate=0.1,
            min_samples_split=5,
            random_state=42
        ),
        'Linear Regression': LinearRegression(),
        'Ridge Regression': Ridge(
            alpha=1.0,
            random_state=42
        )
    }
    
    results = {}
    
    for name, model in models.items():
        print(f"\n{'-'*40}")
        print(f"[*] {name}")
        print(f"{'-'*40}")
        
        # Linear modeller için scaled veri kullan
        use_scaled = name in ['Ridge Regression', 'Linear Regression']
        if use_scaled:
            model.fit(X_train_scaled, y_train)
            y_pred = model.predict(X_test_scaled)
        else:
            model.fit(X_train, y_train)
            y_pred = model.predict(X_test)
        
        # Metrikler
        mae = mean_absolute_error(y_test, y_pred)
        rmse = np.sqrt(mean_squared_error(y_test, y_pred))
        r2 = r2_score(y_test, y_pred)
        
        # Cross-validation
        if use_scaled:
            cv_scores = cross_val_score(model, X_train_scaled, y_train, cv=5, scoring='r2')
        else:
            cv_scores = cross_val_score(model, X_train, y_train, cv=5, scoring='r2')
        
        results[name] = {
            'model': model,
            'mae': mae,
            'rmse': rmse,
            'r2': r2,
            'cv_mean': cv_scores.mean(),
            'cv_std': cv_scores.std(),
            'use_scaler': use_scaled
        }
        
        print(f"  R² Score:     {r2:.4f}")
        print(f"  MAE:          {mae:,.0f} TL")
        print(f"  RMSE:         {rmse:,.0f} TL")
        print(f"  CV R² Mean:   {cv_scores.mean():.4f} (±{cv_scores.std():.4f})")
    
    # En iyi modeli seç (R² bazında)
    best_name = max(results, key=lambda x: results[x]['r2'])
    best_result = results[best_name]
    
    print("\n" + "="*60)
    print(f"*** EN IYI MODEL: {best_name} ***")
    print("="*60)
    print(f"  R² Score:  {best_result['r2']:.4f}")
    print(f"  MAE:       {best_result['mae']:,.0f} TL")
    print(f"  RMSE:      {best_result['rmse']:,.0f} TL")
    
    # Modeli kaydet
    os.makedirs(MODEL_PATH, exist_ok=True)
    
    model_file = os.path.join(MODEL_PATH, 'price_model.pkl')
    scaler_file = os.path.join(MODEL_PATH, 'scaler.pkl')
    config_file = os.path.join(MODEL_PATH, 'model_config.pkl')
    
    joblib.dump(best_result['model'], model_file)
    joblib.dump(scaler, scaler_file)
    joblib.dump({
        'model_name': best_name,
        'feature_cols': feature_cols,
        'use_scaler': best_result['use_scaler'],
        'metrics': {
            'r2': best_result['r2'],
            'mae': best_result['mae'],
            'rmse': best_result['rmse']
        }
    }, config_file)
    
    print(f"\n[OK] Model kaydedildi: {model_file}")
    print(f"[OK] Scaler kaydedildi: {scaler_file}")
    print(f"[OK] Config kaydedildi: {config_file}")
    
    # Feature importance (Random Forest veya Gradient Boosting için)
    if hasattr(best_result['model'], 'feature_importances_'):
        print("\n[+] Ozellik Onemliligi:")
        importances = best_result['model'].feature_importances_
        for feat, imp in sorted(zip(feature_cols, importances), key=lambda x: x[1], reverse=True):
            print(f"  {feat}: {imp:.4f}")
    
    return best_result


def predict_price(ram_gb, storage_gb, condition, model_name):
    """Fiyat tahmini yap - Yeni veri seti formatına göre"""
    # Model ve config yükle
    model = joblib.load(os.path.join(MODEL_PATH, 'price_model.pkl'))
    scaler = joblib.load(os.path.join(MODEL_PATH, 'scaler.pkl'))
    config = joblib.load(os.path.join(MODEL_PATH, 'model_config.pkl'))
    
    # Model bilgileri - Yeni format (segment, seri_no)
    model_info = {
        'iPhone 8': {'kamera_mp': 12, 'ekran': 4, 'batarya': 1821, 'yil': 2017, 'segment': 0, 'seri_no': 8},
        'iPhone SE 2020': {'kamera_mp': 12, 'ekran': 4, 'batarya': 1821, 'yil': 2020, 'segment': 0, 'seri_no': 9},
        'iPhone X': {'kamera_mp': 12, 'ekran': 5, 'batarya': 2716, 'yil': 2017, 'segment': 0, 'seri_no': 10},
        'iPhone 11': {'kamera_mp': 12, 'ekran': 6, 'batarya': 3110, 'yil': 2019, 'segment': 0, 'seri_no': 11},
        'iPhone 12': {'kamera_mp': 12, 'ekran': 6, 'batarya': 2815, 'yil': 2020, 'segment': 0, 'seri_no': 12},
        'iPhone 13': {'kamera_mp': 12, 'ekran': 6, 'batarya': 3227, 'yil': 2021, 'segment': 0, 'seri_no': 13},
        'iPhone 14': {'kamera_mp': 48, 'ekran': 6, 'batarya': 3279, 'yil': 2022, 'segment': 0, 'seri_no': 14},
        'iPhone 15': {'kamera_mp': 48, 'ekran': 6, 'batarya': 3349, 'yil': 2023, 'segment': 0, 'seri_no': 15},
        'iPhone 16': {'kamera_mp': 48, 'ekran': 6, 'batarya': 3561, 'yil': 2024, 'segment': 0, 'seri_no': 16},
    }
    
    # Model adından bilgi al
    info = model_info.get(model_name, model_info['iPhone 13'])
    
    # Yeni feature sırası: segment, seri_no, ram_gb, kamera_mp, ekran_boyutu, batarya_mah, storage_gb, cihaz_durum, cikis_yili
    features = pd.DataFrame([[
        info['segment'],
        info['seri_no'],
        ram_gb,
        info['kamera_mp'],
        info['ekran'],
        info['batarya'],
        storage_gb,
        condition,
        info['yil']
    ]], columns=config['feature_cols'])
    
    # Tahmin
    if config['use_scaler']:
        features_scaled = scaler.transform(features)
        prediction = model.predict(features_scaled)[0]
    else:
        prediction = model.predict(features)[0]
    
    return round(prediction, 2)


if __name__ == "__main__":
    train_and_evaluate()
