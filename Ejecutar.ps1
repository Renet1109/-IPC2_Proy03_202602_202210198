param(
    [Parameter(Mandatory=$true)][string]$CarpetaApi,
    [Parameter(Mandatory=$true)][string]$CarpetaWeb,
    [string]$ArchivoApi = 'SatDte.Api.dll',
    [string]$ArchivoWeb = 'SatDte.Web.dll',
    [ValidateRange(1024,65535)][int]$PuertoWeb = 5000,
    [ValidateRange(1024,65535)][int]$PuertoApi = 5001,
    [switch]$NoAbrirNavegador
)
$ErrorActionPreference = 'Stop'
if ($PuertoWeb -eq $PuertoApi) { throw 'La Web y la API necesitan puertos diferentes.' }
$dotnetProyecto = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
if (-not $dotnetProyecto) { throw 'Instala el runtime de ASP.NET Core 10 x64. No se encontro dotnet en PATH.' }
$runtimesProyecto = & $dotnetProyecto --list-runtimes
if (-not ($runtimesProyecto -match '^Microsoft.AspNetCore.App 10\.')) { throw 'Se necesita el runtime de ASP.NET Core 10 x64 (o el SDK .NET 10).' }
foreach ($puertoProyecto in @($PuertoWeb,$PuertoApi)) {
    $listenerProyecto = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback,$puertoProyecto)
    try { $listenerProyecto.Start() }
    catch { throw "El puerto $puertoProyecto esta ocupado. Cierra la instancia anterior o elige otros puertos al ejecutar Iniciar.ps1." }
    finally { $listenerProyecto.Stop() }
}
$serviciosProyecto = @(
    @{ Nombre='SatDte.Api'; Carpeta=[System.IO.Path]::GetFullPath($CarpetaApi); Archivo=$ArchivoApi; Puerto=$PuertoApi; Salud="http://localhost:$PuertoApi/salud" },
    @{ Nombre='SatDte.Web'; Carpeta=[System.IO.Path]::GetFullPath($CarpetaWeb); Archivo=$ArchivoWeb; Puerto=$PuertoWeb; Salud="http://localhost:$PuertoWeb/" }
)
foreach ($servicioProyecto in $serviciosProyecto) {
    if (-not (Test-Path -LiteralPath (Join-Path $servicioProyecto.Carpeta $servicioProyecto.Archivo))) {
        throw "Falta $($servicioProyecto.Nombre). Extrae todo el ZIP o compila primero el proyecto."
    }
}
$carpetaLogsProyecto = Join-Path $PSScriptRoot 'logs'
New-Item -ItemType Directory -Path $carpetaLogsProyecto -Force | Out-Null
$archivoRegistroProyecto = Join-Path $carpetaLogsProyecto 'servicios.json'
$identificadorProyecto = [guid]::NewGuid().ToString('N')
$procesosProyecto = @()
$apiAnteriorProyecto = $env:Api__BaseUrl
$env:Api__BaseUrl = "http://localhost:$PuertoApi/"
function Esperar-ServicioProyecto($procesoProyecto,$servicioProyecto) {
    $relojArranqueProyecto = [System.Diagnostics.Stopwatch]::StartNew()
    while ($relojArranqueProyecto.Elapsed.TotalSeconds -lt 30) {
        if ($procesoProyecto.HasExited) {
            $detalleProyecto = Get-Content -LiteralPath (Join-Path $carpetaLogsProyecto ($servicioProyecto.Nombre + '.error.log')) -Tail 8 -ErrorAction SilentlyContinue
            throw "$($servicioProyecto.Nombre) no pudo iniciar. Revisa logs/. $detalleProyecto"
        }
        try {
            $respuestaProyecto = Invoke-WebRequest -UseBasicParsing -Uri $servicioProyecto.Salud -TimeoutSec 1
            if ($respuestaProyecto.StatusCode -eq 200) { return }
        } catch { }
        Start-Sleep -Milliseconds 150
    }
    throw "$($servicioProyecto.Nombre) no respondio en 30 segundos. Revisa logs/."
}
try {
    foreach ($servicioProyecto in $serviciosProyecto) {
        $dllProyecto = Join-Path $servicioProyecto.Carpeta $servicioProyecto.Archivo
        $argumentosProyecto = '"' + $dllProyecto + '" --urls "http://localhost:' + $servicioProyecto.Puerto + '"'
        $procesoProyecto = Start-Process -FilePath $dotnetProyecto -ArgumentList $argumentosProyecto -WorkingDirectory $servicioProyecto.Carpeta -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $carpetaLogsProyecto ($servicioProyecto.Nombre + '.log')) -RedirectStandardError (Join-Path $carpetaLogsProyecto ($servicioProyecto.Nombre + '.error.log'))
        $procesosProyecto += $procesoProyecto
        Esperar-ServicioProyecto $procesoProyecto $servicioProyecto
    }
    $registroProyecto = @{
        Id=$identificadorProyecto
        Raiz=[System.IO.Path]::GetFullPath($PSScriptRoot)
        Procesos=@($procesosProyecto | ForEach-Object { @{ Id=$_.Id; InicioUtc=$_.StartTime.ToUniversalTime().Ticks.ToString(); Ejecutable=$dotnetProyecto } })
    }
    $registroProyecto | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $archivoRegistroProyecto -Encoding UTF8
    Write-Host "Programa listo: http://localhost:$PuertoWeb" -ForegroundColor Green
    Write-Host "API: http://localhost:$PuertoApi"
    Write-Host 'Deja esta ventana abierta. Usa Ctrl+C o Detener.cmd para cerrar ambos servicios.'
    if (-not $NoAbrirNavegador) { Start-Process -FilePath "http://localhost:$PuertoWeb" }
    while (Test-Path -LiteralPath $archivoRegistroProyecto) {
        foreach ($procesoProyecto in $procesosProyecto) {
            if ($procesoProyecto.HasExited) { throw 'Un servicio se detuvo. Consulta los archivos de la carpeta logs/.' }
        }
        Start-Sleep -Milliseconds 400
    }
} finally {
    $env:Api__BaseUrl = $apiAnteriorProyecto
    foreach ($procesoProyecto in $procesosProyecto) {
        if (-not $procesoProyecto.HasExited) { Stop-Process -Id $procesoProyecto.Id -ErrorAction SilentlyContinue }
    }
    if (Test-Path -LiteralPath $archivoRegistroProyecto) {
        $registroActualProyecto = Get-Content -LiteralPath $archivoRegistroProyecto -Raw | ConvertFrom-Json
        if ($registroActualProyecto.Id -eq $identificadorProyecto) { Remove-Item -LiteralPath $archivoRegistroProyecto }
    }
}
