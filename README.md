# vss-codex

[![CI](https://github.com/qheri5/vss-codex/actions/workflows/ci.yml/badge.svg)](https://github.com/qheri5/vss-codex/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

**A complete, always-current reference for modding Vintage Story — generated straight from the game's
own binaries.**

Vintage Story is closed-source: the whole modding API, the engine internals, and every method you
might hook live inside compiled C# DLLs. `vss-codex` decompiles them and turns them into a browsable
knowledge base — the full API, an events/enums index, the engine internals, and a catalog of every
Harmony-patchable method — for whatever game version you point it at. One command; re-run it on each
update.

The result is markdown you can read directly, browse as an ordered, searchable local site, or hand to
an AI assistant as a Claude Code skill so it answers about the real API instead of guessing.

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
                 dotnet run --project src/VssCodex   (the vss-codex CLI)
                        │
  ┌──────────┬─────────┼───────────────┐
  ▼          ▼         ▼                ▼
01 decompile  02 generate            03 install docs + render skill
ilspycmd      Mono.Cecil → markdown   curated notes + the vss skill
  │              │                       │
  ▼              ▼                       ▼
out/reference/decompiled/   …/docs/generated/   out/.claude/skills/vss/
```

1. **Decompile** the Vintage Story assemblies with `ilspycmd` (run as a subprocess, auto-installed).
2. **Generate** the docs from the binaries with Mono.Cecil (no parsing of decompiled text): the API
   reference, events/enums indexes, an engine-internals surface, the Harmony patchability catalog,
   and a version-diff CHANGELOG.
3. **Install** the curated notes and render the `vss` skill.
4. **Build** an ordered, searchable site from those docs with MkDocs + Material (run as a subprocess).
   Optional: skipped with a note if Python 3 isn't available, or with `--no-site`.

One cross-platform process (no PowerShell), idempotent — rebuild against a new game version any time.

## Quickstart

Nothing to configure: it defaults the game install to `%APPDATA%\Vintagestory` (on Windows) and writes
everything into a gitignored `out/` folder. Override any of that with a flag.

```bash
# Build everything from your local Vintage Story install
dotnet run --project src/VssCodex

# Converter mode: hand it a downloaded server/client archive (.zip or .tar.gz) and it does the rest
dotnet run --project src/VssCodex -- --zip <path-to-vs-archive>

# Use a non-default game install, or a custom output dir
dotnet run --project src/VssCodex -- --install <vs-install-dir> --out <output-dir>

# Reuse the existing decompiled tree (skip step 01) for fast doc/skill iteration
dotnet run --project src/VssCodex -- --skip-decompile

# Skip the browsable site (e.g. on a box without Python)
dotnet run --project src/VssCodex -- --no-site
```

**Converter mode** (`--zip`) is the zero-setup path: download an official server build (e.g. from
`https://cdn.vintagestory.at/gamefiles/stable/`), point `--zip` at it, and `vss-codex` extracts it
(pure .NET), locates the binaries, and runs the whole pipeline. Server-only archives simply lack the
client-only assemblies; those are skipped cleanly.

**Requirements:** the .NET 10 SDK (`dotnet`) and `ilspycmd` (auto-installed as a global tool). You need
either a local VS install or a VS archive — the binaries and `VintagestoryAPI.xml` are read directly.
The browsable site additionally needs **Python 3** (with `venv`; on Debian/Ubuntu install
`python3-venv`); `mkdocs-material` is auto-installed into a cached per-user virtualenv on first run.
Without Python the site is skipped gracefully — everything else still builds — or opt out with `--no-site`.
**Runs on Windows, Linux, and macOS** — there's no PowerShell or shell-specific code. On Linux/macOS,
point it at the game with `--install`/`--zip` or the `VINTAGE_STORY` environment variable (on Windows it
defaults to `%APPDATA%\Vintagestory`). Tested end-to-end across Vintage Story **1.20–1.22**.

**Prefer a prebuilt binary?** Each [release](https://github.com/qheri5/vss-codex/releases) ships a
self-contained executable for Windows, Linux, and macOS — no .NET SDK needed. Unzip and run, e.g.
`./vss-codex --zip <vs-archive>`.

## What it produces

Everything lands in `out/` (gitignored — it's derived from the game's binaries, so it's never
committed):

```
out/
├── reference/                  the knowledge base
│   ├── decompiled/             the VS-authored assemblies (ILSpy output)
│   ├── docs/
│   │   ├── README.md, entity-simulation.md   curated notes
│   │   └── generated/          api/ (endpoints + events.md + enums.md + lib/), harmony/
│   │                           (✓/✗ patchable catalog + curated hotspots), CHANGELOG-*.md
│   └── site/                   the same docs as an ordered, searchable MkDocs site (run serve-docs.cmd/.sh)
└── .claude/skills/vss/         the rendered Claude Code skill
```

### Browsing the site

> **To open the browsable docs, double-click `serve-docs.cmd` (Windows) or `serve-docs.sh`
> (Linux/macOS) in `out/reference/site/`.** It serves the folder and opens it at
> <http://localhost:8000>.

Don't open `index.html` directly: Material's **search** loads its index over HTTP and can't run from a
`file://` page, so opening the file directly shows the pages but no working search. `serve-docs.*` is
the one-click way to get the full experience.

### Using the skill

The skill is emitted as a ready-to-use folder at `out/.claude/skills/vss/`, with the absolute path to
the knowledge base injected so it keeps working wherever it lives. To use it in Claude Code, copy that
`vss` folder into your project's `.claude/skills/` (or `~/.claude/skills/`); then ask the assistant
about the VS API and it will consult the reference.

## What's in this repo

| Path | Role |
|---|---|
| `src/VssCodex/` | the **tool** (C#) — CLI + orchestrator (`Program.cs`, `Pipeline.cs`) + generator (Mono.Cecil → markdown) |
| `skill/` | the **skill source** (`SKILL.md.template` + references + examples) |
| `docs-src/` | curated docs source (prose + `file:line`, no verbatim decompiled code) |
| `docs/` | this project's own documentation |
| `tests/VssCodex.Tests/` | xUnit unit tests for the generator |

## Tests & error handling

```bash
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

## License

The tool's source code is [MIT](LICENSE) licensed and contains no game code. The reference it
**generates** is derived from proprietary Vintage Story binaries — keep it local and do not
redistribute it; you need a legally obtained copy of the game. See [`NOTICE`](NOTICE) for details.
