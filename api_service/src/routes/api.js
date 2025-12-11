/**
 * API Routes - Ana endpoint'ler
 */

const express = require('express');
const router = express.Router();
const grpcClient = require('../services/grpc_client');
const soapService = require('../services/soap_service');
const db = require('../services/database');

/**
 * GET /api/health
 * Health check
 */
router.get('/health', async (req, res) => {
    try {
        let grpcStatus = 'unavailable';
        let mlModelStatus = 'fallback_mode';
        
        try {
            const grpcHealth = await grpcClient.healthCheck();
            grpcStatus = grpcHealth.status === 'healthy' ? 'ok' : 'error';
            mlModelStatus = grpcHealth.model_loaded ? 'loaded' : 'not_loaded';
        } catch (e) {
            console.log('gRPC not available, using fallback mode');
        }
        
        const dbHealth = await db.pool.query('SELECT NOW()');
        
        res.json({
            status: 'healthy',
            timestamp: new Date().toISOString(),
            services: {
                api: 'ok',
                database: dbHealth.rows ? 'ok' : 'error',
                grpc: grpcStatus,
                ml_model: mlModelStatus
            }
        });
    } catch (error) {
        res.status(500).json({
            status: 'unhealthy',
            error: error.message
        });
    }
});

/**
 * GET /api/brands
 * Tüm markaları getir
 */
router.get('/brands', async (req, res) => {
    try {
        const brands = await db.getBrands();
        res.json({
            success: true,
            data: brands
        });
    } catch (error) {
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

/**
 * GET /api/models
 * Tüm modelleri veya markaya ait modelleri getir
 */
router.get('/models', async (req, res) => {
    try {
        const { brand_id } = req.query;
        
        let models;
        if (brand_id) {
            models = await db.getModelsByBrand(parseInt(brand_id));
        } else {
            models = await db.getAllModels();
        }
        
        res.json({
            success: true,
            data: models
        });
    } catch (error) {
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

/**
 * GET /api/specs/:modelId
 * Model özelliklerini getir
 */
router.get('/specs/:modelId', async (req, res) => {
    try {
        const { modelId } = req.params;
        const specs = await db.getSpecsByModel(parseInt(modelId));
        
        res.json({
            success: true,
            data: specs
        });
    } catch (error) {
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

/**
 * GET /api/model-stats/:modelName
 * Model istatistiklerini CSV'den hesapla (karşılaştırma için)
 */
router.get('/model-stats/:modelName', async (req, res) => {
    try {
        const { modelName } = req.params;
        const fs = require('fs');
        const path = require('path');
        
        // CSV dosyasını oku
        const csvPath = path.join(__dirname, '..', '..', '..', 'data', 'dataset.csv');
        const csvContent = fs.readFileSync(csvPath, 'utf-8');
        const lines = csvContent.trim().split('\n');
        const headers = lines[0].split(',');
        
        // Segment puanını isme çevir
        const segmentNames = { 1: 'Mini', 2: 'Base', 3: 'Plus', 4: 'Pro', 5: 'Pro Max' };
        
        // İlgili modelin verilerini filtrele (cihaz_isim kolonunda ara)
        const modelData = [];
        for (let i = 1; i < lines.length; i++) {
            const values = lines[i].split(',');
            const row = {};
            headers.forEach((h, idx) => row[h] = values[idx]);
            
            // Apple iPhone XX formatında ara
            if (row.cihaz_isim && row.cihaz_isim.includes(modelName)) {
                modelData.push(row);
            }
        }
        
        if (modelData.length === 0) {
            return res.status(404).json({
                success: false,
                error: 'Model bulunamadı'
            });
        }
        
        // İstatistikleri hesapla (cihaz_fiyat kolonunu kullan)
        const prices = modelData.map(d => parseFloat(d.cihaz_fiyat));
        const avgPrice = prices.reduce((a, b) => a + b, 0) / prices.length;
        const minPrice = Math.min(...prices);
        const maxPrice = Math.max(...prices);
        
        // RAM ve Storage değerlerini topla
        const ramValues = [...new Set(modelData.map(d => parseInt(d.ram_gb)))].filter(v => !isNaN(v)).sort((a, b) => a - b);
        const storageValues = [...new Set(modelData.map(d => parseInt(d.storage_gb)))].filter(v => !isNaN(v)).sort((a, b) => a - b);
        
        // İlk satırdan diğer bilgileri al
        const firstRow = modelData[0];
        const segmentPuan = parseInt(firstRow.segment_puan) || 2;
        
        res.json({
            success: true,
            data: {
                model_name: modelName,
                ram_gb: ramValues,
                storage_gb: storageValues,
                camera_mp: parseInt(firstRow.kamera_mp) || 12,
                segment: segmentNames[segmentPuan] || 'Base',
                release_year: parseInt(firstRow.cikis_yili) || 2020,
                avg_price: Math.round(avgPrice * 100) / 100,
                min_price: Math.round(minPrice * 100) / 100,
                max_price: Math.round(maxPrice * 100) / 100,
                listing_count: modelData.length
            }
        });
    } catch (error) {
        console.error('Model stats hatası:', error);
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

/**
 * POST /api/predict
 * Fiyat tahmini yap (Ana endpoint)
 */
router.post('/predict', async (req, res) => {
    try {
        const { model_id, ram_gb, storage_gb, condition, release_year, user_id } = req.body;
        
        // Validasyon
        if (!model_id || !ram_gb || !storage_gb || !condition) {
            return res.status(400).json({
                success: false,
                error: 'Eksik parametreler: model_id, ram_gb, storage_gb, condition gerekli'
            });
        }
        
        let prediction;
        
        try {
            // gRPC ile tahmin yap
            prediction = await grpcClient.predictPrice({
                model_id,
                ram_gb,
                storage_gb,
                condition,
                release_year: release_year || 2020
            });
        } catch (grpcError) {
            console.log('gRPC unavailable, using fallback calculation');
            // Fallback: Basit fiyat hesaplaması (gRPC çalışmıyorsa)
            prediction = calculateFallbackPrice(model_id, ram_gb, storage_gb, condition);
        }
        
        // USD karşılığını hesapla (SOAP servisi)
        const usdConversion = await soapService.convertTLtoUSD(prediction.predicted_price);
        
        // Veritabanına kaydet (opsiyonel user_id varsa)
        if (user_id) {
            // Spec ID'yi bul
            const specDetail = await db.pool.query(`
                SELECT s.id FROM specs s
                JOIN models m ON s.model_id = m.id
                WHERE m.id = $1 AND s.ram_gb = $2 AND s.storage_gb = $3
                LIMIT 1
            `, [model_id, ram_gb, storage_gb]);
            
            if (specDetail.rows.length > 0) {
                await db.recordPrediction(
                    user_id,
                    specDetail.rows[0].id,
                    condition,
                    prediction.predicted_price,
                    prediction.confidence_score
                );
            }
        }
        
        res.json({
            success: true,
            data: {
                prediction: {
                    price_tl: prediction.predicted_price,
                    price_usd: usdConversion.amount_usd,
                    confidence: prediction.confidence_score,
                    range: prediction.price_range
                },
                exchange_rate: usdConversion.exchange_rate,
                input: {
                    model_id,
                    ram_gb,
                    storage_gb,
                    condition
                },
                timestamp: new Date().toISOString()
            }
        });
        
    } catch (error) {
        console.error('Tahmin hatası:', error);
        res.status(500).json({
            success: false,
            error: error.message || 'Tahmin yapılırken hata oluştu'
        });
    }
});

/**
 * Fallback fiyat hesaplama (gRPC olmadan)
 */
function calculateFallbackPrice(model_id, ram_gb, storage_gb, condition) {
    // Model bazlı baz fiyatlar (TL)
    const modelPrices = {
        1: 12000, 2: 15000, 3: 17000,  // iPhone 11 serisi
        4: 16000, 5: 14000, 6: 20000, 7: 22000,  // iPhone 12 serisi
        8: 20000, 9: 18000, 10: 26000, 11: 28000,  // iPhone 13 serisi
        12: 28000, 13: 30000, 14: 38000, 15: 42000,  // iPhone 14 serisi
        16: 42000, 17: 45000, 18: 55000, 19: 62000   // iPhone 15 serisi
    };
    
    // Hafıza çarpanları
    const storageMultiplier = {
        64: 1.0, 128: 1.15, 256: 1.35, 512: 1.60, 1024: 1.90
    };
    
    // Durum çarpanları
    const conditionMultiplier = {
        'Mükemmel': 1.0, 'Çok İyi': 0.93, 'İyi': 0.85, 'Orta': 0.75
    };
    
    const basePrice = modelPrices[model_id] || 20000;
    const storageMult = storageMultiplier[storage_gb] || 1.0;
    const conditionMult = conditionMultiplier[condition] || 0.85;
    
    const predictedPrice = Math.round(basePrice * storageMult * conditionMult);
    const variance = predictedPrice * 0.1;
    
    return {
        predicted_price: predictedPrice,
        confidence_score: 75.0,
        price_range: {
            min: Math.round(predictedPrice - variance),
            max: Math.round(predictedPrice + variance)
        }
    };
}

/**
 * GET /api/stats/dashboard
 * Dashboard istatistikleri
 */
router.get('/stats/dashboard', async (req, res) => {
    try {
        const stats = await db.getDashboardStats();
        const brandPrices = await db.getBrandAveragePrices();
        const scraperStats = await db.getScraperStats();
        
        res.json({
            success: true,
            data: {
                overall: stats,
                brands: brandPrices,
                scraper: scraperStats
            }
        });
    } catch (error) {
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});


/**
 * POST /soap/convert
 * SOAP döviz dönüşüm endpoint'i
 */
router.post('/soap/convert', soapService.soapEndpoint);
router.get('/soap/convert', soapService.soapEndpoint);

module.exports = router;

