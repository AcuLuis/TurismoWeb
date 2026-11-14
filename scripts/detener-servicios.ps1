# Detiene la API (puerto 5259) y el sitio web (puerto 9000) iniciados con iniciar.bat.
# Si el proceso fue abierto por iniciar.bat, también cierra su ventana; nunca cierra otras terminales.
$servicios = @(
    @{ Nombre = 'La API'; Puerto = 5259; Marca = 'TurismoGo.Api' },
    @{ Nombre = 'El sitio web'; Puerto = 9000; Marca = 'npm run dev' }
)

foreach ($s in $servicios) {
    $conexion = Get-NetTCPConnection -LocalPort $s.Puerto -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $conexion) { Write-Host "[--] $($s.Nombre) no estaba en ejecución"; continue }

    $proceso = Get-CimInstance Win32_Process -Filter "ProcessId=$($conexion.OwningProcess)"
    $objetivo = $proceso

    # Busca la ventana "cmd /k" que abrió iniciar.bat (identificada por su línea de comandos)
    $actual = $proceso
    while ($actual -and $actual.ParentProcessId) {
        $padre = Get-CimInstance Win32_Process -Filter "ProcessId=$($actual.ParentProcessId)"
        if (-not $padre) { break }
        if ($padre.Name -eq 'cmd.exe' -and $padre.CommandLine -like "*$($s.Marca)*") { $objetivo = $padre; break }
        $actual = $padre
    }

    taskkill /PID $objetivo.ProcessId /T /F | Out-Null
    Write-Host "[OK] $($s.Nombre) se detuvo"
}
