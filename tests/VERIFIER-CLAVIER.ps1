$ErrorActionPreference='Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
 $fx=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
 $target=Join-Path $PSScriptRoot 'NativeKeyboardTests.exe'
 & "$fx\csc.exe" /nologo /target:exe "/out:$target" src\Native.cs tests\NativeKeyboardTests.cs
 if($LASTEXITCODE -ne 0){throw 'Compilation des tests clavier impossible.'}
 & $target
 if($LASTEXITCODE -ne 0){throw 'Un test clavier a échoué.'}
} finally {Pop-Location}
