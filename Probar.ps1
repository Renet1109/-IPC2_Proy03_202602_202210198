$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Compilar.ps1')
$dotnetProyecto = (Get-Command dotnet -ErrorAction Stop).Source
$entradaProyecto = Join-Path $PSScriptRoot 'pruebas\entrada'
$datosPruebaProyecto = Join-Path $PSScriptRoot 'pruebas\App_Data'
& $dotnetProyecto (Join-Path $PSScriptRoot 'pruebas\SatDte.Tests\bin\Release\net10.0\SatDte.Tests.dll') $entradaProyecto $datosPruebaProyecto
if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas del núcleo.' }
Write-Host 'Para comprobar HTTP: python pruebas/verificar_http.py'
