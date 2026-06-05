# vss MCP — design & scaffold

A Model Context Protocol server that lets Claude drive the **live** Vintage Story dedicated server.
This folder currently ships a **stub** (tools return "not implemented yet") plus the design below.
Flesh out the live integration when you're ready — it is intentionally not wired to weavy yet.

## Why

The knowledge base (built by `vss-codex`) tells Claude how VS *works*. The MCP closes the loop: it
lets Claude *observe and drive the running server* — run console commands, read logs, deploy a mod,
kick off a load bench — turning "I think this patch helps" into "measured on `vsserver-dev`".

## Architecture (phased)

**A1 — console/SSH bridge (start here, no in-game code).**
A stdio MCP (Python) that SSHes to the server host and drives the VS server **console** (the same
STDIN-FIFO channel the existing `vs-anchor-bench.sh` / `vs-loadtest.sh` use) and reads `docker logs`.
- Host: `weavy@192.168.1.80` (key auth, no password), container `vsserver-dev` (Docker).
- No server-side code → no new attack surface, prod-safe.

**A2 — telemetry mod (later, optional).**
A tiny server-side VS mod exposing a localhost-bound JSON endpoint (`/api/stats|entities|chunks|anchors`)
behind a Bearer token, for structured low-latency reads (live TPS, entity/chunk counts, anchor state).
Only build this if A1's log-scraping is too coarse.

## Tools (this scaffold registers them as stubs)

| Tool | Arch | Does |
|---|---|---|
| `vss_run_command` | A1 | run one VS console command (e.g. `/vssanchor status`, `/stats`) and return output |
| `vss_tail_log` | A1 | last N lines of the server log (`docker logs --tail`) |
| `vss_deploy_mod` | A1 | wrap `vintage-story-server/scripts/deploy-mod.ps1` (build on PC → push → restart on weavy) |
| `vss_start_bench` | A1 | trigger `vs-anchor-bench.sh` / `vs-loadtest.sh` and stream results |
| `vss_get_tps` | A2 | live tick-rate / main-thread time (needs the telemetry mod) |
| `vss_list_entities` / `vss_chunk_stats` | A2 | structured live counts (needs the telemetry mod) |

## Safety (non-negotiable on weavy)

- **Plex runs in production on weavy — never reboot the host.** Restarts are limited to the
  `vsserver-dev` container.
- Scope the SSH key to the Tailscale IP; the MCP only issues whitelisted console commands + reads.
- `vss_run_command` must refuse host-level shell — it talks to the **VS console**, not bash.
- A2's listener binds `localhost` only, token-gated.

## Enable the stub (manual — not auto-registered)

The formatter (`vss-codex.ps1`) drops `.mcp.json.example` at the container root. To try the stub:
1. `pip install -r vss-codex/mcp/server/requirements.txt` (or `uv`).
2. Merge `mcp.config.example.json` into your project `.mcp.json` (or `claude mcp add`).
3. The 4 A1 tools will list and return "not implemented yet" until the live integration is written.

## Next steps to make it live (A1)

Implement in `server/server.py`: shell out to `ssh $VSS_SSH_HOST` to (a) write a command into the
server console FIFO and read the echoed result, and (b) `docker logs --tail N vsserver-dev`. Reuse the
FIFO path/conventions from `vintage-story-server/scripts/vs-anchor-bench.sh`.
