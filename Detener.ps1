$ErrorActionPreference = 'Stop'
$archivoRegistroProyecto = Join-Path $PSScriptRoot 'logs\servicios.json'
if (-not (Test-Path -LiteralPath $archivoRegistroProyecto)) { Write-Host 'No hay una instancia registrada en esta carpeta.'; return }
$registroProyecto = Get-Content -LiteralPath $archivoRegistroProyecto -Raw | ConvertFrom-Json
if ($registroProyecto.Raiz -ne [System.IO.Path]::GetFullPath($PSScriptRoot)) {
    throw 'El registro corresponde a otra carpeta. No se detuvo ningun proceso.'
}
$procesosParaDetenerProyecto = @()
foreach ($datoProyecto in $registroProyecto.Procesos) {
    $procesoProyecto = Get-Process -Id ([int]$datoProyecto.Id) -ErrorAction SilentlyContinue
    if ($procesoProyecto -and $procesoProyecto.StartTime.ToUniversalTime().Ticks.ToString() -eq $datoProyecto.InicioUtc -and $procesoProyecto.Path -eq $datoProyecto.Ejecutable) {
        $procesosParaDetenerProyecto += $procesoProyecto
    }
}
Remove-Item -LiteralPath $archivoRegistroProyecto
foreach ($procesoProyecto in $procesosParaDetenerProyecto) {
    Stop-Process -Id $procesoProyecto.Id -ErrorAction SilentlyContinue
}
Write-Host 'Programa detenido. Los datos siguen guardados para la siguiente sesion.'
