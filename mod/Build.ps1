[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameManagedPath,
    [Parameter(Mandatory = $true)][string]$BepInExCorePath,
    [string]$CompilerPath = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    [string]$BuildDirectory = (Join-Path $PSScriptRoot '.build')
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# This script never installs files or writes to either reference directory.
$managed = (Resolve-Path -LiteralPath $GameManagedPath).Path
$core = (Resolve-Path -LiteralPath $BepInExCorePath).Path
$compiler = (Resolve-Path -LiteralPath $CompilerPath).Path
$expectedGame = '40279b7fda17867f4cfc3e3be0e3bbae7df20bdb5b3b3c9fde0a2827d9862838'
$expectedBep = '8255b28902886085c578b9e427d3073c97002db85176d2090cdeda90ef14ce70'
$gameAssembly = Join-Path $managed 'Assembly-CSharp.dll'
$bepAssembly = Join-Path $core 'BepInEx.dll'
$gameHash = (Get-FileHash -LiteralPath $gameAssembly -Algorithm SHA256).Hash.ToLowerInvariant()
$bepHash = (Get-FileHash -LiteralPath $bepAssembly -Algorithm SHA256).Hash.ToLowerInvariant()
if ($gameHash -ne $expectedGame) { throw "Unsupported game assembly hash: $gameHash" }
if ($bepHash -ne $expectedBep) { throw "Expected official BepInEx 5.4.23.5 core; got hash: $bepHash" }

$references = @('mscorlib.dll', 'System.dll', 'System.Core.dll',
    'UnityEngine.dll', 'UnityEngine.CoreModule.dll', 'UnityEngine.InputLegacyModule.dll',
    'UnityEngine.IMGUIModule.dll', 'UnityEngine.TextRenderingModule.dll', 'UnityEngine.UnityWebRequestModule.dll', 'UnityEngine.UI.dll', 'Assembly-CSharp.dll') |
    ForEach-Object { Join-Path $managed $_ }
$references += $bepAssembly
foreach ($reference in $references) {
    if (!(Test-Path -LiteralPath $reference -PathType Leaf)) { throw "Missing reference: $reference" }
}

$build = [IO.Path]::GetFullPath($BuildDirectory)
$destination = [IO.Path]::GetFullPath($PSScriptRoot)
foreach ($target in @($build, $destination)) {
    foreach ($readOnlyPath in @($managed, $core)) {
        if ($target.Equals($readOnlyPath, [StringComparison]::OrdinalIgnoreCase) -or
            $target.StartsWith($readOnlyPath.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Output must not be inside a reference directory.'
        }
    }
    # Guard the known installation root as well, including sibling output paths.
    $steamGame = 'C:\Program Files (x86)\Steam\steamapps\common\Green Hell'
    if ($target.Equals($steamGame, [StringComparison]::OrdinalIgnoreCase) -or
        $target.StartsWith($steamGame + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'This build script does not write into the Steam game installation.'
    }
}
[void](New-Item -ItemType Directory -Path $build -Force)
$temporaryDll = Join-Path $build 'GreenHellAI.dll'
$arguments = @('/nologo', '/noconfig', '/nostdlib+', '/target:library', '/platform:x64',
    '/langversion:5', '/optimize+', '/debug-', '/warn:4', '/warnaserror+', "/out:$temporaryDll")
$arguments += $references | ForEach-Object { '/reference:' + $_ }
$sourcePaths = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' -File | Sort-Object Name)
$arguments += $sourcePaths | ForEach-Object { $_.FullName }

$compilerOutput = @(& $compiler @arguments 2>&1)
$compilerExit = $LASTEXITCODE
$compilerOutput | Set-Content -LiteralPath (Join-Path $build 'compiler.log') -Encoding UTF8
$compilerOutput | ForEach-Object { Write-Host $_ }
if ($compilerExit -ne 0) { throw "Compiler failed ($compilerExit); see $build\compiler.log" }

$dll = Join-Path $destination 'GreenHellAI.dll'
Copy-Item -LiteralPath $temporaryDll -Destination $dll -Force
$manifest = [ordered]@{
    plugin = 'GreenHellAI'; version = '0.3.0'; process = 'GH.exe'; platform = 'x64'
    buildUtc = [DateTime]::UtcNow.ToString('O')
    compilerVersion = (Get-Item -LiteralPath $compiler).VersionInfo.FileVersion
    compilerExitCode = $compilerExit; warningsAsErrors = $true; language = 'C# 5'
    bepinexRelease = '5.4.23.5'; bepinexCoreSha256 = $bepHash
    gameAssemblySha256 = $gameHash
    outputSha256 = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash.ToLowerInvariant()
    sources = $sourcePaths | ForEach-Object {
        [ordered]@{ path = ('src/' + $_.Name); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
    }
    phase = 3; phase1Runtime = 'User reported pass; read-only inspection of log confirmed four samples.'
    runtimeTested = $false; installedInGame = $false
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $destination 'BUILD-MANIFEST.json') -Encoding UTF8
Write-Host "Compiled: $dll"
Write-Host 'NOT installed or runtime tested. Manual in-game testing remains required.'
