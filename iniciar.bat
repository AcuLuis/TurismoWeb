@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 65001 >nul
title TurismoGo - Inicio
cd /d "%~dp0"

echo.
echo  ==============================================
echo    TurismoGo - inicio en modo desarrollo
echo  ==============================================
echo.

rem ---------------------------------------------------------------
rem 1. Requisitos: .NET 10, Node.js y Docker
rem ---------------------------------------------------------------
set "DOTNET="
where dotnet >nul 2>&1 && set "DOTNET=dotnet"
if not defined DOTNET if exist "%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe" set "DOTNET=%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe"
if not defined DOTNET if exist "%ProgramFiles%\dotnet\dotnet.exe" set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"
if not defined DOTNET (
  echo [ERROR] No se encontró el SDK de .NET 10. Descárgalo de https://dotnet.microsoft.com/download
  goto :error
)
"%DOTNET%" --list-sdks 2>nul | findstr /b "10." >nul
if errorlevel 1 (
  echo [ERROR] Se necesita el SDK de .NET 10. Descárgalo de https://dotnet.microsoft.com/download
  goto :error
)
echo [OK] .NET 10 SDK

where node >nul 2>&1
if errorlevel 1 (
  echo [ERROR] No se encontró Node.js. Instala la versión 24 LTS desde https://nodejs.org/
  goto :error
)
for /f "delims=" %%v in ('node --version') do set "NODE_VERSION=%%v"
echo [OK] Node.js !NODE_VERSION!

where docker >nul 2>&1
if errorlevel 1 (
  echo [ERROR] No se encontró Docker. Instala Docker Desktop desde https://www.docker.com/products/docker-desktop/
  goto :error
)

rem ---------------------------------------------------------------
rem 2. Docker Desktop en ejecución
rem ---------------------------------------------------------------
docker info >nul 2>&1
if not errorlevel 1 goto :docker_listo
echo [..] Iniciando Docker Desktop...
set "DOCKER_APP="
if exist "%ProgramFiles%\Docker\Docker\Docker Desktop.exe" set "DOCKER_APP=%ProgramFiles%\Docker\Docker\Docker Desktop.exe"
if exist "%LOCALAPPDATA%\Programs\DockerDesktop\Docker Desktop.exe" set "DOCKER_APP=%LOCALAPPDATA%\Programs\DockerDesktop\Docker Desktop.exe"
if not defined DOCKER_APP (
  echo [ERROR] Docker no está en ejecución. Abre Docker Desktop y vuelve a ejecutar este archivo.
  goto :error
)
start "" "!DOCKER_APP!"
set /a intentos=0
:esperar_docker
ping -n 4 127.0.0.1 >nul
docker info >nul 2>&1
if not errorlevel 1 goto :docker_listo
set /a intentos+=1
if !intentos! geq 60 (
  echo [ERROR] Docker no respondió después de 3 minutos.
  goto :error
)
goto :esperar_docker
:docker_listo
echo [OK] Docker en ejecución

rem ---------------------------------------------------------------
rem 3. Base de datos (SQL Server en Docker)
rem ---------------------------------------------------------------
echo [..] Iniciando la base de datos (la primera vez descarga la imagen, puede tardar)...
docker compose up -d
if errorlevel 1 (
  echo [ERROR] No se pudo iniciar el contenedor de SQL Server.
  goto :error
)
set /a intentos=0
:esperar_bd
for /f "delims=" %%s in ('docker inspect -f "{{.State.Health.Status}}" turismogo-sqlserver 2^>nul') do set "ESTADO_BD=%%s"
if "!ESTADO_BD!"=="healthy" goto :bd_lista
set /a intentos+=1
if !intentos! geq 60 (
  echo [ERROR] La base de datos no estuvo lista a tiempo. Revisa con: docker compose logs sqlserver
  goto :error
)
ping -n 4 127.0.0.1 >nul
goto :esperar_bd
:bd_lista
echo [OK] Base de datos lista (localhost:14333)

rem ---------------------------------------------------------------
rem 4. API (.NET) en su propia ventana
rem ---------------------------------------------------------------
curl -s -o nul -m 3 http://localhost:5259/health >nul 2>&1
if not errorlevel 1 (
  echo [OK] La API ya estaba en ejecución
  goto :api_lista
)
echo [..] Iniciando la API ^(la primera vez compila el proyecto^)...
start "TurismoGo - API" /D "%~dp0backend" cmd /k ""%DOTNET%" run --project src\TurismoGo.Api --launch-profile http"
set /a intentos=0
:esperar_api
ping -n 4 127.0.0.1 >nul
curl -s -o nul -m 3 http://localhost:5259/health >nul 2>&1
if not errorlevel 1 goto :api_lista
set /a intentos+=1
if !intentos! geq 80 (
  echo [ERROR] La API no respondió. Revisa los mensajes en la ventana "TurismoGo - API".
  goto :error
)
goto :esperar_api
:api_lista
echo [OK] API lista en http://localhost:5259  ^(documentación: http://localhost:5259/swagger^)

rem ---------------------------------------------------------------
rem 5. Frontend (Quasar) en su propia ventana
rem ---------------------------------------------------------------
if not exist "frontend\node_modules\" (
  echo [..] Instalando dependencias del frontend ^(solo la primera vez^)...
  pushd frontend
  call npm install --no-fund --no-audit
  if errorlevel 1 (
    popd
    echo [ERROR] Falló la instalación de dependencias del frontend.
    goto :error
  )
  popd
)
echo [..] Iniciando el sitio web...
start "TurismoGo - Web" /D "%~dp0frontend" cmd /k "npm run dev"

echo.
echo  ----------------------------------------------
echo   Listo. El navegador se abrirá en:
echo     http://localhost:9000
echo.
echo   Cuentas de prueba (contraseña Demo2026!):
echo     Viajero:  ana.quispe@correo.pe
echo     Empresa:  ventas@andesexplorer.pe
echo     Admin:    admin@turismogo.pe  (Admin2026!)
echo.
echo   Para detener todo, ejecuta detener.bat
echo  ----------------------------------------------
echo.
pause
exit /b 0

:error
echo.
pause
exit /b 1
