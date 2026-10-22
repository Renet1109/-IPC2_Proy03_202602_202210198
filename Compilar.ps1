param([ValidateSet('Debug','Release')][string]$Configuracion = 'Release')
$ErrorActionPreference = 'Stop'
$dotnetProyecto = (Get-Command dotnet -ErrorAction Stop).Source
$sdkRaizProyecto = Join-Path (Split-Path $dotnetProyecto) 'sdk'
$sdkProyecto = Get-ChildItem -LiteralPath $sdkRaizProyecto -Directory |
    Where-Object { $_.Name -match '^10\.0\.\d+$' } |
    Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
if (-not $sdkProyecto) { throw 'Instala el SDK de .NET 10 para compilar este proyecto.' }
$env:MSBuildEnableWorkloadResolver = 'false'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = 'true'
Push-Location $PSScriptRoot
try {
    & $dotnetProyecto (Join-Path $sdkProyecto.FullName 'MSBuild.dll') 'Proyecto3.slnx' '-restore' '-t:Build' '-verbosity:minimal' '-nodeReuse:false' '-p:UseSharedCompilation=false' "-p:Configuration=$Configuracion"
    if ($LASTEXITCODE -ne 0) { throw 'La compilación no finalizó correctamente.' }
} finally { Pop-Location }
