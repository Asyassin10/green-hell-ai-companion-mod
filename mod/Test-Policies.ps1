[CmdletBinding()]
param(
    [string]$CompilerPath = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    [string]$BuildDirectory = (Join-Path $PSScriptRoot '.policy-tests')
)
$ErrorActionPreference = 'Stop'
$testBuild = [IO.Path]::GetFullPath($BuildDirectory)
$gameRoot = 'C:\Program Files (x86)\Steam\steamapps\common\Green Hell'
if ($testBuild.Equals($gameRoot, [StringComparison]::OrdinalIgnoreCase) -or
    $testBuild.StartsWith($gameRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Tests must remain outside the game installation.'
}
[void](New-Item -ItemType Directory -Path $testBuild -Force)
$exe = Join-Path $testBuild 'PolicyTests.exe'
$sources = @('src\FocusLease.cs', 'src\ChatHistory.cs', 'src\Snapshot.cs', 'src\MarkdownRichText.cs', 'tests\PolicyTests.cs') |
    ForEach-Object { Join-Path $PSScriptRoot $_ }
& $CompilerPath /nologo /target:exe /langversion:5 /warn:4 /warnaserror+ "/out:$exe" @sources
if ($LASTEXITCODE -ne 0) { throw 'Policy test compilation failed.' }
$result = @(& $exe 2>&1)
$testExit = $LASTEXITCODE
$result | ForEach-Object { Write-Host $_ }
if ($testExit -ne 0) { throw 'Policy tests failed.' }
$result | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'POLICY-TEST-RESULT.txt') -Encoding UTF8
