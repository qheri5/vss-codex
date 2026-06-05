# Pipeline

`vss-codex.ps1` runs four steps. Each is an independently-runnable script in `steps/` (useful for
debugging a single stage). All output goes under the workspace container, never into a git repo.

| # | Step | Input | Output | Notes |
|---|---|---|---|---|
| 01 | `01-decompile.ps1` | VS binaries (10 VS-authored DLLs) | `vs-game-reference/decompiled/<asm>/` | ensures `ilspycmd`; ~minutes; skip with `-SkipDecompile` |
| 02 | `02-generate-docs.ps1` | decompiled refs + `VintagestoryAPI.xml` | `vs-game-reference/docs/generated/` + `build-info.json` | builds + runs `VssCodex`; path forced inside `vs-game-reference` |
| 03 | `03-install-docs-skill.ps1` | `docs-src/`, `skill/`, `build-info.json` | `vs-game-reference/docs/` + `.claude/skills/vss/` | copies curated docs; renders skill template (UTF-8) |
| 04 | `04-setup-mcp.ps1` | `mcp/` | `.mcp.json.example` at container root | non-invasive; does not auto-register |

## Inputs

- **VS install** (`-Install`, default `%APPDATA%\Vintagestory`): the `.dll`s + `VintagestoryAPI.xml`.
- **Container** (`-Container`, default the repo's parent): where `vs-game-reference/` and `.claude/` live.

## Re-run cadence

Re-run the **whole** pipeline after every VS update so the reference + skill track the deployed build:

```powershell
./vss-codex.ps1            # full: decompile + generate + install
```

For iterating on the generator, skill, or curated docs without re-decompiling:

```powershell
./vss-codex.ps1 -SkipDecompile
```

After a version change, step 02 writes a `CHANGELOG-<old>-to-<new>.md` (diff of API surface +
patchability) by comparing to the previous run's `.snapshot-*.json` — your checklist for re-verifying
`high-value-targets.md` line numbers.

## Safety guards

- The output path must be inside `vs-game-reference` (step 02 refuses otherwise).
- The formatter refuses to run if `vs-game-reference/` is a git repo (it must stay un-committable).
- `.ps1` files are ASCII-only (Windows PowerShell 5.1 misreads UTF-8 scripts without a BOM).
- The skill is written as UTF-8 **without** a BOM (a leading BOM can break YAML frontmatter).

## Encoding note (PowerShell 5.1)

When reading UTF-8 files that contain non-ASCII characters (templates, JSON), the steps use
`[System.IO.File]::ReadAllText(path, UTF8)` rather than `Get-Content -Raw` (which defaults to ANSI and
would mangle em-dashes/arrows/✓✗). Markdown is copied byte-for-byte with `Copy-Item`.
