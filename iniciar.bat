@echo off
cd /d "%~dp0"
set PORT=3001
set NODE_ENV=production
echo Iniciando FarmaControl...
echo.
echo Acesse: http://localhost:3001
echo Para encerrar, feche esta janela.
echo.
node server.js
pause
