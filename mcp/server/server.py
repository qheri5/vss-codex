"""
vss MCP server — A1 scaffold (console/SSH bridge to a live Vintage Story server).

STUB: tools are registered with their real signatures but return "not implemented yet". Flesh out
the SSH + console + `docker logs` integration here (see ../README.md). Safety: this talks to the VS
*console*, never a host shell; never reboot the host (restart only the VS container).

Run:  python server.py   (stdio MCP; configure via ../mcp.config.example.json)
Deps: pip install -r requirements.txt
"""
from __future__ import annotations

import os

from mcp.server.fastmcp import FastMCP

mcp = FastMCP("vss")

# Configuration via env (see mcp.config.example.json)
SSH_HOST = os.environ.get("VSS_SSH_HOST", "user@your-vs-host")
CONTAINER = os.environ.get("VSS_CONTAINER", "your-vs-container")

_NYI = "not implemented yet - this is the vss-codex MCP scaffold (see mcp/README.md)"


@mcp.tool()
def vss_run_command(command: str) -> str:
    """Run a single Vintage Story *console* command on the live server (e.g. '/stats', '/entity').
    Returns the console output. Refuses host-level shell commands."""
    return _NYI


@mcp.tool()
def vss_tail_log(lines: int = 100) -> str:
    """Return the last `lines` lines of the VS server container log (docker logs --tail)."""
    return _NYI


@mcp.tool()
def vss_deploy_mod(mod_dir: str = "") -> str:
    """Build a mod, push it to the server, and restart the VS container. Empty mod_dir = the default mod."""
    return _NYI


@mcp.tool()
def vss_start_bench(bench: str = "default") -> str:
    """Start a load-benchmark script on the server and return the measured results."""
    return _NYI


if __name__ == "__main__":
    mcp.run()
