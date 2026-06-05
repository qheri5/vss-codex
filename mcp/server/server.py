"""
vss MCP server — A1 scaffold (console/SSH bridge to the live Vintage Story server).

STUB: tools are registered with their real signatures but return "not implemented yet". Flesh out
the SSH + console-FIFO + `docker logs` integration here (see ../README.md). Safety: this talks to the
VS *console*, never a host shell; never reboot weavy (Plex is in prod).

Run:  python server.py   (stdio MCP; configure via ../mcp.config.example.json)
Deps: pip install -r requirements.txt
"""
from __future__ import annotations

import os

from mcp.server.fastmcp import FastMCP

mcp = FastMCP("vss")

# Configuration via env (see mcp.config.example.json)
SSH_HOST = os.environ.get("VSS_SSH_HOST", "weavy@192.168.1.80")
CONTAINER = os.environ.get("VSS_CONTAINER", "vsserver-dev")

_NYI = "not implemented yet — this is the vss-codex MCP scaffold (see vss-codex/mcp/README.md)"


@mcp.tool()
def vss_run_command(command: str) -> str:
    """Run a single Vintage Story *console* command on the live server (e.g. '/vssanchor status',
    '/stats', '/entity'). Returns the console output. Refuses host-level shell commands."""
    return _NYI


@mcp.tool()
def vss_tail_log(lines: int = 100) -> str:
    """Return the last `lines` lines of the vsserver-dev container log (docker logs --tail)."""
    return _NYI


@mcp.tool()
def vss_deploy_mod(mod_dir: str = "") -> str:
    """Build a mod on the PC, push it, and restart the vsserver-dev container — wraps
    vintage-story-server/scripts/deploy-mod.ps1. Empty mod_dir = the default mod."""
    return _NYI


@mcp.tool()
def vss_start_bench(bench: str = "anchor") -> str:
    """Start a load benchmark on weavy ('anchor' -> vs-anchor-bench.sh, 'entity' -> vs-loadtest.sh)
    and return the measured results."""
    return _NYI


if __name__ == "__main__":
    mcp.run()
