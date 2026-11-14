# Verificación completa del proyecto en local (sin servicios externos).
# Uso:  powershell -ExecutionPolicy Bypass -File scripts/verificar.ps1
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot

function Paso($titulo, [scriptblock]$accion) {
    Write-Host "`n==> $titulo" -ForegroundColor Cyan
    & $accion
    if ($LASTEXITCODE -ne 0) { Write-Host "FALLÓ: $titulo" -ForegroundColor Red; exit $LASTEXITCODE }
}

Push-Location "$raiz/backend"
try {
    Paso 'Backend: compilación' { dotnet build TurismoGo.sln -nologo -v q }
    Paso 'Backend: pruebas' { dotnet test TurismoGo.sln --no-build -nologo }
    Paso 'Backend: paquetes vulnerables' {
        $salida = dotnet list TurismoGo.sln package --vulnerable --include-transitive
        $salida | Write-Host
        if ($salida -match 'tiene los paquetes vulnerables|has the following vulnerable packages') { $global:LASTEXITCODE = 1 }
    }
}
finally { Pop-Location }

Push-Location "$raiz/frontend"
try {
    Paso 'Frontend: lint' { npm run lint }
    Paso 'Frontend: pruebas' { npm test }
    Paso 'Frontend: auditoría de dependencias' { npm audit --audit-level=moderate }
    Paso 'Frontend: build de producción' { npm run build }
}
finally { Pop-Location }

Write-Host "`nTodo correcto." -ForegroundColor Green
