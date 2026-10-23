param(
    [string]$Entrada = (Join-Path $PSScriptRoot 'Documentacion.docx'),
    [string]$Salida = (Join-Path $PSScriptRoot 'Documentacion.pdf')
)
$ErrorActionPreference = 'Stop'
$wordEnsayo = $null
$documentoEnsayo = $null
$instanciaPropiaEnsayo = $false
try {
    $wordEnsayo = New-Object -ComObject Word.Application
    $instanciaPropiaEnsayo = ($wordEnsayo.Documents.Count -eq 0)
    if (-not $instanciaPropiaEnsayo) { throw 'Word devolvio una instancia con documentos abiertos. Se conserva sin cambios.' }
    $wordEnsayo.Visible = $false
    $wordEnsayo.DisplayAlerts = 0
    $wordEnsayo.AutomationSecurity = 3
    $documentoEnsayo = $wordEnsayo.Documents.Open([IO.Path]::GetFullPath($Entrada), $false, $true, $false)
    $documentoEnsayo.Repaginate()
    $documentoEnsayo.ExportAsFixedFormat([IO.Path]::GetFullPath($Salida), 17)
    Write-Output ('PDF exportado. Paginas: ' + $documentoEnsayo.ComputeStatistics(2))
    Write-Output ([IO.Path]::GetFullPath($Salida))
} finally {
    if ($documentoEnsayo) { $documentoEnsayo.Close(0) | Out-Null }
    if ($wordEnsayo -and $instanciaPropiaEnsayo) { $wordEnsayo.Quit() | Out-Null }
    if ($documentoEnsayo) { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($documentoEnsayo) }
    if ($wordEnsayo) { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($wordEnsayo) }
}
