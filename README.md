# vss-codex

**Point it at any Vintage Story version and get a complete, browsable map of the entire modding
surface — then hand that map to your AI.**

One command turns a Vintage Story install (or a freshly downloaded server archive) into a complete
knowledge base — API docs, a function (Harmony) catalog, and engine internals — then installs a
Claude Code skill and scaffolds an MCP so an AI assistant can use it. The reference it builds
(decompiled code + generated docs) stays **local and gitignored** — only the tooling lives here.

Vintage Story is a closed-source, single-binary C# game: everything is a mod, and the API is large.
Figuring out what's available usually means decompiling DLLs by hand and grepping. `vss-codex` does
that once, structures the result, and keeps it in sync with whatever game version you're targeting.

## Who it's for

The same build serves three audiences — for **client or server** mods and tools, at any level of AI
involvement. Pick the layer that fits how you work:

- **Modders & tool developers** → the **knowledge base** is a searchable reference for the whole API,
  every Harmony-patchable method, the events/enums, and the engine internals. Far faster than
  decompiling and reading thousands of files.
- **People building mods with Claude (or another AI)** → the **skill** drops accurate, version-pinned
  grounding straight into the assistant, so it answers about the real VS API instead of guessing.
- **Developers who want the AI in the loop at runtime** → the **MCP** (a scaffold to flesh out) lets
  the assistant observe and drive a live server — run console commands, read logs, deploy, benchmark.

Everyone gets the same up-to-date reference; you decide how much of it the AI touches.

## How it works

```
                 vss-codex.ps1  (the formatter / orchestrator)
                        │
  ┌──────────┬─────────┼────────────────┬──────────────┐
  ▼          ▼         ▼                 ▼              ▼
01 decompile  02 generate (VssCodex)  03 install      04 scaffold
ilspycmd      Mono.Cecil → markdown   docs + skill    MCP
  │              │                       │              │
  ▼              ▼                       ▼              ▼
vs-game-reference/decompiled/  …/docs/generated/   .claude/skills/vss/   vss-codex/mcp/
```

1. **Decompile** the Vintage Story assemblies with `ilspycmd`.
2. **Generate** the docs from the binaries with Mono.Cecil (no parsing of decompiled text): the API
   reference, events/enums indexes, an engine-internals surface, the Harmony patchability catalog,
   and a version-diff CHANGELOG.
3. **Install** the curated notes and render + install the `vss` skill.
4. **Scaffold** the MCP and drop a registration example.

It re-runs idempotently, so you can rebuild against a new game version any time.

## Quickstart

No paths to configure: the script finds itself (so you can run it from anywhere), defaults the game
install to `%APPDATA%\Vintagestory`, and writes its output next to the repo. Override any of that with
a flag.

```powershell
# Build everything from your local Vintage Story install
./vss-codex.ps1

# Converter mode: hand it a downloaded server/client archive (.zip or .tar.gz) and it does the rest
./vss-codex.ps1 -Zip <path-to-vs-archive>

# Use a non-default game install
./vss-codex.ps1 -Install <vs-install-dir>

# Reuse the existing decompiled tree (skip step 01) for fast doc/skill iteration
./vss-codex.ps1 -SkipDecompile
```

**Converter mode** (`-Zip`) is the zero-setup path: download an official server build (e.g. from
`https://cdn.vintagestory.at/gamefiles/stable/`), point `-Zip` at it, and `vss-codex` extracts it,
locates the binaries, and runs the whole pipeline. Server-only archives simply lack the client-only
assemblies; those are skipped cleanly.

**Requirements:** the .NET 10 SDK (`dotnet`) and `ilspycmd` (auto-installed as a global tool). You
need either a local VS install or a VS archive — the binaries and `VintagestoryAPI.xml` are read
directly. The MCP stub additionally wants Python + the `mcp` package. Tested end-to-end on Vintage
Story **1.20**, **1.21**, and **1.22**.

## What it produces

Everything lands in `../vs-game-reference/` (a sibling of this repo). It's derived from the game's
proprietary binaries, so it's **gitignored and never committed** — only the tooling in this repo is.

- `decompiled/` — the VS-authored assemblies (ILSpy output).
- `docs/generated/api/` — API endpoints (one file per namespace) + `events.md` + `enums.md` + `lib/`
  (engine internals); official + inherited summaries, constructors, and `[Obsolete]` flags.
- `docs/generated/harmony/` — every method flagged ✓/✗ Harmony-patchable (one file per assembly), plus
  a curated `high-value-targets.md`.
- `docs/generated/CHANGELOG-<old>-to-<new>.md` — what changed across a game update.
- `.claude/skills/vss/` — the rendered Claude Code skill (build stats injected).
- `.mcp.json.example` — a registration example for the MCP scaffold.

## What's in this repo

| Path | Role |
|---|---|
| `vss-codex.ps1` + `steps/` | the **formatter** — runs the pipeline and installs everything in place |
| `src/VssCodex/` | the **generator** — Mono.Cecil → markdown (C#) |
| `skill/` | the **skill source** (`SKILL.md.template` + references + examples) |
| `docs-src/` | curated docs source (prose + `file:line`, no verbatim decompiled code) |
| `mcp/` | the **MCP** design doc + stub server |
| `docs/` | this project's own documentation |
| `tests/VssCodex.Tests/` | xUnit unit tests for the generator |

## Tests & error handling

```powershell
dotnet test tests/VssCodex.Tests
```

39 xUnit tests cover the version-sensitive logic: doc-comment id generation (generics, byref, arrays,
nested types, constructors), C# signature rendering, `[Obsolete]` detection, XML-summary flattening,
inherited-summary resolution, Harmony patchability, and the snapshot/CHANGELOG diff. Fixtures are read
back from the test assembly with Mono.Cecil, so the tests need no game binaries.

The generator exits non-zero with a one-line message (no stack trace) on a missing assembly or bad
arguments; the formatter traps any failure, prints a clean banner with the reason, and exits 1.

## Learn more

See [`docs/architecture.md`](docs/architecture.md), [`docs/pipeline.md`](docs/pipeline.md), and
[`docs/knowledge-base-layout.md`](docs/knowledge-base-layout.md), plus
[`mcp/README.md`](mcp/README.md) for the MCP design.
