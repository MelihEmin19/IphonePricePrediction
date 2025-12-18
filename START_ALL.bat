@echo off
echo ========================================
echo iPhone Fiyat Tahmin Sistemi
echo Tum Servisleri Baslat
echo ========================================
echo.

echo [1/3] Python gRPC Sunucu baslatiliyor...
start "Python gRPC Server" cmd /k "cd ml_service && python grpc_server.py"
timeout /t 3 /nobreak > nul

echo [2/3] Node.js API baslatiliyor...
start "Node.js API" cmd /k "cd api_service && npm start"
timeout /t 3 /nobreak > nul

echo [3/3] ASP.NET Web Uygulamasi baslatiliyor...
start "ASP.NET Web" cmd /k "cd web_app\IphonePriceWeb && dotnet run"

echo.
echo ========================================
echo Tum servisler baslatildi!
echo ========================================
echo.
echo Python gRPC: localhost:50051
echo Node.js API: http://localhost:3000
echo ASP.NET Web: http://localhost:5164
echo.
echo Tarayicinizi acin: http://localhost:5164
echo.
pause
