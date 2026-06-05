# vss MCP — design & scaffold

A Model Context Protocol server that lets an AI assistant drive a **live** Vintage Story dedicated
server. This folder currently ships a **stub** (tools return "not implemented yet") plus the design
below. Flesh out the live integration when you're ready — it is intentionally not wired to any host.

## Why

The knowledge base (built by `vss-codex`) tells the AI how Vintage Story *works*. The MCP closes the
loop: it lets the AI *observe and drive the running server* — run console commands, read logs, deploy
a mod, kick off a load bench — turning "I think this patch helps" into "measured on the server".

## Architecture (phased)

**A1 — console/SSH bridge (start here, no in-game code).**
A stdio MCP (Python) that SSHes to your server host and drives the VS server **console** (via the
server's STDIN channel) and reads `docker logs`.
- Host + container are configured via environment variables (`VSS_SSH_HOST`, `VSS_CONTAINER`).
- No server-side code → no new attack surface.

**A2 — telemetry mod (later, optional).**
A tiny server-side VS mod exposing a localhost-bound JSON endpoint
(`/api/stats|entities|chunks|anchors`) behind a Bearer token, for structured low-latency reads (live
TPS, entity/chunk counts). Only build this if A1's log-scraping is too coarse.

## Tools (this scaffold registers them as stubs)

| Tool | Arch | Does |
|---|---|---|
| `vss_run_command` | A1 | run one VS console command (e.g. `/stats`, `/entity`) and return its output |
| `vss_tail_log` | A1 | last N lines of the server container log (`docker logs --tail`) |
| `vss_deploy_mod` | A1 | build a mod, push it to the server, and restart the VS container |
| `vss_start_bench` | A1 | trigger a load-benchmark script on the server and stream its results |
| `vss_get_tps` | A2 | live tick-rate / main-thread time (needs the telemetry mod) |
| `vss_list_entities` / `vss_chunk_stats` | A2 | structured live counts (needs the telemetry mod) |

## Safety

- **Never reboot the host.** Restarts are limited to the VS server **container**.
- Restrict the SSH key to the server host; the MCP only issues whitelisted console commands + reads.
- `vss_run_command` must refuse host-level shell — it talks to the **VS console**, not bash.
- A2's listener binds `localhost` only, token-gated.

## Enable the stub (manual — not auto-registered)

The formatter (`vss-codex.ps1`) drops `.mcp.json.example` at the container root. To try the stub:
1. `pip install -r mcp/server/requirements.txt` (or use `uv`).
2. Merge `mcp.config.example.json` into your project `.mcp.json` (or `claude mcp add`), setting
   `VSS_SSH_HOST` and `VSS_CONTAINER` for your server.
3. The 4 A1 tools will list and return "not implemented yet" until the live integration is written.

## Next steps to make it live (A1)

Implement in `server/server.py`: shell out to `ssh $VSS_SSH_HOST` to (a) write a command into the VS
server console and read the echoed result, and (b) `docker logs --tail N $VSS_CONTAINER`. A simple
FIFO/stdin pipe into the running container is the usual way to feed the console.
