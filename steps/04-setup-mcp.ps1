#requires -Version 5.1
<#
.SYNOPSIS  Step 04 - scaffold the MCP and surface its registration config (non-invasive).
.DESCRIPTION
    The MCP server source lives in this repo (mcp/server, a stub). This step does NOT auto-register it
    with Claude Code (that would edit settings) - it just confirms the scaffold and drops a registration
    example at the container root so the user can enable it. See mcp/README.md for the design + how to
    flesh out the live-server integration.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$RepoRoot,
    [Parameter(Mandatory)][string]$Container
)
$ErrorActionPreference = 'Stop'

$mcp = Join-Path $RepoRoot 'mcp'
if (-not (Test-Path (Join-Path $mcp 'server'))) { Write-Warning "  mcp/server scaffold missing - skipping"; return }

$example = Join-Path $mcp 'mcp.config.example.json'
if (Test-Path $example) {
    Copy-Item $example (Join-Path $Container '.mcp.json.example') -Force
    Write-Host "  MCP registration example -> $(Join-Path $Container '.mcp.json.example')"
}
Write-Host "  MCP scaffold ready at $mcp (stub tools). Enable: see mcp/README.md - not auto-registered."
