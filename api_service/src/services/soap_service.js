/**
 * SOAP Service - Döviz kuru servisi (TL -> USD dönüşümü)
 * PDF isteri: SOAP protokolü kullanımı
 */

const axios = require('axios');
const config = require('../config');

// Son alınan kuru ve zamanını cache'le (5 dakika geçerli)
let cachedRate = null;
let cacheTime = null;
const CACHE_DURATION = 5 * 60 * 1000; // 5 dakika

/**
 * Güncel USD/TRY kurunu çek (birden fazla kaynak dener)
 */
async function getExchangeRate() {
    // Cache kontrolü
    if (cachedRate && cacheTime && (Date.now() - cacheTime < CACHE_DURATION)) {
        console.log(`Cache'den USD/TRY kuru: ${cachedRate}`);
        return cachedRate;
    }

    // Kaynak 1: ExchangeRate-API (ücretsiz, güvenilir)
    try {
        const response = await axios.get('https://api.exchangerate-api.com/v4/latest/USD', {
            timeout: 10000
        });
        
        if (response.data && response.data.rates && response.data.rates.TRY) {
            const rate = response.data.rates.TRY;
            cachedRate = rate;
            cacheTime = Date.now();
            console.log(`ExchangeRate-API USD/TRY kuru: ${rate}`);
            return rate;
        }
    } catch (error) {
        console.warn('ExchangeRate-API başarısız:', error.message);
    }

    // Kaynak 2: Frankfurter API (backup)
    try {
        const response = await axios.get('https://api.frankfurter.app/latest?from=USD&to=TRY', {
            timeout: 10000
        });
        
        if (response.data && response.data.rates && response.data.rates.TRY) {
            const rate = response.data.rates.TRY;
            cachedRate = rate;
            cacheTime = Date.now();
            console.log(`Frankfurter API USD/TRY kuru: ${rate}`);
            return rate;
        }
    } catch (error) {
        console.warn('Frankfurter API başarısız:', error.message);
    }

    // Kaynak 3: TCMB (eski kaynak)
    try {
        const response = await axios.get(config.soap.exchangeRateApi, {
            timeout: 10000
        });
        
        const usdMatch = response.data.match(/<Currency Kod="USD"[\s\S]*?<ForexSelling>([\d.,]+)<\/ForexSelling>/);
        
        if (usdMatch && usdMatch[1]) {
            const rate = parseFloat(usdMatch[1].replace(',', '.'));
            cachedRate = rate;
            cacheTime = Date.now();
            console.log(`TCMB USD/TRY kuru: ${rate}`);
            return rate;
        }
    } catch (error) {
        console.warn('TCMB servisi başarısız:', error.message);
    }

    // Tüm kaynaklar başarısız olduysa cache'den veya varsayılan değer
    if (cachedRate) {
        console.log(`Eski cache kullanılıyor: ${cachedRate}`);
        return cachedRate;
    }

    console.warn('Tüm döviz API kaynakları başarısız, varsayılan kur kullanılıyor');
    return 34.50; // Aralık 2024 yaklaşık kuru
}

/**
 * TL'yi USD'ye çevir
 */
async function convertTLtoUSD(amountTL) {
    try {
        const rate = await getExchangeRate();
        const amountUSD = amountTL / rate;
        
        return {
            amount_tl: amountTL,
            amount_usd: Math.round(amountUSD * 100) / 100,
            exchange_rate: rate,
            timestamp: new Date().toISOString()
        };
    } catch (error) {
        console.error('Döviz dönüşüm hatası:', error);
        throw error;
    }
}

/**
 * SOAP XML response oluştur (PDF isteri için)
 */
function createSoapResponse(amountTL, amountUSD, rate) {
    return `<?xml version="1.0" encoding="UTF-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
    <soap:Body>
        <ConvertCurrencyResponse xmlns="http://iphone-price-prediction.com/soap">
            <Result>
                <AmountTL>${amountTL}</AmountTL>
                <AmountUSD>${amountUSD}</AmountUSD>
                <ExchangeRate>${rate}</ExchangeRate>
                <Timestamp>${new Date().toISOString()}</Timestamp>
                <Status>success</Status>
            </Result>
        </ConvertCurrencyResponse>
    </soap:Body>
</soap:Envelope>`;
}

/**
 * SOAP servis endpoint'i (Express middleware)
 */
async function soapEndpoint(req, res) {
    try {
        // SOAP request parse et (basitleştirilmiş)
        const amountTL = parseFloat(req.body.amount_tl || req.query.amount_tl || 0);
        
        if (amountTL <= 0) {
            throw new Error('Geçersiz miktar');
        }
        
        const result = await convertTLtoUSD(amountTL);
        
        // SOAP XML response döndür
        const soapXml = createSoapResponse(
            result.amount_tl,
            result.amount_usd,
            result.exchange_rate
        );
        
        res.set('Content-Type', 'text/xml; charset=utf-8');
        res.send(soapXml);
        
    } catch (error) {
        const errorXml = `<?xml version="1.0" encoding="UTF-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
    <soap:Body>
        <soap:Fault>
            <faultcode>soap:Server</faultcode>
            <faultstring>${error.message}</faultstring>
        </soap:Fault>
    </soap:Body>
</soap:Envelope>`;
        
        res.status(500).set('Content-Type', 'text/xml; charset=utf-8').send(errorXml);
    }
}

module.exports = {
    getExchangeRate,
    convertTLtoUSD,
    soapEndpoint,
    createSoapResponse
};

