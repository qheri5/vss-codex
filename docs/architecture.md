# Architecture

`vss-codex` is a single cross-platform .NET tool (`src/VssCodex/`, run with `dotnet run`). It has
three cooperating parts: the **orchestrator** runs the pipeline; the **generator** extracts the data;
the **skill** is the consumable output. No PowerShell — it runs identically on Windows, Linux, macOS.

## 1. Generator — `src/VssCodex/` (C#, Mono.Cecil)

Reflects over the local VS binaries (no decompiled-text parsing) to emit markdown. One file = one role:

| File | Role |
|---|---|
| `Generator.cs` | drives the passes below; writes `build-info.json` |
| `CecilContext.cs` | Mono.Cecil reader + cross-assembly resolver (install + Lib + Mods) |
| `DocId.cs` | XML doc-comment IDs (`T:`/`M:`/`P:`…) to match `VintagestoryAPI.xml` |
| `XmlDocIndex.cs` | parse the XML, flatten `<summary>` |
| `InheritDocResolver.cs` | borrow summaries from base types/interfaces (`*(inherited)*`) |
| `SignatureFormatter.cs` | readable C# signatures; nested-type qualification; `[Obsolete]` |
| `ApiReferenceGenerator.cs` | Doc A: API surface (also engine-internal mode for `lib/`) |
| `EventsEnumsGenerator.cs` | `events.md` + `enums.md` |
| `HarmonyTargetGenerator.cs` | Doc B: ✓/✗ patchable catalog per assembly |
| `SymbolSnapshot.cs` + `ChangelogGenerator.cs` | version fingerprint + inter-version diff |
| `MarkdownWriter.cs` | banners, anchors, writing |
| `BuildInfo.cs` | the version+counts handoff to the skill renderer |

`Generator.Run(install, genRoot)` is **pure**: same inputs → same outputs.

## 2. Orchestrator — `Program.cs` + `Pipeline.cs` (C#)

`Program.cs` parses `--install` / `--zip` / `--out` / `--skip-decompile`; `Pipeline.cs` runs the three
steps in-process: **decompile** (`Decompiler.cs` shells out to `ilspycmd`, auto-installed,
crash-isolated), **generate** (`Generator.cs`), **install docs + render skill** (`CuratedDocs.cs` +
`SkillRenderer.cs`). Converter mode (`--zip`) extracts an archive via pure .NET (`ArchiveExtractor.cs`).
**Self-contained output:** everything lands under the gitignored `out/` — the reference in
`out/reference/`, the rendered skill in `out/.claude/skills/vss/`. A top-level try/catch prints a clean
failure and returns non-zero.

## 3. Skill — `skill/` → `out/.claude/skills/vss/`

A lookup skill (Read/Glob/Grep only). `SKILL.md.template` carries `{{PLACEHOLDERS}}` the renderer fills
from `build-info.json` (version, type counts, coverage) and the **absolute reference path** (so the
copied skill still resolves the knowledge base). The `references/` and `examples/` are copied verbatim.
The `skill/` + `docs-src/` sources ship with the tool (copied to its output as content), so a single
`dotnet run` is self-contained. To use the skill, copy `out/.claude/skills/vss/` into a Claude Code
project's `.claude/skills/`.

## Data flow & the committable/proprietary boundary

```
VS install (binaries + .xml)
      │  01 ilspycmd                    02 VssCodex (Mono.Cecil + .xml)
      ▼                                  ▼
out/reference/decompiled/  ──►  out/reference/docs/generated/ + build-info.json
      │                                  │ 03 render skill + copy curated docs
      │                                  ▼
      └────────────────────────►  out/reference/docs/ + out/.claude/skills/vss/
```

- **Committable (this repo):** generator code, formatter, skill source, curated-doc source. Contains
  **zero verbatim decompiled code** — curated docs use `file:line` + prose.
- **Output (gitignored, `out/`):** decompiled code + generated docs + the rendered skill +
  `build-info.json` + `.snapshot-*.json`. Never committed.

## Key design decisions

- **Mono.Cecil over text parsing** — exact signatures + reliable patchability (`HasBody && !abstract
  && !extern`); already shipped in the VS install.
- **API allowlist `Vintagestory.API.*`** — drops bundled OSS / engine namespaces from the API doc.
- **Generator never overwrites `high-value-targets.md`** — it is hand-curated (installed from
  `docs-src/`).
- **Everything version-stamped** — re-run after a VS update; the CHANGELOG tells you what moved.
