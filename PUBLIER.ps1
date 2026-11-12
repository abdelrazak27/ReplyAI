$ErrorActionPreference='Stop'
Push-Location $PSScriptRoot
try {
 $version=(Get-Content VERSION -Raw).Trim()
 if($version -notmatch '^\d+\.\d+\.\d+$'){throw 'Version invalide'}
 if(!(Test-Path engine-v1.9/parole-engine.exe)){throw 'Construire le moteur avant la publication'}
 if(!(Test-Path engine-local/reply-translate.exe) -or !(Test-Path engine-local/model/model.bin)){throw 'Construire le traducteur local avant la publication'}
 & ./CONSTRUIRE.ps1
 $destination=Join-Path $PSScriptRoot ('.build/package-'+[Guid]::NewGuid().ToString('N'))
 $package=Join-Path $destination 'ReplyAI'
 New-Item -ItemType Directory -Force $package | Out-Null
 Copy-Item ReplyAI.exe,LANCER.cmd,LISEZ-MOI.md $package
 Copy-Item engine-v1.9,engine-local,brand $package -Recurse
 if(Test-Path notices){Copy-Item notices (Join-Path $package 'engine-v1.9/licenses') -Recurse -Force}
 $files=@(Get-ChildItem $package -File -Recurse | Sort-Object FullName | ForEach-Object {@{Path=$_.FullName.Substring($package.Length+1).Replace('\','/');Sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}})
 @{Product='Reply AI';Version=$version;Files=$files}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $package 'update-manifest.json') -Encoding UTF8
 $zip=Join-Path $PSScriptRoot '.build/ReplyAI-win-x64.zip'
 if(Test-Path $zip){Remove-Item -LiteralPath $zip}
 Compress-Archive -LiteralPath $package -DestinationPath $zip -CompressionLevel Optimal
 Write-Output $zip
} finally {Pop-Location}
