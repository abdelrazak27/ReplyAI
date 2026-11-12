param([string]$WorkingRoot=$env:TEMP)
$ErrorActionPreference='Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
 New-Item -ItemType Directory -Force .build | Out-Null
 $fx=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
 & "$fx\csc.exe" /nologo /out:.build\UsageTests.exe /r:System.Net.Http.dll /r:System.Web.Extensions.dll /r:System.Security.dll src\Core.cs src\TranslationSupport.cs src\Connections.cs src\Usage.cs src\UndoState.cs tests\UsageTests.cs
 if($LASTEXITCODE -ne 0){throw 'Compilation impossible'}
 & .\.build\UsageTests.exe $WorkingRoot
 if($LASTEXITCODE -ne 0){throw 'Tests de consommation et annulation échoués'}
} finally {Pop-Location}
