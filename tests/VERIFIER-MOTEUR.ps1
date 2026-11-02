param([string]$WorkingRoot=$env:TEMP)
$ErrorActionPreference='Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
 $tempTests=Join-Path $WorkingRoot ('ReplyAI-transport-'+[Guid]::NewGuid().ToString('N'))
 New-Item -ItemType Directory -Force $tempTests | Out-Null
 $fx=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
 & "$fx\csc.exe" /nologo /target:exe "/out:$tempTests\FakeEngine.exe" /r:System.Web.Extensions.dll tests\FakeEngine.cs
 if($LASTEXITCODE -ne 0){throw 'Compilation du moteur factice impossible'}
 & "$fx\csc.exe" /nologo /target:exe "/out:$tempTests\Tests.exe" /r:System.Net.Http.dll /r:System.Web.Extensions.dll /r:System.Security.dll src\Core.cs src\Connections.cs src\ApiErrors.cs src\Usage.cs src\PythonApi.cs src\EngineWorker.cs src\EngineProcesses.cs src\EngineDiagnostics.cs tests\EngineDeadlineTests.cs
 if($LASTEXITCODE -ne 0){throw 'Compilation des tests de communication impossible'}
 & "$tempTests\Tests.exe" $tempTests "$tempTests\FakeEngine.exe"
 if($LASTEXITCODE -ne 0){throw 'Un test de communication a échoué'}
} finally {Pop-Location}
