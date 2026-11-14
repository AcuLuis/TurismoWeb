@echo off
setlocal EnableExtensions
chcp 65001 >nul
title TurismoGo - Reiniciar datos de demostración
cd /d "%~dp0"

echo.
echo  Esto BORRA la base de datos local de TurismoGo y la vuelve a crear con los datos
echo  de demostración (empresas, viajeros, salidas, reservas y reseñas ficticias).
echo  Cualquier cambio que hayas hecho en la base local se perderá.
echo.
choice /C SN /M "¿Deseas continuar"
if errorlevel 2 (
  echo Operación cancelada.
  goto :fin
)

rem Contraseña de SQL Server (de .env si existe; si no, la de desarrollo por defecto)
set "SA_PASSWORD=TurismoGo_Dev_2026!"
if exist ".env" (
  for /f "usebackq tokens=1,* delims==" %%a in (".env") do (
    if /i "%%a"=="MSSQL_SA_PASSWORD" set "SA_PASSWORD=%%b"
  )
)

docker info >nul 2>&1
if errorlevel 1 (
  echo [ERROR] Docker no está en ejecución. Abre Docker Desktop o ejecuta iniciar.bat primero.
  goto :fin
)

rem La API no debe estar usando la base de datos mientras se elimina
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\detener-servicios.ps1"

docker compose up -d >nul 2>&1
echo [..] Esperando a la base de datos...
set /a intentos=0
:esperar_bd
set "ESTADO_BD="
for /f "delims=" %%s in ('docker inspect -f "{{.State.Health.Status}}" turismogo-sqlserver 2^>nul') do set "ESTADO_BD=%%s"
if "%ESTADO_BD%"=="healthy" goto :bd_lista
set /a intentos+=1
if %intentos% geq 60 (
  echo [ERROR] La base de datos no respondió.
  goto :fin
)
ping -n 4 127.0.0.1 >nul
goto :esperar_bd
:bd_lista

docker exec turismogo-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "%SA_PASSWORD%" -C -b -Q "IF DB_ID('TurismoGo') IS NOT NULL BEGIN ALTER DATABASE TurismoGo SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE TurismoGo; END" >nul
if errorlevel 1 (
  echo [ERROR] No se pudo eliminar la base de datos.
  goto :fin
)
echo [OK] Base de datos eliminada.
echo.
echo Ejecuta iniciar.bat: al arrancar, la API recreará la base y cargará los datos de demostración.

:fin
echo.
pause
