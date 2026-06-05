# vss-codex

**A complete, always-current reference for modding Vintage Story — generated straight from the game's
own binaries.**

Vintage Story is closed-source: the whole modding API, the engine internals, and every method you
might hook live inside compiled C# DLLs. `vss-codex` decompiles them and turns them into a browsable
knowledge base — the full API, an events/enums index, the engine internals, and a catalog of every
Harmony-patchable method — for whatever game version you point it at. One command; re-run it on each
update.

The result is markdown you can read directly, or hand to an AI assistant as a Claude Code skill so it
answers about the real API instead of guessing.

> ## ⚠️ Proprietary game content
> `vss-codex` decompiles Vintage Story's **copyrighted binaries on your own machine**, and the
> reference it produces is **derived from those proprietary game files**. Keep that output **local**:
> don't modify it, and **don't publish or redistribute** it (or any decompiled source). This repository
> ships **no game code** — it is a **development aid** for building mods and tools *for* Vintage Story,
> and nothing more. You need a legally obtained copy of the game to run it.

## Who it's for

- **Modders & tool developers** → the **knowledge base** is a searchable reference for the whole API,
  every Harmony-patchable method, the events/enums, and the engine internals. Far faster than
  decompiling and reading thousands of files by hand.
- **People building mods with Claude (or another AI)** → the generated **skill** drops accurate,
  version-pinned grounding into the assistant, so it works from the real VS API instead of guessing.

It covers **client and server** modding alike, and you can use either layer on its own.

## How it works

```
                 vss-codex.ps1  (the formatter / orchestrator)
                        │
  ┌──────────┬─────────┼───────────────┐
  ▼          ▼         ▼                ▼
01 decompile  02 generate (VssCodex)  03 install docs + render skill
ilspycmd      Mono.Cecil → markdown   curated notes + the vss skill
  │              │                       │
  ▼              ▼                       ▼
out/reference/decompiled/   …/docs/generated/   out/.claude/skills/vss/
```

1. **Decompile** the Vintage Story assemblies with `ilspycmd`.
2. **Generate** the docs from the binaries with Mono.Cecil (no parsing of decompiled text): the API
   reference, events/enums indexes, an engine-internals surface, the Harmony patchability catalog,
   and a version-diff CHANGELOG.
3. **Install** the curated notes and render the `vss` skill.

It re-runs idempotently, so you can rebuild against a new game version any time.

## Quickstart

No paths to configure: the script finds itself (so you can run it from anywhere), defaults the game
install to `%APPDATA%\Vintagestory`, and writes everything into a gitignored `out/` folder next to it.
Override any of that with a flag.

```powershell
# Build everything from your local Vintage Story install
./vss-codex.ps1

# Converter mode: hand it a downloaded server/client archive (.zip or .tar.gz) and it does the rest
./vss-codex.ps1 -Zip <path-to-vs-archive>

# Use a non-default game install, or a custom output dir
./vss-codex.ps1 -Install <vs-install-dir> -Out <output-dir>

# Reuse the existing decompiled tree (skip step 01) for fast doc/skill iteration
./vss-codex.ps1 -SkipDecompile
```

**Converter mode** (`-Zip`) is the zero-setup path: download an official server build (e.g. from
`https://cdn.vintagestory.at/gamefiles/stable/`), point `-Zip` at it, and `vss-codex` extracts it,
locates the binaries, and runs the whole pipeline. Server-only archives simply lack the client-only
assemblies; those are skipped cleanly.

**Requirements:** the .NET 10 SDK (`dotnet`) and `ilspycmd` (auto-installed as a global tool). You
need either a local VS install or a VS archive — the binaries and `VintagestoryAPI.xml` are read
directly. Tested end-to-end on Vintage Story **1.20**, **1.21**, and **1.22**.

## What it produces

Everything lands in `out/` (gitignored — it's derived from the game's binaries, so it's never
committed):

```
out/
├── reference/                  the knowledge base
│   ├── decompiled/             the VS-authored assemblies (ILSpy output)
│   └── docs/
│       ├── README.md, entity-simulation.md   curated notes
│       └── generated/          api/ (endpoints + events.md + enums.md + lib/), harmony/
│                               (✓/✗ patchable catalog + curated hotspots), CHANGELOG-*.md
└── .claude/skills/vss/         the rendered Claude Code skill
```

### Using the skill

The skill is emitted as a ready-to-use folder at `out/.claude/skills/vss/`, with the absolute path to
the knowledge base injected so it keeps working wherever it lives. To use it in Claude Code, copy that
`vss` folder into your project's `.claude/skills/` (or `~/.claude/skills/`); then ask the assistant
about the VS API and it will consult the reference.

## What's in this repo

| Path | Role |
|---|---|
| `vss-codex.ps1` + `steps/` | the **formatter** — runs the pipeline |
| `src/VssCodex/` | the **generator** — Mono.Cecil → markdown (C#) |
| `skill/` | the **skill source** (`SKILL.md.template` + references + examples) |
| `docs-src/` | curated docs source (prose + `file:line`, no verbatim decompiled code) |
| `docs/` | this project's own documentation |
| `tests/VssCodex.Tests/` | xUnit unit tests for the generator |

## Tests & error handling

```powershell
dotnet test tests/VssCodex.Tests
```

The xUnit suite covers the version-sensitive logic: doc-comment id generation (generics, byref,
arrays, nested types, constructors), C# signature rendering, `[Obsolete]` detection, XML-summary
flattening, inherited-summary resolution, Harmony patchability, and the snapshot/CHANGELOG diff.
Fixtures are read back from the test assembly with Mono.Cecil, so the tests need no game binaries.

The generator exits non-zero with a one-line message (no stack trace) on a missing assembly or bad
arguments; the formatter traps any failure, prints a clean banner with the reason, and exits 1.

## Learn more

See [`docs/architecture.md`](docs/architecture.md), [`docs/pipeline.md`](docs/pipeline.md), and
[`docs/knowledge-base-layout.md`](docs/knowledge-base-layout.md).
