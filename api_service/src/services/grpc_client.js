/**
 * gRPC Client - Python ML servisine bağlantı
 */

const grpc = require('@grpc/grpc-js');
const protoLoader = require('@grpc/proto-loader');
const path = require('path');
const config = require('../config');

// Proto dosyasını yükle
const PROTO_PATH = path.join(__dirname, '../../..', 'ml_service/proto/prediction.proto');

const packageDefinition = protoLoader.loadSync(PROTO_PATH, {
    keepCase: true,
    longs: String,
    enums: String,
    defaults: true,
    oneofs: true
});

const protoDescriptor = grpc.loadPackageDefinition(packageDefinition);
const pricePredictionProto = protoDescriptor.iphone_price_prediction;

// gRPC client oluştur
const grpcAddress = `${config.grpc.host}:${config.grpc.port}`;
const client = new pricePredictionProto.PricePrediction(
    grpcAddress,
    grpc.credentials.createInsecure()
);

console.log(`gRPC Client oluşturuldu: ${grpcAddress}`);

/**
 * Fiyat tahmini yap
 */
function predictPrice(phoneSpec) {
    return new Promise((resolve, reject) => {
        const request = {
            model_id: phoneSpec.model_id,
            ram_gb: phoneSpec.ram_gb,
            storage_gb: phoneSpec.storage_gb,
            condition: phoneSpec.condition,
            release_year: phoneSpec.release_year || 2020
        };
        
        console.log('gRPC isteği gönderiliyor:', request);
        
        client.PredictPrice(request, (error, response) => {
            if (error) {
                console.error('gRPC hatası:', error);
                reject(error);
            } else {
                console.log('gRPC yanıtı alındı:', response.predicted_price);
                resolve({
                    predicted_price: response.predicted_price,
                    confidence_score: response.confidence_score,
                    price_range: {
                        min: response.price_range.min_price,
                        max: response.price_range.max_price
                    },
                    status: response.status,
                    message: response.message
                });
            }
        });
    });
}

/**
 * Model bilgisi getir
 */
function getModelInfo(modelId) {
    return new Promise((resolve, reject) => {
        client.GetModelInfo({ model_id: modelId }, (error, response) => {
            if (error) {
                reject(error);
            } else {
                resolve({
                    model_name: response.model_name,
                    release_year: response.release_year,
                    available_storage: response.available_storage,
                    ram_gb: response.ram_gb,
                    is_pro: response.is_pro
                });
            }
        });
    });
}

/**
 * Health check
 */
function healthCheck() {
    return new Promise((resolve, reject) => {
        client.HealthCheck({ service: 'api' }, (error, response) => {
            if (error) {
                reject(error);
            } else {
                resolve({
                    status: response.status,
                    version: response.version,
                    model_loaded: response.model_loaded,
                    uptime: response.uptime
                });
            }
        });
    });
}

module.exports = {
    predictPrice,
    getModelInfo,
    healthCheck
};

