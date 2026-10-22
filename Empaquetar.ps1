param([string]$Destino)
$ErrorActionPreference = 'Stop'
if (-not $Destino) { $Destino = Join-Path $PSScriptRoot 'dist\SAT_DTE_Windows_202210198' }
$destinoProyecto = [System.IO.Path]::GetFullPath($Destino)
if ((Test-Path -LiteralPath $destinoProyecto) -or (Test-Path -LiteralPath ($destinoProyecto + '.zip'))) { throw 'La carpeta o ZIP de destino ya existe. Elige un destino nuevo para conservar sus datos.' }
& (Join-Path $PSScriptRoot 'Compilar.ps1')
$dotnetProyecto = (Get-Command dotnet).Source
$sdkProyecto = Get-ChildItem -LiteralPath (Join-Path (Split-Path $dotnetProyecto) 'sdk') -Directory |
    Where-Object { $_.Name -match '^10\.0\.\d+$' } | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
New-Item -ItemType Directory -Path $destinoProyecto -Force | Out-Null
foreach ($nombreProyecto in @('Api','Web')) {
    $proyectoProyecto = Join-Path $PSScriptRoot "src\SatDte.$nombreProyecto\SatDte.$nombreProyecto.csproj"
    $carpetaPublicadaProyecto = Join-Path $destinoProyecto "aplicacion\$nombreProyecto"
    & $dotnetProyecto (Join-Path $sdkProyecto.FullName 'MSBuild.dll') $proyectoProyecto '-restore' '-t:Publish' '-verbosity:minimal' '-nodeReuse:false' '-p:UseSharedCompilation=false' '-p:Configuration=Release' '-p:SelfContained=false' "-p:PublishDir=$carpetaPublicadaProyecto\"
    if ($LASTEXITCODE -ne 0) { throw "No se pudo publicar $nombreProyecto." }
}
foreach ($archivoProyecto in @('Ejecutar.ps1','Detener.ps1','Iniciar.cmd','Detener.cmd')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $archivoProyecto) -Destination $destinoProyecto
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs') -Destination $destinoProyecto -Recurse
$carpetaPruebasProyecto = Join-Path $destinoProyecto 'pruebas'
New-Item -ItemType Directory -Path $carpetaPruebasProyecto -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'pruebas\entrada') -Destination $carpetaPruebasProyecto -Recurse
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'pruebas\SAT-DTE.postman_collection.json') -Destination $carpetaPruebasProyecto
@'
param(
    [switch]$NoAbrirNavegador,
    [ValidateRange(1024,65535)][int]$PuertoWeb=5000,
    [ValidateRange(1024,65535)][int]$PuertoApi=5001
)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Ejecutar.ps1') -CarpetaApi (Join-Path $PSScriptRoot 'aplicacion\Api') -CarpetaWeb (Join-Path $PSScriptRoot 'aplicacion\Web') -PuertoWeb $PuertoWeb -PuertoApi $PuertoApi -NoAbrirNavegador:$NoAbrirNavegador
'@ | Set-Content -LiteralPath (Join-Path $destinoProyecto 'Iniciar.ps1') -Encoding UTF8
@'
SAT / DTE - William René Toledo Corado - 202210198 - Seccion B

VERSION COMPILADA PARA WINDOWS X64

1. Extrae TODO el ZIP a una carpeta con permiso de escritura.
2. Haz doble clic en Iniciar.cmd. No lo abras desde dentro del ZIP.
3. Se abrira el navegador en http://localhost:5000 cuando los servicios esten listos.
4. Deja la ventana del programa abierta. Para cerrar, usa Detener.cmd o Ctrl+C.

Requiere el runtime de ASP.NET Core 10 x64 o el SDK .NET 10. No requiere
compilar, instalar paquetes NuGet, Python ni Node. La version .NET ya instalada
en el equipo donde se preparo este paquete permite ejecutarlo directamente.

Carga primero pruebas/entrada/01_rtu_completo.json y despues
02_facturas_marzo_graficas.json. Revisa los resultados y abre Peticiones.
Ejemplo: NIT 999999K, del 2025-03-01 al 2025-03-02.

Los datos comienzan vacios y se guardan en aplicacion/Api/App_Data/estado.json.
No borres esta carpeta si deseas conservar tus datos. Inicializar en la Web
elimina los registros del simulador. Detener el programa conserva los datos.

La documentacion esta en docs/Documentacion.pdf y tambien se descarga en Ayuda.
Si hay un error de inicio, revisa logs/ y comprueba que 5000 y 5001 esten libres.
Puedes cambiar puertos desde PowerShell:
  .\Iniciar.ps1 -PuertoWeb 5010 -PuertoApi 5011

Este paquete es la aplicacion compilada. El ZIP IPC2_Proy03_202602_202210198
contiene el codigo fuente, la solucion de C# y las pruebas reproducibles.
'@ | Set-Content -LiteralPath (Join-Path $destinoProyecto 'LEEME.txt') -Encoding UTF8
Compress-Archive -LiteralPath $destinoProyecto -DestinationPath ($destinoProyecto + '.zip') -CompressionLevel Optimal
Write-Host "Paquete creado: $destinoProyecto.zip"
