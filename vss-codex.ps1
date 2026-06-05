#requires -Version 5.1
<#
.SYNOPSIS
    vss-codex - build the entire Vintage Story knowledge base from a VS install, and install it.

.DESCRIPTION
    The formatter / orchestrator. From one command it runs the full pipeline:
      01  decompile the 10 VS-authored assemblies          -> vs-game-reference/decompiled/
      02  generate the API + Harmony + events/enums + lib   -> vs-game-reference/docs/generated/
      03  install curated docs + render & install the skill -> vs-game-reference/docs/ + .claude/skills/vss/
      04  scaffold the MCP + emit its registration config
    Idempotent and re-runnable on any VS version. The generator code + skill source + curated docs
    live in THIS repo (committable). The OUTPUT (decompiled code + generated docs) is proprietary and
    is written only into the gitignored ../vs-game-reference/ tree - never committed.

.PARAMETER Install
    Vintage Story install dir. Default %APPDATA%\Vintagestory.
.PARAMETER Zip
    A VS server/client archive (.zip or .tar.gz). Converter mode: it is extracted and the binaries are
    auto-located, then used as the install. One downloaded file in -> full codex + skill + MCP out.
.PARAMETER Container
    Workspace container (where vs-game-reference/ and .claude/ live). Default: this repo's parent.
.PARAMETER SkipDecompile
    Skip step 01 (reuse the existing decompiled tree) - fast iteration on docs/skill.

.EXAMPLE
    ./vss-codex.ps1
.EXAMPLE
    ./vss-codex.ps1 -Install "D:\Games\Vintagestory" -SkipDecompile
.EXAMPLE
    ./vss-codex.ps1 -Zip "C:\Downloads\vs_server_win-x64_1.21.1.zip"   # converter: archive in -> codex out
#>
[CmdletBinding()]
param(
    [string]$Install = "$env:APPDATA\Vintagestory",
    [string]$Zip,
    [string]$Container,
    [switch]$SkipDecompile
)

$ErrorActionPreference = 'Stop'

# Clean failure: print a one-line reason (not a stack trace) and exit non-zero on any terminating error.
trap {
    Write-Host ""
    Write-Host "##############################################################" -ForegroundColor Red
    Write-Host "#  vss-codex FAILED" -ForegroundColor Red
    Write-Host ("#  {0}" -f $_.Exception.Message) -ForegroundColor Red
    Write-Host "##############################################################" -ForegroundColor Red
    exit 1
}

$RepoRoot = $PSScriptRoot
if (-not $Container) { $Container = (Resolve-Path (Join-Path $RepoRoot '..')).Path }
$Ref = Join-Path $Container 'vs-game-reference'

# Converter mode: a VS server/client archive (.zip / .tar.gz) in -> extract, auto-locate the binaries,
# and use that as the install dir. Makes the whole thing end-to-end from a single downloaded file.
function Resolve-InstallFromArchive([string]$Archive) {
    if (-not (Test-Path $Archive)) { throw "archive not found: $Archive" }
    $name = [System.IO.Path]::GetFileNameWithoutExtension($Archive) -replace '\.tar$',''
    $dest = Join-Path $env:TEMP "vss-codex-extract\$name"
    Remove-Item -Recurse -Force $dest -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force $dest | Out-Null
    Write-Host "  extracting $([System.IO.Path]::GetFileName($Archive)) ..." -ForegroundColor DarkCyan
    if ($Archive -match '\.zip$') {
        Expand-Archive -Path $Archive -DestinationPath $dest -Force
    } elseif ($Archive -match '\.(tar\.gz|tgz)$') {
        tar -xzf $Archive -C $dest
        if ($LASTEXITCODE -ne 0) { throw "tar extraction failed for $Archive" }
    } else {
        throw "unsupported archive type (expected .zip or .tar.gz): $Archive"
    }
    # Auto-locate the folder that holds VintagestoryAPI.dll (shallowest match).
    $api = Get-ChildItem $dest -Recurse -Filter 'VintagestoryAPI.dll' -ErrorAction SilentlyContinue |
           Sort-Object { ($_.FullName -split '[\\/]').Count } | Select-Object -First 1
    if (-not $api) { throw "VintagestoryAPI.dll not found inside $Archive - is this a VS server/client archive?" }
    Write-Host "  binaries -> $($api.DirectoryName)" -ForegroundColor DarkCyan
    return $api.DirectoryName
}

if ($Zip) { $Install = Resolve-InstallFromArchive $Zip }
if (-not (Test-Path $Install)) { throw "VS install dir not found: $Install" }
# Safety: the proprietary output tree must NOT be a git repo (it must stay un-committable).
if (Test-Path (Join-Path $Ref '.git')) { throw "vs-game-reference is a git repo - refusing to write proprietary output into it." }

$TotalSteps = if ($SkipDecompile) { 3 } else { 4 }
$script:StepNo = 0
$RunTimer = [System.Diagnostics.Stopwatch]::StartNew()

function Step($msg, [scriptblock]$body) {
    $script:StepNo++
    Write-Host ""
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    & $body
    if ($LASTEXITCODE -ne $null -and $LASTEXITCODE -ne 0) { throw "step $script:StepNo failed (exit $LASTEXITCODE)" }
    Write-Host ("    [OK] step $script:StepNo/$TotalSteps done in {0:n1}s" -f $sw.Elapsed.TotalSeconds) -ForegroundColor Green
}

Write-Host ""
Write-Host "##############################################################" -ForegroundColor Green
Write-Host "#  vss-codex  -  building the Vintage Story knowledge base    #" -ForegroundColor Green
Write-Host "##############################################################" -ForegroundColor Green
Write-Host "  install   : $Install"
Write-Host "  container : $Container"
Write-Host "  reference : $Ref"
Write-Host "  steps     : $TotalSteps$(if($SkipDecompile){' (decompile skipped)'})"

if ($SkipDecompile) {
    Write-Host "`n  (decompile skipped: -SkipDecompile, reusing existing decompiled tree)" -ForegroundColor DarkYellow
} else {
    Step 'decompile the 10 VS-authored assemblies (ilspycmd)' { & "$RepoRoot\steps\01-decompile.ps1" -Install $Install -Ref $Ref }
}
Step 'generate docs: API + events/enums + lib + Harmony + CHANGELOG (VssCodex/Mono.Cecil)' { & "$RepoRoot\steps\02-generate-docs.ps1" -Install $Install -Ref $Ref -RepoRoot $RepoRoot }
Step 'install curated docs + render & install the vss skill'                                { & "$RepoRoot\steps\03-install-docs-skill.ps1" -Ref $Ref -RepoRoot $RepoRoot -Container $Container }
Step 'scaffold the MCP + emit registration config'                                          { & "$RepoRoot\steps\04-setup-mcp.ps1" -RepoRoot $RepoRoot -Container $Container }

# Summary from the generator's handoff
$infoPath = Join-Path $Ref 'docs\generated\build-info.json'
if (Test-Path $infoPath) {
    $i = Get-Content $infoPath -Raw | ConvertFrom-Json
    Write-Host ""
    Write-Host "##############################################################" -ForegroundColor Green
    Write-Host ("#  DONE  -  VS $($i.VsVersion)  -  total {0:n1}s" -f $RunTimer.Elapsed.TotalSeconds) -ForegroundColor Green
    Write-Host "##############################################################" -ForegroundColor Green
    Write-Host "  API     : $($i.ApiTypes) types / $($i.ApiNamespaces) ns, $($i.CoveragePct)% documented"
    Write-Host "  Indexes : $($i.Events) events, $($i.Enums) enums, $($i.LibTypes) engine types"
    Write-Host "  Knowledge base -> $Ref"
    Write-Host "  Skill          -> $(Join-Path $Container '.claude\skills\vss')"
    Write-Host "  MCP scaffold   -> $(Join-Path $RepoRoot 'mcp')  (see mcp/README.md to enable)"
}
