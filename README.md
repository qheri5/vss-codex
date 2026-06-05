# vss-codex

**One command turns a Vintage Story install into a complete, browsable knowledge base — API docs,
function (Harmony) catalog, engine internals — and installs a Claude Code skill + an MCP scaffold to
use it.**

`vss-codex` is the single source of the VSS reference *tooling*. The **output** it produces
(decompiled code + generated docs) is proprietary and is written **outside** this repo, into the
gitignored `../vs-game-reference/` tree — it is never committed.

```
                 vss-codex.ps1  (the formatter / orchestrator)
                        │
  ┌─────────┬──────────┼───────────────┬─────────────┐
  ▼         ▼          ▼                ▼             ▼
01 decompile  02 generate (VssCodex)  03 install     04 scaffold
ilspycmd      Mono.Cecil → markdown   docs + skill   MCP
  │              │                       │              │
  ▼              ▼                       ▼              ▼
vs-game-reference/decompiled/   …/docs/generated/   .claude/skills/vss/   vss-codex/mcp/
```

## Quickstart

```powershell
# Build the whole knowledge base + install the skill + scaffold the MCP, from your local VS install
./vss-codex.ps1

# CONVERTER MODE: one downloaded server archive in -> full codex + skill + MCP out (auto-extract + locate)
./vss-codex.ps1 -Zip "C:\Downloads\vs_server_win-x64_1.21.1.zip"

# Faster iteration when the decompiled tree already exists (skip step 01)
./vss-codex.ps1 -SkipDecompile

# Point at a specific VS install
./vss-codex.ps1 -Install "D:\Games\Vintagestory"
```

**Converter mode** (`-Zip`) takes a VS server/client archive (`.zip` or `.tar.gz`, e.g. from
`https://cdn.vintagestory.at/gamefiles/stable/vs_server_win-x64_<version>.zip`), extracts it, finds the
binaries automatically, and runs the whole pipeline — nothing else to set up. Server-only archives lack
the client assemblies (`Vintagestory.dll`, crash reporter, ModMaker); those are skipped cleanly.

Requirements: .NET 10 SDK (`dotnet`), `ilspycmd` (auto-installed as a global tool), and either a local
VS install or a VS archive (the binaries + `VintagestoryAPI.xml` are read directly). Optional for the
MCP stub: Python + `mcp`. Tested end-to-end on VS **1.20**, **1.21**, and **1.22**.

## What it produces (into `../vs-game-reference/`, gitignored)

- `decompiled/` — the 10 VS-authored assemblies (ILSpy).
- `docs/generated/api/` — API endpoints (1 file/namespace) + `events.md` + `enums.md` + `lib/`
  (engine internals); official + inherited summaries, constructors, `[Obsolete]` flags.
- `docs/generated/harmony/` — every method flagged ✓/✗ Harmony-patchable (1 file/assembly) +
  curated `high-value-targets.md`.
- `docs/generated/CHANGELOG-<old>-to-<new>.md` — what changed across a VS update.
- `docs/{README,entity-simulation}.md` — curated hand notes (installed from `docs-src/`).
- `.claude/skills/vss/` — the rendered Claude Code skill (build stats injected).
- `vss-codex/mcp/` — the MCP scaffold + a `.mcp.json.example` at the container root.

## What lives here (committable)

| Path | Role |
|---|---|
| `vss-codex.ps1` + `steps/` | the **formatter** — runs the pipeline, installs in place |
| `src/VssCodex/` | the **generator** — Mono.Cecil → markdown (C#) |
| `skill/` | the **skill source** (`SKILL.md.template` + references + examples) |
| `docs-src/` | curated docs source (prose + `file:line`, **no verbatim decompiled code**) |
| `mcp/` | the **MCP** design doc + stub server |
| `docs/` | this project's own documentation |

## Rules

- **Never commit proprietary output.** Decompiled code + generated docs go only to
  `../vs-game-reference/` (`.gitignore = *`). Committable curated docs reference the decompiled
  source by `file:line` and describe it in prose — they embed no verbatim decompiled code.
- **Re-run after every VS update** so the reference (and the skill's build stamp) track the deployed
  build. The version is stamped into every generated file.
- All artifacts are **English**.

See [`docs/architecture.md`](docs/architecture.md), [`docs/pipeline.md`](docs/pipeline.md), and
[`docs/knowledge-base-layout.md`](docs/knowledge-base-layout.md) for details, and
[`mcp/README.md`](mcp/README.md) for the MCP design.
