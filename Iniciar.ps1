param(
    [switch]$SinCompilar,
    [switch]$NoAbrirNavegador,
    [ValidateRange(1024,65535)][int]$PuertoWeb = 5000,
    [ValidateRange(1024,65535)][int]$PuertoApi = 5001
)
$ErrorActionPreference = 'Stop'
if (-not $SinCompilar) { & (Join-Path $PSScriptRoot 'Compilar.ps1') }
& (Join-Path $PSScriptRoot 'Ejecutar.ps1') -CarpetaApi (Join-Path $PSScriptRoot 'src\SatDte.Api') -CarpetaWeb (Join-Path $PSScriptRoot 'src\SatDte.Web') -ArchivoApi 'bin\Release\net10.0\SatDte.Api.dll' -ArchivoWeb 'bin\Release\net10.0\SatDte.Web.dll' -PuertoWeb $PuertoWeb -PuertoApi $PuertoApi -NoAbrirNavegador:$NoAbrirNavegador
