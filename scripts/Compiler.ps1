# Entorno compartido de compilación y pruebas (sin dependencias externas).
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (-not (Test-Path -LiteralPath (Join-Path $framework 'csc.exe'))) { $framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319' }
$compiler = Join-Path $framework 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Se necesita .NET Framework 4.8 para compilar.' }
$references = @('PresentationCore','PresentationFramework','WindowsBase') | ForEach-Object { '/reference:' + (Join-Path $framework ('WPF\' + $_ + '.dll')) }
$references += @('/reference:System.IO.Compression.dll')
