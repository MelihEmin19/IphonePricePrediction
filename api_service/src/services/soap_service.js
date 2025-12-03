/**
 * SOAP Service - Döviz kuru servisi (TL -> USD dönüşümü)
 * PDF isteri: SOAP protokolü kullanımı
 */

const axios = require('axios');
const config = require('../config');

/**
 * TCMB'den güncel USD/TRY kurunu çek
 */
async function getExchangeRate() {
    try {
        // TCMB XML servisi (gerçek)
        const response = await axios.get(config.soap.exchangeRateApi, {
            timeout: 5000
        });
        
        // XML parse et (basit regex ile)
        const usdMatch = response.data.match(/<Currency Kod="USD"[\s\S]*?<ForexSelling>([\d.]+)<\/ForexSelling>/);
        
        if (usdMatch && usdMatch[1]) {
            const rate = parseFloat(usdMatch[1]);
            console.log(`TCMB USD/TRY kuru: ${rate}`);
            return rate;
        }
        
        throw new Error('USD kuru parse edilemedi');
        
    } catch (error) {
        console.warn('TCMB servisi çalışmıyor, mock kur kullanılıyor:', error.message);
        // Fallback: Mock kur
        return 32.50; // 2024 Aralık mock kuru
    }
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

