# Pipeline

`vss-codex` runs three steps in one cross-platform process (`Pipeline.cs`). All output goes under the
gitignored `out/` folder.

| # | Step (`src/VssCodex/…`) | Input | Output | Notes |
|---|---|---|---|---|
| 01 | `Decompiler.cs` | VS binaries (the VS-authored DLLs) | `out/reference/decompiled/<asm>/` | runs `ilspycmd` (auto-installed) as a subprocess; ~minutes; skip with `--skip-decompile` |
| 02 | `Generator.cs` | the VS DLLs + `VintagestoryAPI.xml` | `out/reference/docs/generated/` + `build-info.json` | reads metadata via Mono.Cecil; path forced under the reference root |
| 03 | `CuratedDocs.cs` + `SkillRenderer.cs` | `docs-src/`, `skill/`, `build-info.json` | `out/reference/docs/` + `out/.claude/skills/vss/` | copies curated docs; renders the skill with the absolute reference path injected |

## Inputs

- **VS install** (`--install`, default `%APPDATA%\Vintagestory` on Windows): the `.dll`s +
  `VintagestoryAPI.xml`. Or `--zip <archive>` for converter mode (extracted via pure .NET).
- **Output** (`--out`, default `./out`): the gitignored folder everything is written to.

## Re-run cadence

Re-run the **whole** pipeline after every VS update so the reference + skill track the deployed build:

```
dotnet run --project src/VssCodex                       # full: decompile + generate + install
dotnet run --project src/VssCodex -- --skip-decompile   # docs/skill only (reuse the decompiled tree)
```

After a version change, step 02 writes a `CHANGELOG-<old>-to-<new>.md` (diff of API surface +
patchability) by comparing to the previous run's `.snapshot-*.json` — your checklist for re-verifying
`high-value-targets.md` line numbers.

## Safety & robustness

- The generated-docs path must be under the reference root (step 02 refuses otherwise).
- `out/` is gitignored, so the proprietary output can never be committed.
- The skill is written as UTF-8 **without** a BOM (a leading BOM can break YAML frontmatter).
- `ilspycmd` runs as a **subprocess** so a decompiler crash on a pathological assembly stays isolated
  (the tool tolerates a non-zero exit and keeps the partial output — the generated docs don't depend
  on the decompiled `.cs`, only on the DLL metadata).
