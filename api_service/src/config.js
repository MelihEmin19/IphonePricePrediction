/**
 * API Konfigürasyonu
 */

require('dotenv').config();

module.exports = {
    // Server ayarları
    server: {
        port: process.env.PORT || 3000,
        env: process.env.NODE_ENV || 'development'
    },
    
    // Database ayarları
    database: {
        host: process.env.DB_HOST || 'localhost',
        port: parseInt(process.env.DB_PORT) || 5432,
        database: process.env.DB_NAME || 'iphone_price_db',
        user: process.env.DB_USER || 'postgres',
        password: process.env.DB_PASSWORD || 'postgres123'
    },
    
    // gRPC ayarları
    grpc: {
        host: process.env.GRPC_HOST || 'localhost',
        port: parseInt(process.env.GRPC_PORT) || 50051,
        protoPath: '../ml_service/proto/prediction.proto'
    },
    
    // SOAP ayarları
    soap: {
        exchangeRateApi: process.env.EXCHANGE_RATE_API || 'https://www.tcmb.gov.tr/kurlar/today.xml'
    }
};

