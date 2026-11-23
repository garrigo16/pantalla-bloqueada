param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repo 'artifacts' }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$build = Join-Path $repo 'build'
. (Join-Path $PSScriptRoot 'Compiler.ps1')
$appSources = @(Get-ChildItem -LiteralPath (Join-Path $repo 'src\App') -Filter '*.cs' -File | Select-Object -ExpandProperty FullName)
$shared = @(Get-ChildItem -LiteralPath (Join-Path $repo 'src\Shared') -Filter '*.cs' -File | Select-Object -ExpandProperty FullName)
$setup = @(Get-ChildItem -LiteralPath (Join-Path $repo 'src\Setup') -Filter '*.cs' -File | Select-Object -ExpandProperty FullName)
New-Item -ItemType Directory -Path $build,$OutputDirectory -Force | Out-Null
$icon = Join-Path $build 'Bao.ico'
& (Join-Path $PSScriptRoot 'BuildIcon.ps1') -OutputPath $icon
$iconOption = '/win32icon:' + $icon
$iconResource = '/resource:' + $icon + ',Bao.ico'
$appExe = Join-Path $build 'DrifftAnalyzer.exe'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 $iconOption $iconResource '/reference:System.Windows.Forms.dll' '/reference:System.Drawing.dll' '/reference:System.Xaml.dll' '/reference:System.Net.Http.dll' '/reference:System.Runtime.Serialization.dll' @references ('/out:' + $appExe) @shared @appSources
if ($LASTEXITCODE -ne 0) { throw 'Error al compilar la aplicación.' }
$remove = Join-Path $OutputDirectory 'Desinstalar.exe'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 $iconOption /define:UNINSTALLER '/reference:System.Windows.Forms.dll' '/reference:System.Drawing.dll' ('/out:' + $remove) @shared @setup
if ($LASTEXITCODE -ne 0) { throw 'Error al compilar el desinstalador.' }
$start = Join-Path $OutputDirectory 'Iniciar.exe'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 $iconOption '/reference:System.Windows.Forms.dll' '/reference:System.Drawing.dll' ('/resource:' + $appExe + ',Application.exe') ('/resource:' + $remove + ',Uninstaller.exe') ('/out:' + $start) @shared @setup
if ($LASTEXITCODE -ne 0) { throw 'Error al compilar el instalador.' }
$package = Join-Path $OutputDirectory 'package\DrifftAnalyzer'
New-Item -ItemType Directory -Path $package -Force | Out-Null
Copy-Item -LiteralPath $start,$remove -Destination $package
Copy-Item -LiteralPath (Join-Path $repo 'docs\PARA_COMPARTIR.txt') -Destination (Join-Path $package 'LEEME.txt')
$archive = Join-Path $OutputDirectory 'DrifftAnalyzer.zip'
Compress-Archive -LiteralPath $package -DestinationPath $archive -Force -CompressionLevel Optimal
$checksums = @($start,$remove,$archive) | ForEach-Object { (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + (Split-Path -Leaf $_) }
[IO.File]::WriteAllLines((Join-Path $OutputDirectory 'SHA256SUMS.txt'), $checksums, [Text.UTF8Encoding]::new($false))
Write-Output ('Versión ' + [Diagnostics.FileVersionInfo]::GetVersionInfo($appExe).ProductVersion + ' preparada en ' + $OutputDirectory)
