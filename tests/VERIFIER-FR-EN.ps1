param([string]$WorkingRoot=$env:TEMP,[string]$Python='python')
$ErrorActionPreference='Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
 $folder=Join-Path $WorkingRoot ('ReplyAI-fr-en-'+[Guid]::NewGuid().ToString('N'))
 New-Item -ItemType Directory -Force $folder | Out-Null
 $fx=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
 $refs=@('/r:System.Xaml.dll','/r:System.Net.Http.dll','/r:System.Web.Extensions.dll','/r:System.Security.dll')
 $refs+=@('PresentationFramework.dll','PresentationCore.dll','WindowsBase.dll'|ForEach-Object{"/r:$fx\WPF\$_"})
 $core=@('src\Core.cs','src\TranslationSupport.cs')
 & "$fx\csc.exe" /nologo "/out:$folder\UiTests.exe" @refs @core src\Usage.cs src\ChatOverlayWindow.cs tests\TranslationUiTests.cs
 if($LASTEXITCODE -ne 0){throw 'Compilation des tests UI impossible'}
 & "$folder\UiTests.exe" $folder
 if($LASTEXITCODE -ne 0){throw 'Echec des tests UI'}
 & "$fx\csc.exe" /nologo "/out:$folder\ReaderTests.exe" @refs @core src\FiveMChatReader.cs tests\ChatReaderTests.cs
 if($LASTEXITCODE -ne 0){throw 'Compilation des tests chat impossible'}
 & $Python tests\run_chat_reader_test.py "$folder\ReaderTests.exe"
 if($LASTEXITCODE -ne 0){throw 'Echec du lecteur de chat'}
 & "$fx\csc.exe" /nologo "/out:$folder\ProtocolTests.exe" @refs @core src\Connections.cs src\Usage.cs src\ApiErrors.cs src\EngineWorker.cs src\EngineProcesses.cs src\PythonApi.cs tests\TranslationProtocolTests.cs
 if($LASTEXITCODE -ne 0){throw 'Compilation des tests de protocole impossible'}
 & "$folder\ProtocolTests.exe" --run
 if($LASTEXITCODE -ne 0){throw 'Echec du protocole de traduction'}
 & $Python -m unittest discover -s tests -p test_translation.py
 if($LASTEXITCODE -ne 0){throw 'Echec des tests du moteur FR-EN'}
} finally {Pop-Location}
