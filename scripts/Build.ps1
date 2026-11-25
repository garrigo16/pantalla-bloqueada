param([string]$OutputDirectory, [string]$PinballPack, [string]$GodotZip)
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
$lunarResource = '/resource:' + (Join-Path $repo 'assets\lunar-backdrop.jpg') + ',LunarBackdrop.jpg'
$gameResources = @()
if (-not $PinballPack) {
    & (Join-Path $PSScriptRoot 'BuildPinball.ps1') -GodotZip $GodotZip
    $pinBuilt = Get-Content -LiteralPath (Join-Path $build 'pinball-build.json') -Raw | ConvertFrom-Json
    $PinballPack = $pinBuilt.Pack; $GodotZip = $pinBuilt.Zip
}
if ($PinballPack -or $GodotZip) {
    if (-not $PinballPack -or -not $GodotZip) { throw 'Indica el paquete local y la distribución oficial de Godot.' }
    if ((Get-FileHash -LiteralPath $GodotZip -Algorithm SHA256).Hash.ToLowerInvariant() -ne '731980f9608d61333e5baf54a2ef17210acc7a538446c0cb9969f002aca1e953') { throw 'La distribución de Godot no coincide con la oficial.' }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $pinArchive = [IO.Compression.ZipFile]::OpenRead($GodotZip)
    try {
        $pinInput = $pinArchive.GetEntry('Godot_v4.7.2-stable_win64.exe').Open()
        try { $pinHash = [BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash($pinInput)).Replace('-','').ToLowerInvariant() } finally { $pinInput.Dispose() }
    } finally { $pinArchive.Dispose() }
    $pinManifest = Join-Path $build 'PinballManifest.txt'
    [IO.File]::WriteAllLines($pinManifest, @($pinHash,(Get-FileHash -LiteralPath $PinballPack -Algorithm SHA256).Hash.ToLowerInvariant()),[Text.UTF8Encoding]::new($false))
    $gameResources += @(('/resource:' + [IO.Path]::GetFullPath($PinballPack) + ',CatPinball.pck'),('/resource:' + [IO.Path]::GetFullPath($GodotZip) + ',Godot.zip'),('/resource:' + $pinManifest + ',PinballManifest.txt'),('/resource:' + (Join-Path $repo 'integration\Godot-LICENSE.txt') + ',GodotLicense.txt'))
}
else { throw 'Esta versión requiere el paquete local de Cat Pinball y la distribución oficial de Godot.' }
$appExe = Join-Path $build 'DrifftAnalyzer.exe'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 $iconOption $iconResource $lunarResource @gameResources '/reference:System.Windows.Forms.dll' '/reference:System.Drawing.dll' '/reference:System.Xaml.dll' '/reference:System.Net.Http.dll' '/reference:System.Runtime.Serialization.dll' @references ('/out:' + $appExe) @shared @appSources
if ($LASTEXITCODE -ne 0) { throw 'Error al compilar la aplicación.' }
$remove = Join-Path $OutputDirectory 'Desinstalar.exe'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 $iconOption /define:UNINSTALLER '/reference:System.Windows.Forms.dll' '/reference:System.Drawing.dll' ('/out:' + $remove) @shared @setup
if ($LASTEXITCODE -ne 0) { throw 'Error al compilar el desinstalador.' }
$full = Join-Path $OutputDirectory 'DrifftAnalyzer-Setup.exe'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 $iconOption '/reference:System.Windows.Forms.dll' '/reference:System.Drawing.dll' ('/resource:' + $appExe + ',Application.exe') ('/resource:' + $remove + ',Uninstaller.exe') ('/out:' + $full) @shared @setup
if ($LASTEXITCODE -ne 0) { throw 'Error al compilar el instalador.' }
$fullHash = (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant()
$fullSize = (Get-Item -LiteralPath $full).Length
$bootstrapInfo = Join-Path $build 'BootstrapPackage.cs'
[IO.File]::WriteAllText($bootstrapInfo, ('internal static class BootstrapPackage { internal const string Sha256 = "' + $fullHash + '"; internal const long Size = ' + $fullSize + 'L; }'), [Text.UTF8Encoding]::new($false))
$start = Join-Path $OutputDirectory 'Iniciar.exe'
& $compiler /nologo /target:winexe /main:Bootstrap /platform:anycpu /optimize+ /codepage:65001 $iconOption '/reference:System.Windows.Forms.dll' '/reference:System.Drawing.dll' '/reference:System.Net.Http.dll' '/reference:System.Runtime.Serialization.dll' ('/out:' + $start) @shared $bootstrapInfo (Join-Path $repo 'src\Bootstrap\Bootstrap.cs') (Join-Path $repo 'src\App\ReleaseClient.cs') (Join-Path $repo 'src\App\UpdateTransport.cs')
if ($LASTEXITCODE -ne 0 -or (Get-Item -LiteralPath $start).Length -ge 64MB) { throw 'El instalador inicial no es compatible con las versiones anteriores.' }
$package = Join-Path $OutputDirectory 'package\DrifftAnalyzer'
New-Item -ItemType Directory -Path $package -Force | Out-Null
Copy-Item -LiteralPath $start,$full,$remove -Destination $package
Copy-Item -LiteralPath (Join-Path $repo 'docs\PARA_COMPARTIR.txt') -Destination (Join-Path $package 'LEEME.txt')
Copy-Item -LiteralPath (Join-Path $repo 'THIRD_PARTY_NOTICES.md'),(Join-Path $repo 'integration\Godot-LICENSE.txt'),(Join-Path $repo 'integration\godot\Nunito-OFL.txt'),(Join-Path $repo 'game\cat-pinball\fonts\LICENSE-NotoSansTC.txt') -Destination $package
$archive = Join-Path $OutputDirectory 'DrifftAnalyzer.zip'
Compress-Archive -LiteralPath $package -DestinationPath $archive -Force -CompressionLevel Optimal
$checksums = @($start,$full,$remove,$archive) | ForEach-Object { (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + (Split-Path -Leaf $_) }
[IO.File]::WriteAllLines((Join-Path $OutputDirectory 'SHA256SUMS.txt'), $checksums, [Text.UTF8Encoding]::new($false))
$version = [Diagnostics.FileVersionInfo]::GetVersionInfo($appExe).ProductVersion
$assets = @($start,$full) | ForEach-Object { @{ name = (Split-Path -Leaf $_); state = 'uploaded'; browser_download_url = ('https://github.com/garrigo16/pantalla-bloqueada/releases/download/v' + $version + '/' + (Split-Path -Leaf $_)); digest = ('sha256:' + (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()); size = (Get-Item -LiteralPath $_).Length } }
@{ tag_name = ('v' + $version); draft = $false; prerelease = $false; assets = @($assets) } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'update.json') -Encoding utf8
Write-Output ('Versión ' + [Diagnostics.FileVersionInfo]::GetVersionInfo($appExe).ProductVersion + ' preparada en ' + $OutputDirectory)
