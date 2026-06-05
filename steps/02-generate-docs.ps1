#requires -Version 5.1
<#
.SYNOPSIS  Step 02 - build + run the VssCodex generator.
.DESCRIPTION
    Emits the API reference, events/enums indexes, engine-internal lib/ surface, the Harmony catalog,
    the version-diff CHANGELOG, and build-info.json into the reference root's docs/generated/.
    Output is derived from proprietary binaries - the path is forced under the reference root (gitignored).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Install,
    [Parameter(Mandatory)][string]$Ref,
    [Parameter(Mandatory)][string]$RepoRoot
)
$ErrorActionPreference = 'Stop'

$proj = Join-Path $RepoRoot 'src\VssCodex\VssCodex.csproj'
$out  = Join-Path $Ref 'docs\generated'
if (-not (Test-Path $proj)) { throw "generator project not found: $proj" }
# The output must live under the reference root (default: the gitignored out/ tree), never elsewhere.
if ($out -notmatch '[\\/]reference[\\/]') { throw "refusing to write generated docs outside the reference root ($out)" }

Write-Host "  building VssCodex ..."
dotnet build $proj -c Release --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw "generator build failed." }

Write-Host "  generating ..."
# Merge stderr into stdout so an informational stderr line can't become a terminating PS error;
# trust the process exit code instead.
$eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
dotnet run --project $proj -c Release --no-build -- --install $Install --out $out 2>&1 | ForEach-Object { "$_" }
$code = $LASTEXITCODE
$ErrorActionPreference = $eap
if ($code -ne 0) { throw "generation failed (exit $code)." }
