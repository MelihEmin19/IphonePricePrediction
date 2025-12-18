/**
 * iPhone Fiyat Tahmin API - Node.js Express Server
 * Middleware: Database + gRPC (ML) + SOAP (Döviz)
 */

const express = require('express');
const cors = require('cors');
const bodyParser = require('body-parser');
const morgan = require('morgan');
const helmet = require('helmet');
const rateLimit = require('express-rate-limit');
const config = require('./src/config');
const apiRoutes = require('./src/routes/api');

const app = express();

// Middleware
app.use(helmet()); // Security headers
app.use(cors()); // CORS
app.use(bodyParser.json());
app.use(bodyParser.urlencoded({ extended: true }));
app.use(bodyParser.text({ type: 'text/xml' })); // SOAP için
app.use(morgan('dev')); // Logging

// Rate limiting
const limiter = rateLimit({
    windowMs: 15 * 60 * 1000, // 15 dakika
    max: 100, // max 100 request
    message: 'Çok fazla istek gönderildi, lütfen daha sonra tekrar deneyin'
});
app.use('/api/', limiter);

// Ana sayfa
app.get('/', (req, res) => {
    res.json({
        name: 'iPhone Fiyat Tahmin API',
        version: '1.0.0',
        description: 'SOA mimarisi ile iPhone fiyat tahmin servisi',
        endpoints: {
            health: 'GET /api/health',
            brands: 'GET /api/brands',
            models: 'GET /api/models',
            specs: 'GET /api/specs/:modelId',
            predict: 'POST /api/predict',
            dashboard: 'GET /api/stats/dashboard',
            soap: 'POST /soap/convert'
        },
        technologies: ['Node.js', 'Express', 'PostgreSQL', 'gRPC', 'SOAP']
    });
});

// API routes
app.use('/api', apiRoutes);

// 404 handler
app.use((req, res) => {
    res.status(404).json({
        success: false,
        error: 'Endpoint bulunamadı',
        path: req.path
    });
});

// Error handler
app.use((err, req, res, next) => {
    console.error('Sunucu hatası:', err);
    res.status(500).json({
        success: false,
        error: config.server.env === 'development' ? err.message : 'Sunucu hatası'
    });
});

// Sunucuyu başlat
const PORT = config.server.port;
app.listen(PORT, () => {
    console.log('='.repeat(60));
    console.log('NODE.JS API SUNUCUSU BAŞLATILDI');
    console.log('='.repeat(60));
    console.log(`Port: ${PORT}`);
    console.log(`Environment: ${config.server.env}`);
    console.log(`Database: ${config.database.host}:${config.database.port}`);
    console.log(`gRPC: ${config.grpc.host}:${config.grpc.port}`);
    console.log('='.repeat(60));
    console.log('\nEndpoint\'ler:');
    console.log('  GET  /                      - API bilgisi');
    console.log('  GET  /api/health            - Health check');
    console.log('  GET  /api/brands            - Markalar');
    console.log('  GET  /api/models            - Modeller');
    console.log('  GET  /api/specs/:id         - Model özellikleri');
    console.log('  POST /api/predict           - Fiyat tahmini');
    console.log('  GET  /api/stats/dashboard   - İstatistikler');
    console.log('  POST /soap/convert          - Döviz (SOAP)');
    console.log('='.repeat(60));
    console.log('\nSunucu hazır! (Durdurmak için Ctrl+C)\n');
});

module.exports = app;

