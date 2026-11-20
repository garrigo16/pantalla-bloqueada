$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$build = Join-Path $repo 'build\tests'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (-not (Test-Path -LiteralPath (Join-Path $framework 'csc.exe'))) { $framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319' }
$compiler = Join-Path $framework 'csc.exe'
$references = @('PresentationCore','PresentationFramework','WindowsBase') | ForEach-Object { '/reference:' + (Join-Path $framework ('WPF\' + $_ + '.dll')) }
$app = @(Get-ChildItem -LiteralPath (Join-Path $repo 'src\App') -Filter '*.cs' -File | Select-Object -ExpandProperty FullName)
$shared = @(Get-ChildItem -LiteralPath (Join-Path $repo 'src\Shared') -Filter '*.cs' -File | Select-Object -ExpandProperty FullName)
New-Item -ItemType Directory -Path $build -Force | Out-Null
$packageTests = Join-Path $build 'PackageTests.exe'
& $compiler /nologo /target:exe /main:PackageTests /codepage:65001 ('/out:' + $packageTests) (Join-Path $repo 'src\Setup\Installation.cs') (Join-Path $repo 'tests\PackageTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Error al compilar las pruebas del instalador.' }
& $packageTests (Join-Path $repo 'artifacts\Iniciar.exe') (Join-Path $repo 'build\PantallaBloqueada.exe') (Join-Path $repo 'artifacts\Desinstalar.exe')
if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas del instalador.' }
$updateTests = Join-Path $build 'UpdateTests.exe'
& $compiler /nologo /target:exe /main:UpdateTests /codepage:65001 '/reference:System.Windows.Forms.dll' '/reference:System.Drawing.dll' '/reference:System.Xaml.dll' '/reference:System.Net.Http.dll' '/reference:System.Runtime.Serialization.dll' @references ('/out:' + $updateTests) @shared @app (Join-Path $repo 'tests\UpdateTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Error al compilar las pruebas del actualizador.' }
& $updateTests
if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas del actualizador.' }
