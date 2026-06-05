#requires -Version 5.1
<#
.SYNOPSIS  Step 01 - decompile the 10 VS-authored assemblies with ilspycmd.
.DESCRIPTION
    Produces ../vs-game-reference/decompiled/<assembly>/ (compilable projects). Third-party OSS in
    Lib/ is deliberately NOT decompiled (documented upstream). Output is proprietary - gitignored.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Install,
    [Parameter(Mandatory)][string]$Ref
)
$ErrorActionPreference = 'Stop'

# Ensure ilspycmd (global dotnet tool) is available on PATH for this session.
$toolsDir = Join-Path $env:USERPROFILE '.dotnet\tools'
if ($env:PATH -notlike "*$toolsDir*") { $env:PATH = "$env:PATH;$toolsDir" }
if (-not (Get-Command ilspycmd -ErrorAction SilentlyContinue)) {
    Write-Host "  installing ilspycmd (global dotnet tool)..."
    dotnet tool install --global ilspycmd | Out-Null
}

$dec  = Join-Path $Ref 'decompiled'
New-Item -ItemType Directory -Force $dec | Out-Null

# Core + client/tooling assemblies live in the install root; first-party content mods in Mods/.
$core = 'VintagestoryAPI','VintagestoryLib','VintagestoryServer','Vintagestory','VSCrashReporter','VSCrashReporterLib','ModMaker'
$mods = 'VSEssentials','VSSurvivalMod','VSCreativeMod'
$partial = @()
$total = $core.Count + $mods.Count
$script:idx = 0

function Invoke-Decompile([string]$Name, [string]$Dll) {
    $script:idx++
    if (-not (Test-Path $Dll)) { Write-Warning "  [$script:idx/$total] missing $Dll - skipping"; return }
    $target = Join-Path $dec $Name
    Remove-Item -Recurse -Force $target -ErrorAction SilentlyContinue   # clean for a deterministic rebuild
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    Write-Host ("  [$script:idx/$total] decompiling $Name ...") -NoNewline
    # ilspycmd can hit a native stack overflow on a few pathological types (e.g. VSCreativeMod). It still
    # emits most files. Our GENERATED docs read the DLLs via Mono.Cecil, not these .cs, so a partial
    # decompile only affects human browsing - tolerate it and keep going instead of failing the pipeline.
    # Run via cmd so a native crash (STATUS_STACK_OVERFLOW) cannot become a terminating PowerShell error.
    cmd /c "ilspycmd -p -o `"$target`" `"$Dll`" 2>nul 1>nul"
    $nfiles = (Get-ChildItem $target -Recurse -Filter *.cs -ErrorAction SilentlyContinue | Measure-Object).Count
    if ($LASTEXITCODE -ne 0) {
        $script:partial += $Name
        Write-Host (" PARTIAL ({0} files, exit {1}) {2:n1}s" -f $nfiles, $LASTEXITCODE, $sw.Elapsed.TotalSeconds) -ForegroundColor DarkYellow
    } else {
        Write-Host (" {0} files, {1:n1}s" -f $nfiles, $sw.Elapsed.TotalSeconds) -ForegroundColor DarkGray
    }
}

foreach ($a in $core) { Invoke-Decompile $a (Join-Path $Install "$a.dll") }
foreach ($a in $mods) { Invoke-Decompile $a (Join-Path $Install "Mods\$a.dll") }

if ($partial.Count -gt 0) { Write-Host "  note: partial decompile for: $($partial -join ', ')" -ForegroundColor DarkYellow }
Write-Host "  decompiled -> $dec"
exit 0   # partial decompiles are acceptable; don't abort the formatter
