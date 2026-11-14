@echo off
setlocal EnableExtensions
chcp 65001 >nul
title TurismoGo - Detener
cd /d "%~dp0"

echo.
echo Deteniendo TurismoGo...

rem API y sitio web (cierra también las ventanas abiertas por iniciar.bat)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\detener-servicios.ps1"

rem Base de datos: detiene el contenedor de SQL Server (los datos se conservan)
docker info >nul 2>&1
if not errorlevel 1 (
  docker compose stop >nul 2>&1
  echo [OK] Base de datos detenida ^(los datos se conservan^)
)

echo.
echo Listo.
ping -n 4 127.0.0.1 >nul
