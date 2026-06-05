# Knowledge-base layout (the produced output)

What `vss-codex.ps1` produces. All of this is **proprietary and gitignored** — it lives in the
workspace container, never in a repo.

```
../vs-game-reference/                 (gitignored: .gitignore = *)
├── README.md                         orientation (installed from docs-src/ref-README.md)
├── decompiled/                       ILSpy output, one folder per VS-authored assembly
│   ├── VintagestoryAPI/  VintagestoryLib/  VintagestoryServer/  Vintagestory/
│   ├── VSEssentials/  VSSurvivalMod/  VSCreativeMod/
│   └── VSCrashReporter/  VSCrashReporterLib/  ModMaker/
└── docs/
    ├── README.md                     index (installed from docs-src/kb-README.md)
    ├── entity-simulation.md          curated mechanism note (installed from docs-src/)
    └── generated/                    ← VssCodex output
        ├── api/
        │   ├── INDEX.md              namespace → file + coverage line
        │   ├── Vintagestory.API.*.md one file per public API namespace
        │   ├── events.md            events + delegates, consolidated
        │   ├── enums.md             all public enums + values
        │   └── lib/                 engine internals (VintagestoryLib public types)
        ├── harmony/
        │   ├── INDEX.md             per-assembly patchability summary + scope note
        │   ├── patchable-<asm>.md   every method ✓/✗ patchable
        │   └── high-value-targets.md curated server-perf hotspots (from docs-src/)
        ├── CHANGELOG-<old>-to-<new>.md  written after a VS update
        ├── build-info.json          version + counts (formatter handoff)
        └── .snapshot-<version>.json  symbol fingerprint for the next diff

../.claude/skills/vss/                 the installed skill (rendered from skill/)
├── SKILL.md                          build stats injected from build-info.json
├── references/  (api-usage, harmony-usage, search-strategy, reference-map)
└── examples/    (harmony-patch-server.cs, event-handler.cs, command-registration.cs)

../.mcp.json.example                   MCP registration example (from mcp/)
```

## Scale (VS 1.22.3)

- 10 decompiled assemblies (~4000 `.cs`).
- API: ~1095 public types in 12 `Vintagestory.API.*` namespaces; 116 events; 141 enums; ~37% of
  types carry an upstream XML summary (more via inheritance).
- Engine internals (`lib/`): ~948 public types.
- Harmony catalog: e.g. `VintagestoryLib` 13535/14011 methods patchable.

These numbers are re-derived each run and written to `build-info.json` (and the skill's build stamp).
