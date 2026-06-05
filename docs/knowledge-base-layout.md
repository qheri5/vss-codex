# Output layout (`out/`)

What `vss-codex.ps1` produces. All of it is **gitignored** — it's derived from proprietary game
binaries and is never committed.

```
out/                                  (gitignored)
├── reference/
│   ├── README.md                     orientation (installed from docs-src/ref-README.md)
│   ├── decompiled/                   ILSpy output, one folder per VS-authored assembly
│   │   ├── VintagestoryAPI/  VintagestoryLib/  VintagestoryServer/  Vintagestory/
│   │   ├── VSEssentials/  VSSurvivalMod/  VSCreativeMod/
│   │   └── VSCrashReporter/  VSCrashReporterLib/  ModMaker/
│   └── docs/
│       ├── README.md                 index (installed from docs-src/kb-README.md)
│       ├── entity-simulation.md      curated mechanism note (installed from docs-src/)
│       └── generated/                ← VssCodex output
│           ├── api/
│           │   ├── INDEX.md           namespace → file + coverage line
│           │   ├── Vintagestory.API.*.md  one file per public API namespace
│           │   ├── events.md          events + delegates, consolidated
│           │   ├── enums.md           all public enums + values
│           │   └── lib/               engine internals (VintagestoryLib public types)
│           ├── harmony/
│           │   ├── INDEX.md           per-assembly patchability summary + scope note
│           │   ├── patchable-<asm>.md every method ✓/✗ patchable
│           │   └── high-value-targets.md  curated server-perf hotspots (from docs-src/)
│           ├── CHANGELOG-<old>-to-<new>.md  written after a VS update
│           ├── build-info.json        version + counts (formatter handoff)
│           └── .snapshot-<version>.json  symbol fingerprint for the next diff
└── .claude/skills/vss/               the rendered skill (copy into a Claude Code project to use it)
    ├── SKILL.md                       build stats + absolute reference path injected
    ├── references/  (api-usage, harmony-usage, search-strategy, reference-map, mod-setup)
    └── examples/    (harmony-patch-server.cs, event-handler.cs, command-registration.cs, content-block.cs)
```

## Scale (per a recent VS build)

- 10 decompiled assemblies (~4000 `.cs`).
- API: ~1100 public types in 12 `Vintagestory.API.*` namespaces; ~115 events; ~140 enums; ~37% of
  types carry an upstream XML summary (more via inheritance).
- Engine internals (`lib/`): ~950 public types.
- Harmony catalog: e.g. `VintagestoryLib` ~13.5k of ~14k methods patchable.

These numbers are re-derived each run and written to `build-info.json` (and the skill's build stamp).
