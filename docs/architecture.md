# Architecture

`vss-codex` has four cooperating parts. The **generator** extracts data; the **formatter**
orchestrates and installs; the **skill** and **MCP** are what gets installed/used.

## 1. Generator — `src/VssCodex/` (C#, Mono.Cecil)

Reflects over the local VS binaries (no decompiled-text parsing) to emit markdown. One file = one role:

| File | Role |
|---|---|
| `Program.cs` | entry; orchestrates the passes; writes `build-info.json` |
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
| `BuildInfo.cs` | the version+counts handoff to the formatter |

It takes `--install <VS dir> --out <generated dir>` and is **pure**: same inputs → same outputs.

## 2. Formatter — `vss-codex.ps1` + `steps/`

The orchestrator. It resolves the VS install and the workspace container, then runs four steps
(decompile → generate → install docs+skill → scaffold MCP), each an independently-runnable
`steps/NN-*.ps1`. It reads `build-info.json` to render the skill template and print a summary.
**Install-in-place:** output lands in `vs-game-reference/`, the skill in `.claude/skills/vss/`, the
MCP example at the container root.

## 3. Skill — `skill/` → `.claude/skills/vss/`

A lookup skill (Read/Glob/Grep only). `SKILL.md.template` carries `{{PLACEHOLDERS}}` the formatter
fills from `build-info.json` (version, type counts, coverage). `references/` and `examples/` are
copied verbatim. The installed copy is *output*; the source of truth is here.

## 4. MCP — `mcp/`

Scaffold + design doc for an MCP server that drives a live VS server (A1: SSH + console + logs;
A2: telemetry mod). Stub tools today. See `mcp/README.md`.

## Data flow & the committable/proprietary boundary

```
VS install (binaries + .xml)
      │  01 ilspycmd                    02 VssCodex (Mono.Cecil + .xml)
      ▼                                  ▼
vs-game-reference/decompiled/  ──►  vs-game-reference/docs/generated/ + build-info.json
      │                                  │ 03 render template + copy curated docs
      │                                  ▼
      └────────────────────────►  vs-game-reference/docs/ + .claude/skills/vss/
```

- **Committable (this repo):** generator code, formatter, skill source, curated-doc source, MCP.
  Contains **zero verbatim decompiled code** — curated docs use `file:line` + prose.
- **Proprietary (gitignored, `../vs-game-reference/`):** decompiled code + generated docs +
  `build-info.json` + `.snapshot-*.json`. Never committed.

## Key design decisions

- **Mono.Cecil over text parsing** — exact signatures + reliable patchability (`HasBody && !abstract
  && !extern`); already shipped in the VS install.
- **API allowlist `Vintagestory.API.*`** — drops bundled OSS / engine namespaces from the API doc.
- **Generator never overwrites `high-value-targets.md`** — it is hand-curated (installed from
  `docs-src/`).
- **Everything version-stamped** — re-run after a VS update; the CHANGELOG tells you what moved.
