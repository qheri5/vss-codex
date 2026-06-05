# Pipeline

`vss-codex.ps1` runs three steps. Each is an independently-runnable script in `steps/` (useful for
debugging a single stage). All output goes under the gitignored `out/` folder.

| # | Step | Input | Output | Notes |
|---|---|---|---|---|
| 01 | `01-decompile.ps1` | VS binaries (the VS-authored DLLs) | `out/reference/decompiled/<asm>/` | ensures `ilspycmd`; ~minutes; skip with `-SkipDecompile` |
| 02 | `02-generate-docs.ps1` | decompiled refs + `VintagestoryAPI.xml` | `out/reference/docs/generated/` + `build-info.json` | builds + runs `VssCodex`; path forced under the reference root |
| 03 | `03-install-docs-skill.ps1` | `docs-src/`, `skill/`, `build-info.json` | `out/reference/docs/` + `out/.claude/skills/vss/` | copies curated docs; renders the skill (UTF-8) with the absolute reference path injected |

## Inputs

- **VS install** (`-Install`, default `%APPDATA%\Vintagestory`): the `.dll`s + `VintagestoryAPI.xml`.
  Or `-Zip <archive>` for converter mode.
- **Output** (`-Out`, default `out/` inside the repo): the gitignored folder everything is written to.

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

- The generated-docs path must be under the reference root (step 02 refuses otherwise).
- `out/` is gitignored, so the proprietary output can never be committed.
- `.ps1` files are ASCII-only (Windows PowerShell 5.1 misreads UTF-8 scripts without a BOM).
- The skill is written as UTF-8 **without** a BOM (a leading BOM can break YAML frontmatter).

## Encoding note (PowerShell 5.1)

When reading UTF-8 files that contain non-ASCII characters (templates, JSON), the steps use
`[System.IO.File]::ReadAllText(path, UTF8)` rather than `Get-Content -Raw` (which defaults to ANSI and
would mangle em-dashes/arrows/✓✗). Markdown is copied byte-for-byte with `Copy-Item`.
