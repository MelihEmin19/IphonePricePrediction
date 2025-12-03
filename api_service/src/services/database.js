/**
 * Database Service - PostgreSQL bağlantı ve sorgu yönetimi
 */

const { Pool } = require('pg');
const config = require('../config');

// Connection pool
const pool = new Pool(config.database);

// Bağlantı testi
pool.on('connect', () => {
    console.log('✓ PostgreSQL bağlantısı kuruldu');
});

pool.on('error', (err) => {
    console.error('✗ PostgreSQL hatası:', err);
});

/**
 * Tüm markaları getir
 */
async function getBrands() {
    const result = await pool.query('SELECT * FROM brands ORDER BY name');
    return result.rows;
}

/**
 * Markaya ait modelleri getir
 */
async function getModelsByBrand(brandId) {
    const result = await pool.query(
        'SELECT * FROM models WHERE brand_id = $1 ORDER BY release_year DESC, name',
        [brandId]
    );
    return result.rows;
}

/**
 * Tüm modelleri getir
 */
async function getAllModels() {
    const result = await pool.query(`
        SELECT 
            m.id,
            m.name,
            m.release_year,
            b.name as brand_name,
            b.id as brand_id
        FROM models m
        JOIN brands b ON m.brand_id = b.id
        ORDER BY m.release_year DESC, m.name
    `);
    return result.rows;
}

/**
 * Modele ait özellikleri (specs) getir
 */
async function getSpecsByModel(modelId) {
    const result = await pool.query(
        'SELECT * FROM sp_GetModelSpecs($1)',
        [modelId]
    );
    return result.rows;
}

/**
 * Spec detaylarını getir
 */
async function getSpecDetail(specId) {
    const result = await pool.query(`
        SELECT 
            s.*,
            m.name as model_name,
            m.release_year,
            b.name as brand_name
        FROM specs s
        JOIN models m ON s.model_id = m.id
        JOIN brands b ON m.brand_id = b.id
        WHERE s.id = $1
    `, [specId]);
    
    return result.rows[0];
}

/**
 * Kullanıcı doğrulama
 */
async function authenticateUser(username, passwordHash) {
    const result = await pool.query(
        'SELECT * FROM users WHERE username = $1 AND password_hash = $2',
        [username, passwordHash]
    );
    return result.rows[0];
}

/**
 * Tahmin kaydı ekle
 */
async function recordPrediction(userId, specId, condition, predictedPrice, confidenceScore) {
    const result = await pool.query(
        'SELECT sp_RecordPrediction($1, $2, $3, $4, $5)',
        [userId, specId, condition, predictedPrice, confidenceScore]
    );
    return result.rows[0];
}

/**
 * Kullanıcı tahmin geçmişi
 */
async function getUserPredictionHistory(userId, limit = 10) {
    const result = await pool.query(
        'SELECT * FROM sp_GetUserPredictionHistory($1, $2)',
        [userId, limit]
    );
    return result.rows;
}

/**
 * Dashboard istatistikleri
 */
async function getDashboardStats() {
    const result = await pool.query('SELECT * FROM vw_DashboardStats');
    return result.rows[0];
}

/**
 * Marka ortalama fiyatları
 */
async function getBrandAveragePrices() {
    const result = await pool.query('SELECT * FROM vw_BrandAveragePrices');
    return result.rows;
}

/**
 * Scraper istatistikleri
 */
async function getScraperStats() {
    const result = await pool.query('SELECT * FROM sp_GetScraperStats()');
    return result.rows[0];
}

module.exports = {
    pool,
    getBrands,
    getModelsByBrand,
    getAllModels,
    getSpecsByModel,
    getSpecDetail,
    authenticateUser,
    recordPrediction,
    getUserPredictionHistory,
    getDashboardStats,
    getBrandAveragePrices,
    getScraperStats
};

