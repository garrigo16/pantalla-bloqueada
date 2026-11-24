$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$build = Join-Path $repo 'build\tests'
. (Join-Path $PSScriptRoot 'Compiler.ps1')
$app = @(Get-ChildItem -LiteralPath (Join-Path $repo 'src\App') -Filter '*.cs' -File | Select-Object -ExpandProperty FullName)
$shared = @(Get-ChildItem -LiteralPath (Join-Path $repo 'src\Shared') -Filter '*.cs' -File | Select-Object -ExpandProperty FullName)
New-Item -ItemType Directory -Path $build -Force | Out-Null
$packageTests = Join-Path $build 'PackageTests.exe'
& $compiler /nologo /target:exe /main:PackageTests /codepage:65001 ('/out:' + $packageTests) @shared (Join-Path $repo 'src\Setup\Installation.cs') (Join-Path $repo 'tests\PackageTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Error al compilar las pruebas del instalador.' }
& $packageTests (Join-Path $repo 'artifacts\Iniciar.exe') (Join-Path $repo 'build\DrifftAnalyzer.exe') (Join-Path $repo 'artifacts\Desinstalar.exe')
if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas del instalador.' }
$updateTests = Join-Path $build 'UpdateTests.exe'
& $compiler /nologo /target:exe /main:UpdateTests /codepage:65001 '/reference:System.Windows.Forms.dll' '/reference:System.Drawing.dll' '/reference:System.Xaml.dll' '/reference:System.Net.Http.dll' '/reference:System.Runtime.Serialization.dll' @references ('/out:' + $updateTests) @shared @app (Join-Path $repo 'tests\UpdateTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Error al compilar las pruebas del actualizador.' }
& $updateTests
if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas del actualizador.' }
$idleTests = Join-Path $build 'IdleActivationTests.exe'
& $compiler /nologo /target:exe /main:IdleActivationTests /codepage:65001 '/reference:System.Windows.Forms.dll' '/reference:System.Drawing.dll' '/reference:System.Xaml.dll' '/reference:System.Net.Http.dll' '/reference:System.Runtime.Serialization.dll' @references ('/out:' + $idleTests) @shared @app (Join-Path $repo 'tests\IdleActivationTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Error al compilar las pruebas de inactividad.' }
& $idleTests
if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas de inactividad.' }
$funTests = Join-Path $build 'EasterEggTests.exe'
& $compiler /nologo /target:exe /main:EasterEggTests /codepage:65001 ('/out:' + $funTests) @shared (Join-Path $repo 'src\App\EasterEggEngine.cs') (Join-Path $repo 'tests\EasterEggTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Error al compilar las pruebas de easter eggs.' }
& $funTests
if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas de easter eggs.' }
$hotkeyTests = Join-Path $build 'HotkeyTests.exe'
& $compiler /nologo /target:exe /main:HotkeyTests /codepage:65001 ('/out:' + $hotkeyTests) @shared @app (Join-Path $repo 'tests\HotkeyTests.cs') '/reference:System.Windows.Forms.dll' '/reference:System.Drawing.dll' '/reference:System.Xaml.dll' '/reference:System.Net.Http.dll' '/reference:System.Runtime.Serialization.dll' @references
if ($LASTEXITCODE -ne 0) { throw 'Error al compilar las pruebas de atajos.' }
& $hotkeyTests
if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas de atajos.' }
$pinballTests = Join-Path $build 'PinballTests.exe'
& $compiler /nologo /target:exe /main:PinballTests /codepage:65001 ('/out:' + $pinballTests) (Join-Path $repo 'src\App\PinballPhysics.cs') (Join-Path $repo 'tests\PinballTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Error al compilar las pruebas del pinball.' }
& $pinballTests
if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas del pinball.' }
