param([string]$WorkingRoot=$env:TEMP)
$ErrorActionPreference='Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
 $fx=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
 $tempTests=Join-Path $WorkingRoot ('ParoleRP-clipboard-'+[Guid]::NewGuid().ToString('N'))
 New-Item -ItemType Directory $tempTests | Out-Null
 & "$fx\csc.exe" /nologo /target:exe "/out:$tempTests\ClipboardRetryTests.exe" src\ClipboardRetry.cs tests\ClipboardRetryTests.cs
 if($LASTEXITCODE -ne 0){throw 'Compilation des tests impossible.'}
 & "$tempTests\ClipboardRetryTests.exe"
 if($LASTEXITCODE -ne 0){throw 'Un test presse-papiers a échoué.'}
} finally {Pop-Location}
