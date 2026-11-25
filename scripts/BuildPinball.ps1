param([string]$GodotZip)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$build = Join-Path $repo 'build'
New-Item -ItemType Directory -Path $build -Force | Out-Null
if (-not $GodotZip) { $GodotZip = Join-Path $build 'godot.zip' }
if (-not (Test-Path -LiteralPath $GodotZip)) {
    Invoke-WebRequest -Uri 'https://github.com/godotengine/godot-builds/releases/download/4.7.2-stable/Godot_v4.7.2-stable_win64.exe.zip' -OutFile $GodotZip
}
if ((Get-FileHash -LiteralPath $GodotZip -Algorithm SHA256).Hash.ToLowerInvariant() -ne '731980f9608d61333e5baf54a2ef17210acc7a538446c0cb9969f002aca1e953') { throw 'El ZIP de Godot no coincide con la distribución oficial.' }
$engineDirectory = Join-Path $build 'godot'
$engine = Join-Path $engineDirectory 'Godot_v4.7.2-stable_win64_console.exe'
if (-not (Test-Path -LiteralPath $engine)) { Expand-Archive -LiteralPath $GodotZip -DestinationPath $engineDirectory -Force }
New-Item -ItemType File -Path (Join-Path $engineDirectory '_sc_') -Force | Out-Null
$work = Join-Path $build ('pinball-' + [guid]::NewGuid().ToString('N'))
Copy-Item -LiteralPath (Join-Path $repo 'game\cat-pinball') -Destination $work -Recurse
$layer = Join-Path $work 'drifft'
New-Item -ItemType Directory -Path $layer -Force | Out-Null
Copy-Item -Path (Join-Path $repo 'integration\godot\*') -Destination $layer -Force
Copy-Item -LiteralPath (Join-Path $repo 'assets\pinball-nook.png'),(Join-Path $repo 'assets\pinball-blink.png') -Destination $layer
& $engine --headless --path $work --editor --import --log-file (Join-Path $build 'godot-import.log')
if ($LASTEXITCODE -ne 0) { throw 'No se pudo importar el pinball.' }
foreach ($scene in @('drifft_test','typography_test')) {
    $testLog = Join-Path $build ($scene + '.log')
    & $engine --headless --fixed-fps 60 --path $work --quit-after 10000 --log-file $testLog ('res://dev/' + $scene + '.tscn')
    $expected = if ($scene -eq 'drifft_test') { 'SUPERADAS 36 comprobaciones' } else { 'SUPERADAS 9 comprobaciones' }
    if ($LASTEXITCODE -ne 0 -or -not (Select-String -LiteralPath $testLog -SimpleMatch $expected -Quiet)) { throw ('Falló o quedó incompleta la comprobación Godot: ' + $scene) }
}
$pack = Join-Path $build 'CatPinball.pck'
& $engine --headless --path $work --export-pack Web $pack --log-file (Join-Path $build 'godot-export.log')
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $pack)) { throw 'No se pudo exportar el pinball.' }
# Resultado para Build.ps1 sin mezclarlo con la salida del motor.
@{ Pack = $pack; Zip = [IO.Path]::GetFullPath($GodotZip) } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $build 'pinball-build.json') -Encoding utf8
