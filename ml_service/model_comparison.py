"""
🤖 iPhone Fiyat Tahmini - Model Karşılaştırması
İster: En iyi model seçilmeli (20 puan)

Bu script, farklı ML modellerini karşılaştırır ve en iyi modeli seçer.
Çalıştırma: python model_comparison.py
"""

import pandas as pd
import numpy as np
from sklearn.model_selection import train_test_split, cross_val_score
from sklearn.preprocessing import StandardScaler
from sklearn.metrics import mean_absolute_error, mean_squared_error, r2_score
import joblib
import time
import os
import warnings
warnings.filterwarnings('ignore')

# Modeller
from sklearn.ensemble import RandomForestRegressor, GradientBoostingRegressor, AdaBoostRegressor
from sklearn.linear_model import LinearRegression, Ridge, Lasso, ElasticNet
from sklearn.tree import DecisionTreeRegressor
from sklearn.svm import SVR
from sklearn.neighbors import KNeighborsRegressor

from config import CONDITION_SCORES

# CSV dosya yolu
CSV_PATH = os.path.join(os.path.dirname(__file__), '..', 'data', 'dataset.csv')


def load_data():
    """CSV dosyasından veri yükle"""
    print(f'📂 CSV okunuyor: {CSV_PATH}')
    
    df = pd.read_csv(CSV_PATH)
    
    # Veri kaynakları göster
    if 'source' in df.columns:
        sources = df['source'].value_counts()
        print(f'\n📊 Veri Kaynakları (Sitelerden çekilmiş):')
        for source, count in sources.items():
            print(f'   {source}: {count} ilan')
    
    return df


def prepare_features(df):
    """Feature engineering"""
    # Condition score
    if df['condition'].dtype == 'object':
        df['condition_score'] = df['condition'].map(CONDITION_SCORES)
    else:
        # Sayısal condition (1-5)
        condition_numeric_map = {
            5: 1.0,    # Mükemmel
            4: 0.93,   # Çok İyi
            3: 0.85,   # İyi
            2: 0.75,   # Orta
            1: 0.60    # Kötü
        }
        df['condition_score'] = df['condition'].map(condition_numeric_map)
    
    df['condition_score'] = df['condition_score'].fillna(0.85)
    
    # Model yaşı
    current_year = 2024
    if 'cikis_yili' in df.columns:
        df['model_age'] = current_year - df['cikis_yili']
        df['release_year'] = df['cikis_yili']
    elif 'release_year' in df.columns:
        df['model_age'] = current_year - df['release_year']
    else:
        df['model_age'] = 3  # Default
        df['release_year'] = 2021
    
    # Storage kategorisi
    df['storage_category'] = pd.cut(
        df['storage_gb'],
        bins=[0, 64, 128, 256, 512, 2000],
        labels=[1, 2, 3, 4, 5]
    ).astype(int)
    
    # RAM kategorisi
    df['ram_category'] = pd.cut(
        df['ram_gb'],
        bins=[0, 4, 6, 10],
        labels=[1, 2, 3]
    ).astype(int)
    
    # Pro model flag
    df['is_pro'] = df['model'].str.contains('Pro', case=False).astype(int)
    df['is_pro_max'] = df['model'].str.contains('Pro Max', case=False).astype(int)
    
    # Model ID oluştur
    model_ids = {name: idx for idx, name in enumerate(df['model'].unique(), 1)}
    df['model_id'] = df['model'].map(model_ids)
    
    # Feature columns
    feature_cols = [
        'model_id', 'ram_gb', 'storage_gb', 'condition_score',
        'model_age', 'storage_category', 'ram_category',
        'is_pro', 'is_pro_max', 'release_year'
    ]
    
    X = df[feature_cols]
    y = df['price']
    
    return X, y


def get_models():
    """Karşılaştırılacak modelleri döndür"""
    models = {
        # Ensemble Methods
        'Random Forest': RandomForestRegressor(
            n_estimators=100, max_depth=15, random_state=42, n_jobs=-1
        ),
        'Gradient Boosting': GradientBoostingRegressor(
            n_estimators=100, max_depth=5, learning_rate=0.1, random_state=42
        ),
        'AdaBoost': AdaBoostRegressor(
            n_estimators=100, learning_rate=0.1, random_state=42
        ),
        
        # Linear Models
        'Linear Regression': LinearRegression(),
        'Ridge Regression': Ridge(alpha=1.0),
        'Lasso Regression': Lasso(alpha=1.0),
        'ElasticNet': ElasticNet(alpha=1.0, l1_ratio=0.5),
        
        # Tree-based
        'Decision Tree': DecisionTreeRegressor(max_depth=15, random_state=42),
        
        # Instance-based
        'KNN (k=5)': KNeighborsRegressor(n_neighbors=5),
        'KNN (k=10)': KNeighborsRegressor(n_neighbors=10),
    }
    
    return models


def evaluate_model(model, X_train, X_test, y_train, y_test, name):
    """Modeli değerlendir"""
    # Eğit
    start_time = time.time()
    model.fit(X_train, y_train)
    train_time = time.time() - start_time
    
    # Tahmin
    y_pred_train = model.predict(X_train)
    y_pred_test = model.predict(X_test)
    
    # Metrikler
    results = {
        'Model': name,
        'Train_R2': r2_score(y_train, y_pred_train),
        'Test_R2': r2_score(y_test, y_pred_test),
        'Train_MAE': mean_absolute_error(y_train, y_pred_train),
        'Test_MAE': mean_absolute_error(y_test, y_pred_test),
        'Train_RMSE': np.sqrt(mean_squared_error(y_train, y_pred_train)),
        'Test_RMSE': np.sqrt(mean_squared_error(y_test, y_pred_test)),
        'Overfit': r2_score(y_train, y_pred_train) - r2_score(y_test, y_pred_test),
        'Train_Time': train_time
    }
    
    return results, model


def print_results_table(results_df):
    """Sonuçları tablo olarak yazdır"""
    print('\n' + '='*100)
    print('MODEL KARŞILAŞTIRMA SONUÇLARI')
    print('='*100)
    
    # En iyi modeli belirle
    best_model = results_df.loc[results_df['Test_R2'].idxmax(), 'Model']
    
    print(f'\n{"Model":<20} {"Test R²":>10} {"Test MAE":>12} {"Test RMSE":>12} {"Overfit":>10} {"Süre (s)":>10}')
    print('-'*80)
    
    for _, row in results_df.iterrows():
        is_best = '⭐' if row['Model'] == best_model else '  '
        print(f'{is_best}{row["Model"]:<18} {row["Test_R2"]:>10.4f} {row["Test_MAE"]:>12,.0f} {row["Test_RMSE"]:>12,.0f} {row["Overfit"]:>10.4f} {row["Train_Time"]:>10.3f}')
    
    print('-'*80)
    print(f'\n🏆 EN İYİ MODEL: {best_model}')
    
    best_row = results_df[results_df['Model'] == best_model].iloc[0]
    print(f'   Test R² Score: {best_row["Test_R2"]:.4f} ({best_row["Test_R2"]*100:.2f}%)')
    print(f'   Test MAE: {best_row["Test_MAE"]:,.0f} TL')
    print(f'   Test RMSE: {best_row["Test_RMSE"]:,.0f} TL')


def cross_validation_analysis(X, y, best_model_name, models):
    """Cross-validation analizi"""
    print('\n' + '='*60)
    print('CROSS-VALIDATION ANALİZİ (5-Fold)')
    print('='*60)
    
    scaler = StandardScaler()
    X_scaled = scaler.fit_transform(X)
    
    # En iyi 3 model için CV
    best_3 = ['Random Forest', 'Gradient Boosting', best_model_name]
    best_3 = list(set(best_3))[:3]
    
    print(f'\n{"Model":<25} {"CV Mean R²":>12} {"CV Std":>10}')
    print('-'*50)
    
    for name in best_3:
        if name in models:
            cv_scores = cross_val_score(models[name], X_scaled, y, cv=5, scoring='r2')
            print(f'{name:<25} {cv_scores.mean():>12.4f} {cv_scores.std():>10.4f}')


def save_best_model(model, scaler, model_name):
    """En iyi modeli kaydet"""
    print('\n' + '='*60)
    print('MODEL KAYDETME')
    print('='*60)
    
    joblib.dump(model, 'model.pkl')
    joblib.dump(scaler, 'scaler.pkl')
    
    print(f'\n✅ En iyi model kaydedildi: model.pkl')
    print(f'✅ Scaler kaydedildi: scaler.pkl')
    print(f'📊 Seçilen Model: {model_name}')


def main():
    """Ana fonksiyon"""
    print('\n' + '🤖'*30)
    print('  iPhone Fiyat Tahmini - Model Karşılaştırması')
    print('  (CSV dosyasından veri okuyarak)')
    print('🤖'*30)
    
    # Veri yükle
    print('\n⏳ Veri yükleniyor...')
    df = load_data()
    print(f'✅ {len(df):,} kayıt yüklendi.')
    
    # Feature hazırla
    print('⏳ Feature engineering...')
    X, y = prepare_features(df)
    print(f'✅ Features: {X.shape[1]}, Samples: {X.shape[0]}')
    
    # Train-test split
    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=0.2, random_state=42
    )
    
    # Scaling
    scaler = StandardScaler()
    X_train_scaled = scaler.fit_transform(X_train)
    X_test_scaled = scaler.transform(X_test)
    
    print(f'✅ Train: {len(X_train)}, Test: {len(X_test)}')
    
    # Modelleri al
    models = get_models()
    print(f'\n📊 {len(models)} model karşılaştırılacak...\n')
    
    # Her modeli değerlendir
    all_results = []
    best_model = None
    best_score = -np.inf
    best_model_name = ''
    
    for name, model in models.items():
        print(f'  ⏳ Eğitiliyor: {name}...', end=' ')
        
        try:
            # Bazı modeller scaling gerektirir
            if name in ['Linear Regression', 'Ridge Regression', 'Lasso Regression', 
                        'ElasticNet', 'KNN (k=5)', 'KNN (k=10)', 'SVR']:
                results, trained_model = evaluate_model(
                    model, X_train_scaled, X_test_scaled, y_train, y_test, name
                )
            else:
                results, trained_model = evaluate_model(
                    model, X_train, X_test, y_train, y_test, name
                )
            
            all_results.append(results)
            
            if results['Test_R2'] > best_score:
                best_score = results['Test_R2']
                best_model = trained_model
                best_model_name = name
            
            print(f'✅ R²: {results["Test_R2"]:.4f}')
            
        except Exception as e:
            print(f'❌ Hata: {e}')
    
    # Sonuçları DataFrame'e çevir
    results_df = pd.DataFrame(all_results)
    results_df = results_df.sort_values('Test_R2', ascending=False)
    
    # Sonuçları yazdır
    print_results_table(results_df)
    
    # Cross-validation
    cross_validation_analysis(X, y, best_model_name, models)
    
    # En iyi modeli kaydet
    save_best_model(best_model, scaler, best_model_name)
    
    # Sonuçları CSV'ye kaydet
    results_df.to_csv('model_comparison_results.csv', index=False)
    print(f'\n📁 Sonuçlar kaydedildi: model_comparison_results.csv')
    
    print('\n' + '✅'*30)
    print('  Model Karşılaştırması Tamamlandı!')
    print('✅'*30 + '\n')
    
    return results_df, best_model_name


if __name__ == '__main__':
    results, best = main()
