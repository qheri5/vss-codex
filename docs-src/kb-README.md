# VS understanding base — index

Distilled, hand-written notes on how Vintage Story actually works, backed by the
decompiled source in `../decompiled/`. Consult this **before** diving into 4000 files.
Each doc cites `assembly/path:line` so you can jump to the source on demand.

When you learn something non-obvious about the engine while working, **add it here** —
this is a growing skill-like reference for the engine.

## Docs

### Hand-written
- [entity-simulation.md](entity-simulation.md) — server tick loop, the **"entity active"**
  predicate (`State == Active` gates AI/pathfinding), `AlwaysActive` escape hatch, and the
  player-gated spawner. Foundation for headless entity/AI load benchmarking.

### Auto-generated (`generated/`, via `vss-codex`)
- [generated/api/INDEX.md](generated/api/INDEX.md) — **API endpoints reference**: the public modding
  surface of `VintagestoryAPI.dll` (~1,100 types in 12 `Vintagestory.API.*` namespaces; exact count in
  `build-info.json`), with official
  `<summary>` text merged from `VintagestoryAPI.xml` and inherited from base types/interfaces. One
  file per namespace. Constructors and `[Obsolete]` markers included.
- [generated/api/events.md](generated/api/events.md) — **events & delegates** catalog (the
  event-driven modding surface, consolidated).
- [generated/api/enums.md](generated/api/enums.md) — **enums index** (every public enum + values).
- [generated/api/lib/INDEX.md](generated/api/lib/INDEX.md) — **engine internals** (`VintagestoryLib`
  public types: `ServerMain`, `PhysicsManager`, `ServerSystem*`…). Engine-internal/unstable,
  signatures only (no official docs).
- [generated/harmony/INDEX.md](generated/harmony/INDEX.md) — **Harmony catalog**: every method in the
  6 server-relevant VS-authored assemblies flagged ✓/✗ patchable (one file per assembly), plus the
  hand-curated [high-value-targets.md](generated/harmony/high-value-targets.md) of server-perf hotspots.
- `generated/CHANGELOG-<old>-to-<new>.md` — **version diff** (added/removed members, flipped
  patchability), written after a VS update by comparing the new run to the prior `.snapshot-*.json`.

> **Generated vs hand-written:** the generator rebuilds everything under `generated/` **except**
> `high-value-targets.md`. That one is hand-maintained at its source — `vss-codex/docs-src/high-value-targets.md`
> — and reinstalled here on each run; edit the source, not this copy. Don't hand-edit `INDEX.md` /
> `patchable-*.md` / `api/*` either (regenerated every run).
>
> **Structure (deviations from the original plan):** Doc A is **one file per namespace** (not split
> by category) and Doc B is **one `patchable-<assembly>.md` per assembly** (not a single file) —
> both chosen for Grep-friendliness and to keep individual files small (`VintagestoryLib` alone =
> 14011 methods).

## Map of the most relevant source

| System | Where |
|--------|-------|
| Server tick / systems | `decompiled/VintagestoryLib/Vintagestory.Server/ServerMain.cs`, `ServerSystem*.cs` |
| Entity tick + State | `decompiled/VintagestoryAPI/Vintagestory.API.Common.Entities/Entity.cs` |
| Player-distance → State | `decompiled/VintagestoryLib/Vintagestory.Server/PhysicsManager.cs` |
| Mob spawning | `decompiled/VintagestoryLib/Vintagestory.Server/ServerSystemEntitySpawner.cs` |
| AI tasks / pathfinding | `decompiled/VSEssentials/Vintagestory.GameContent/AiTaskManager.cs`, `EntityBehaviorTaskAI.cs`, `Vintagestory.Essentials/` (WaypointsTraverser, A*) |
| Chunk load / world | `decompiled/VintagestoryLib/Vintagestory.Server/` (chunk/supply systems), `VintagestoryAPI/Vintagestory.API.Server/IWorldManagerAPI.cs` |
| Modding API entry points | `decompiled/VintagestoryAPI/Vintagestory.API.Common/ModSystem.cs`, `ICoreServerAPI.cs` |
