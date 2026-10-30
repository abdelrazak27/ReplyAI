param([string]$WorkingRoot=$env:TEMP)
$ErrorActionPreference='Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
 $fx=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
 $tempTests=Join-Path $WorkingRoot ('ParoleRP-tests-'+[Guid]::NewGuid().ToString('N'))
 New-Item -ItemType Directory $tempTests | Out-Null
 & "$fx\csc.exe" /nologo /target:exe "/out:$tempTests\Tests.exe" /r:System.Net.Http.dll /r:System.Web.Extensions.dll /r:System.Security.dll src\Core.cs src\Connections.cs src\ConnectionDisplay.cs src\ApiErrors.cs tests\Tests.cs
 if($LASTEXITCODE -ne 0){throw 'Compilation des tests impossible.'}
 Push-Location $tempTests
 try { & .\Tests.exe; if($LASTEXITCODE -ne 0){throw 'Un test a échoué.'} } finally { Pop-Location }
} finally { Pop-Location }
