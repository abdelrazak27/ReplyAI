param([string]$WorkingRoot=$env:TEMP)
$ErrorActionPreference='Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
 New-Item -ItemType Directory -Force .build | Out-Null
 $fx=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
 & "$fx\csc.exe" /nologo /out:.build\UpdateTests.exe /r:System.Web.Extensions.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll updater\UpdatePackage.cs tests\UpdateTests.cs
 if($LASTEXITCODE -ne 0){throw 'Compilation impossible'}
 & .\.build\UpdateTests.exe $WorkingRoot
 if($LASTEXITCODE -ne 0){throw 'Test de mise à jour échoué'}
} finally {Pop-Location}
