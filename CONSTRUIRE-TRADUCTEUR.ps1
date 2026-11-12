param([Parameter(Mandatory=$true)][string]$Python,[Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'
$Python=(Resolve-Path $Python).Path
$Destination=[IO.Path]::GetFullPath($Destination)
$build=Join-Path $PSScriptRoot '.build/local-translation'
New-Item -ItemType Directory -Force $build | Out-Null
& $Python (Join-Path $PSScriptRoot 'src/prepare_local_model.py') (Join-Path $build 'model')
if($LASTEXITCODE -ne 0){throw 'Conversion du modèle impossible.'}
& $Python -m PyInstaller --noconfirm --clean --name reply-translate --onedir --console --exclude-module torch --exclude-module transformers --exclude-module huggingface_hub --collect-all ctranslate2 --copy-metadata sentencepiece --copy-metadata numpy --distpath $Destination --workpath (Join-Path $build 'pyinstaller') --specpath $build (Join-Path $PSScriptRoot 'src/local_translate.py')
if($LASTEXITCODE -ne 0){throw 'Compilation du traducteur impossible.'}
$pack=Join-Path $Destination 'reply-translate'
Copy-Item (Join-Path $build 'model') (Join-Path $pack 'model') -Recurse -Force
Copy-Item (Join-Path $PSScriptRoot 'notices/Apache-2.0.txt') (Join-Path $pack 'model/LICENSE')
Copy-Item (Join-Path $PSScriptRoot 'notices/LOCAL-TRANSLATION.md') (Join-Path $pack 'NOTICE.md')
# Delvewheel's renamed VC runtime DLL must retain its directory and exact name.
$site=& $Python -c 'import sysconfig; print(sysconfig.get_paths()["purelib"])'
$sentencepieceLibs=Join-Path $pack '_internal/sentencepiece.libs'
New-Item -ItemType Directory -Force $sentencepieceLibs | Out-Null
Copy-Item (Join-Path $site 'sentencepiece.libs/*.dll') $sentencepieceLibs -Force
Write-Output "Traducteur prêt : $pack"
