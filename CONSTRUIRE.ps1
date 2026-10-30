param([string]$OutputName='ReplyAI.exe')
$ErrorActionPreference = 'Stop'
$fx = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
Push-Location $PSScriptRoot
try {
 $refs = @('System.IO.Compression.dll','System.IO.Compression.FileSystem.dll','System.Net.Http.dll','System.Web.Extensions.dll','System.Security.dll','System.Windows.Forms.dll','System.Drawing.dll','System.Xaml.dll') + @('PresentationFramework.dll','PresentationCore.dll','WindowsBase.dll','UIAutomationClient.dll','UIAutomationTypes.dll' | ForEach-Object { Join-Path "$fx\WPF" $_ })
 New-Item -ItemType Directory -Force .build | Out-Null
 & "$fx\csc.exe" /nologo /target:winexe /out:.build\ReplyAI.Updater.exe /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll updater\Program.cs updater\UpdatePackage.cs
 if ($LASTEXITCODE -ne 0) { throw 'Compilation du programme de mise à jour impossible.' }
 $argsBuild = @('/nologo','/target:winexe','/optimize+',('/out:'+$OutputName),'/resource:src\Shell.xaml,ParoleRP.Shell.xaml','/resource:.build\ReplyAI.Updater.exe,ParoleRP.Updater.exe','updater\UpdatePackage.cs')
 if (Test-Path src\Icon.ico) { $argsBuild += @('/win32icon:src\Icon.ico','/resource:src\Icon.ico,ParoleRP.Icon.ico') }
 $argsBuild += $refs | ForEach-Object { '/r:' + $_ }
 $argsBuild += Get-ChildItem src\*.cs | ForEach-Object { $_.FullName }
 & "$fx\csc.exe" @argsBuild
 if ($LASTEXITCODE -ne 0) { throw 'La compilation a échoué.' }
} finally { Pop-Location }
