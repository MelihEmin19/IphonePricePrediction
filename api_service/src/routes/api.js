/**
 * API Routes - Ana endpoint'ler
 * CSV tabanlı veri okuma
 */

const express = require('express');
const router = express.Router();
const fs = require('fs');
const path = require('path');
const grpcClient = require('../services/grpc_client');
const soapService = require('../services/soap_service');

// CSV dosyasını oku ve parse et
const CSV_PATH = path.join(__dirname, '..', '..', '..', 'data', 'dataset.csv');

// Seri numarasından model adı mapping
const SERI_NO_TO_MODEL = {
    8: { name: 'Apple iPhone 8', year: 2017 },
    9: { name: 'Apple iPhone SE 2020', year: 2020 },
    10: { name: 'Apple iPhone X', year: 2017 },
    11: { name: 'Apple iPhone 11', year: 2019 },
    12: { name: 'Apple iPhone 12', year: 2020 },
    13: { name: 'Apple iPhone 13', year: 2021 },
    14: { name: 'Apple iPhone 14', year: 2022 },
    15: { name: 'Apple iPhone 15', year: 2023 },
    16: { name: 'Apple iPhone 16', year: 2024 }
};

function loadCSVData() {
    const csvContent = fs.readFileSync(CSV_PATH, 'utf-8');
    const lines = csvContent.trim().split('\n');
    const headers = lines[0].split(',');
    
    const data = [];
    for (let i = 1; i < lines.length; i++) {
        const values = lines[i].split(',');
        const row = {};
        headers.forEach((h, idx) => row[h.trim()] = values[idx]?.trim());
        
        // seri_no'dan model adı ekle
        const seriNo = parseInt(row.seri_no);
        if (SERI_NO_TO_MODEL[seriNo]) {
            row.cihaz_isim = SERI_NO_TO_MODEL[seriNo].name;
        }
        
        data.push(row);
    }
    return data;
}

// Unique modelleri çıkar (yeni CSV formatına göre)
function getUniqueModels() {
    const data = loadCSVData();
    const modelMap = new Map();
    
    // seri_no bazlı unique modeller
    data.forEach(row => {
        const seriNo = parseInt(row.seri_no);
        const modelInfo = SERI_NO_TO_MODEL[seriNo];
        
        if (modelInfo && !modelMap.has(seriNo)) {
            modelMap.set(seriNo, {
                id: seriNo,
                name: modelInfo.name,
                release_year: modelInfo.year,
                brand_name: 'Apple',
                brand_id: 1
            });
        }
    });
    
    return Array.from(modelMap.values()).sort((a, b) => a.id - b.id);
}

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
        
        // CSV kontrolü
        const csvExists = fs.existsSync(CSV_PATH);
        
        res.json({
            status: 'healthy',
            timestamp: new Date().toISOString(),
            services: {
                api: 'ok',
                data_source: csvExists ? 'ok' : 'error',
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
 * Tüm markaları getir (sadece Apple)
 */
router.get('/brands', async (req, res) => {
    try {
        res.json({
            success: true,
            data: [{ id: 1, name: 'Apple' }]
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
 * Tüm modelleri CSV'den getir
 */
router.get('/models', async (req, res) => {
    try {
        const models = getUniqueModels();
        
        res.json({
            success: true,
            data: models
        });
    } catch (error) {
        console.error('Models hatası:', error);
        res.status(500).json({
            success: false,
            error: error.message
        });
    }
});

/**
 * GET /api/specs/:modelId
 * Model özelliklerini CSV'den getir
 */
router.get('/specs/:modelId', async (req, res) => {
    try {
        const { modelId } = req.params;
        const models = getUniqueModels();
        const model = models.find(m => m.id === parseInt(modelId));
        
        if (!model) {
            return res.status(404).json({ success: false, error: 'Model bulunamadı' });
        }
        
        const data = loadCSVData();
        const modelData = data.filter(row => row.cihaz_isim === model.name);
        
        // Unique RAM ve Storage değerlerini çıkar
        const ramValues = [...new Set(modelData.map(d => parseInt(d.ram_gb)))].filter(v => !isNaN(v)).sort((a, b) => a - b);
        const storageValues = [...new Set(modelData.map(d => parseInt(d.storage_gb)))].filter(v => !isNaN(v)).sort((a, b) => a - b);
        
        res.json({
            success: true,
            data: {
                ram_options: ramValues,
                storage_options: storageValues
            }
        });
    } catch (error) {
        console.error('Specs hatası:', error);
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
        
        // Model adından seri numarası bul
        let targetSeriNo = null;
        for (const [seriNo, info] of Object.entries(SERI_NO_TO_MODEL)) {
            if (info.name.includes(modelName) || modelName.includes(info.name)) {
                targetSeriNo = parseInt(seriNo);
                break;
            }
        }
        
        // Model adından numara çıkar (örn: "iPhone 13" -> 13)
        if (!targetSeriNo) {
            const match = modelName.match(/iPhone\s*(\d+)/i);
            if (match) {
                targetSeriNo = parseInt(match[1]);
            }
        }
        
        const data = loadCSVData();
        
        // seri_no'ya göre filtrele
        const modelData = data.filter(row => parseInt(row.seri_no) === targetSeriNo);
        
        if (modelData.length === 0) {
            return res.status(404).json({
                success: false,
                error: 'Model bulunamadı'
            });
        }
        
        // Segment isimlerini tanımla
        const segmentNames = { 0: 'Base', 1: 'Mini', 2: 'Base', 3: 'Plus', 4: 'Pro', 5: 'Pro Max' };
        
        // İstatistikleri hesapla
        const prices = modelData.map(d => parseFloat(d.cihaz_fiyat)).filter(p => !isNaN(p));
        const avgPrice = prices.reduce((a, b) => a + b, 0) / prices.length;
        const minPrice = Math.min(...prices);
        const maxPrice = Math.max(...prices);
        
        // RAM ve Storage değerlerini topla
        const ramValues = [...new Set(modelData.map(d => parseInt(d.ram_gb)))].filter(v => !isNaN(v)).sort((a, b) => a - b);
        const storageValues = [...new Set(modelData.map(d => parseInt(d.storage_gb)))].filter(v => !isNaN(v)).sort((a, b) => a - b);
        
        // İlk satırdan diğer bilgileri al
        const firstRow = modelData[0];
        const segment = parseInt(firstRow.segment) || 0;
        const modelInfo = SERI_NO_TO_MODEL[targetSeriNo] || { name: modelName, year: 2020 };
        
        res.json({
            success: true,
            data: {
                model_name: modelInfo.name,
                ram_gb: ramValues,
                storage_gb: storageValues,
                camera_mp: parseInt(firstRow.kamera_mp) || 12,
                segment: segmentNames[segment] || 'Base',
                release_year: modelInfo.year,
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
        const { model_id, ram_gb, storage_gb, condition, release_year } = req.body;
        
        // Validasyon
        if (!model_id || !ram_gb || !storage_gb || !condition) {
            return res.status(400).json({
                success: false,
                error: 'Eksik parametreler: model_id, ram_gb, storage_gb, condition gerekli'
            });
        }
        
        // Model adını bul
        const models = getUniqueModels();
        const model = models.find(m => m.id === parseInt(model_id));
        const modelName = model ? model.name : 'Apple iPhone 13';
        
        let prediction;
        
        try {
            // gRPC ile tahmin yap
            prediction = await grpcClient.predictPrice({
                model_id: parseInt(model_id),
                model_name: modelName,
                ram_gb: parseInt(ram_gb),
                storage_gb: parseInt(storage_gb),
                condition,
                release_year: release_year || (model ? model.release_year : 2020)
            });
        } catch (grpcError) {
            console.log('gRPC unavailable, using fallback calculation');
            prediction = calculateFallbackPrice(model_id, ram_gb, storage_gb, condition);
        }
        
        // USD karşılığını hesapla (SOAP servisi)
        const usdConversion = await soapService.convertTLtoUSD(prediction.predicted_price);
        
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
                    model_name: modelName,
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
 * Dashboard istatistikleri - CSV'den hesapla
 */
router.get('/stats/dashboard', async (req, res) => {
    try {
        const data = loadCSVData();
        const prices = data.map(d => parseFloat(d.cihaz_fiyat)).filter(p => !isNaN(p));
        const models = getUniqueModels();
        
        const avgPrice = prices.reduce((a, b) => a + b, 0) / prices.length;
        const minPrice = Math.min(...prices);
        const maxPrice = Math.max(...prices);
        
        res.json({
            success: true,
            data: {
                overall: {
                    total_listings: data.length,
                    total_models: models.length,
                    avg_price: Math.round(avgPrice),
                    min_price: minPrice,
                    max_price: maxPrice
                },
                brands: [{ name: 'Apple', avg_price: Math.round(avgPrice), count: data.length }],
                data_source: 'CSV'
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

