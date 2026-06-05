#requires -Version 5.1
<#
.SYNOPSIS  Step 03 - install curated docs into the knowledge base + render & install the vss skill.
.DESCRIPTION
    Copies the committable curated docs (docs-src/) into ../vs-game-reference/docs/, then renders
    skill/SKILL.md.template with values from build-info.json and installs the skill into
    <container>/.claude/skills/vss/. The skill source lives in this repo; the installed copy is output.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Ref,
    [Parameter(Mandatory)][string]$RepoRoot,
    [Parameter(Mandatory)][string]$Container
)
$ErrorActionPreference = 'Stop'

$docs = Join-Path $Ref 'docs'
$gen  = Join-Path $docs 'generated'
New-Item -ItemType Directory -Force $docs | Out-Null
New-Item -ItemType Directory -Force (Join-Path $gen 'harmony') | Out-Null

# 1) curated docs -> the knowledge base
Copy-Item (Join-Path $RepoRoot 'docs-src\ref-README.md')        (Join-Path $Ref  'README.md')               -Force
Copy-Item (Join-Path $RepoRoot 'docs-src\kb-README.md')         (Join-Path $docs 'README.md')               -Force
Copy-Item (Join-Path $RepoRoot 'docs-src\entity-simulation.md') (Join-Path $docs 'entity-simulation.md')    -Force
Copy-Item (Join-Path $RepoRoot 'docs-src\high-value-targets.md') (Join-Path $gen 'harmony\high-value-targets.md') -Force
Write-Host "  curated docs installed -> $docs"

# 2) render the skill template from build-info.json
# Read as UTF-8 explicitly - PS 5.1's Get-Content defaults to ANSI and would mangle non-ASCII (em-dashes, arrows).
$utf8 = New-Object System.Text.UTF8Encoding $false
$info = [System.IO.File]::ReadAllText((Join-Path $gen 'build-info.json'), $utf8) | ConvertFrom-Json
$tpl  = [System.IO.File]::ReadAllText((Join-Path $RepoRoot 'skill\SKILL.md.template'), $utf8)
$map = @{
    'VS_VERSION'     = $info.VsVersion;     'API_TYPES'   = $info.ApiTypes
    'API_NAMESPACES' = $info.ApiNamespaces; 'COVERAGE_PCT'= $info.CoveragePct
    'EVENTS'         = $info.Events;        'ENUMS'       = $info.Enums
    'LIB_TYPES'      = $info.LibTypes;      'GENERATED_ON'= $info.GeneratedOn
}
foreach ($k in $map.Keys) { $tpl = $tpl -replace "\{\{$k\}\}", [string]$map[$k] }

$skill = Join-Path $Container '.claude\skills\vss'
New-Item -ItemType Directory -Force (Join-Path $skill 'references') | Out-Null
New-Item -ItemType Directory -Force (Join-Path $skill 'examples')   | Out-Null
# SKILL.md without BOM (a leading BOM can break YAML frontmatter detection)
[System.IO.File]::WriteAllText((Join-Path $skill 'SKILL.md'), $tpl, (New-Object System.Text.UTF8Encoding $false))
Copy-Item (Join-Path $RepoRoot 'skill\references\*') (Join-Path $skill 'references') -Recurse -Force
Copy-Item (Join-Path $RepoRoot 'skill\examples\*')   (Join-Path $skill 'examples')   -Recurse -Force
Write-Host "  skill installed -> $skill"
