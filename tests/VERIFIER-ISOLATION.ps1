param([string]$WorkingRoot=$env:TEMP)
$ErrorActionPreference='Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
 $folder=Join-Path $WorkingRoot ('ReplyAI-clipboard-isolation-'+[Guid]::NewGuid().ToString('N'))
 New-Item -ItemType Directory -Force $folder | Out-Null
 $fx=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
 & "$fx\csc.exe" /nologo /target:exe "/out:$folder\Tests.exe" /r:System.Web.Extensions.dll "/r:$fx\WPF\PresentationFramework.dll" "/r:$fx\WPF\PresentationCore.dll" "/r:$fx\WPF\WindowsBase.dll" src\ClipboardWorker.cs src\ClipboardChannel.cs src\EngineWorker.cs src\EngineProcesses.cs tests\ClipboardIsolationTests.cs
 if($LASTEXITCODE -ne 0){throw 'Compilation des tests de presse-papiers impossible'}
 & "$folder\Tests.exe"
 if($LASTEXITCODE -ne 0){throw "Un test d’isolation du presse-papiers a échoué"}
} finally {Pop-Location}
