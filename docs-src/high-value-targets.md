# Harmony high-value patch targets — server performance

> **⚠️ PROPRIETARY — HAND-WRITTEN — DO NOT COMMIT.** Lives in the gitignored `vs-game-reference/`
> tree. **Not auto-generated** — the generator (`generate-vs-docs.ps1`) writes `INDEX.md` and
> `patchable-*.md` but never touches this file. Maintain it by hand; re-verify line numbers after a
> VS update (the decompiled tree is the source of truth).

Curated shortlist of the methods that actually move server CPU, for the VSS optimization work
(epic PROLAB-11). The exhaustive machine catalog is in `patchable-*.md`; *how* to patch safely is in
the skill's `references/harmony-usage.md`; the entity-active mechanism is dissected in
`../entity-simulation.md`. Paths are relative to `vs-game-reference/decompiled/`.

**Metric reminder:** the cost that matters is **main-thread tick time** (VS sims on one thread).
Methods tagged *(off-thread)* run on `OnSeparateThreadTick` / worker pools — patching them helps
throughput/contention but does **not** directly shrink the main tick. Measure with `FrameProfiler`
(the engine already brackets most of these with `FrameProfiler.Enter(...)`).

## Tier 1 — entity & AI tick (the primary lever; Synergy ≈ −68% entity tick, PROLAB-14)

| # | Target | Location | Why it's hot | Patch approach | Risk |
|--:|---|---|---|---|---|
| 1 | `ServerSystemEntitySimulation.TickEntities` | `VintagestoryLib/Vintagestory.Server/ServerSystemEntitySimulation.cs:188` | Iterates **every** `LoadedEntities` value and calls `OnGameTick` each frame; only gate is `Dimensions.ShouldNotTick`. Cost scales linearly with loaded-entity count. | Prefix to add gating (e.g. distance/State pre-filter, time-slicing); or Transpiler to inline a cheaper active check. | High — core loop, touches every entity |
| 2 | `EntityBehaviorTaskAI.OnGameTick` | `VSEssentials/Vintagestory.GameContent/EntityBehaviorTaskAI.cs:114` | The per-entity AI gate: runs `PathTraverser` + `TaskManager` only when `State == Active`. This is where pathfinding/tasks CPU is actually spent. | Postfix/Prefix to throttle task cadence, or stagger across ticks. For the **v0.3 bench** we instead force `State=Active` via the public `AlwaysActive` API (no patch needed — see entity-simulation.md). | Med |
| 3 | `AiTaskManager.OnGameTick` | `VSEssentials/Vintagestory.GameContent/AiTaskManager.cs:73` | Walks the active task list every tick (target scans, conditions). | Prefix to round-robin tasks or skip low-priority ones under load. | Med |
| 4 | `PhysicsManager.UpdateTrackedEntityState` | `VintagestoryLib/Vintagestory.Server/PhysicsManager.cs:465` | The active-distance predicate: `State=Active` iff a player is within `SimulationRange` (guarded by `!AlwaysActive`). Runs per entity in the physics pass. | Postfix to override activation policy (e.g. anchor-aware). For benching prefer the `AlwaysActive` escape hatch over a patch. | Med |
| 5 | `ServerSystemEntitySpawner.SpawnReadyMobs` | `VintagestoryLib/Vintagestory.Server/ServerSystemEntitySpawner.cs:145` (early-exit `if (server.Clients.IsEmpty)` at `:193`) | Mob spawning + cap accounting; player-gated, so it does nothing headless. | Prefix to relax the `Clients.IsEmpty` gate / treat anchors as pseudo-players → natural headless population for realism benches. | Med |

## Tier 2 — pathfinding & physics

| # | Target | Location | Why it's hot | Patch approach | Risk |
|--:|---|---|---|---|---|
| 6 | `AStar.FindPathOrEscapePath` | `VSEssentials/Vintagestory.Essentials/AStar.cs:68` (and `FindPath` `:62`) | The A\* search — the single heaviest AI op; expensive for large `searchDepth` or many simultaneous requests. | Prefix to cap `searchDepth`/budget per tick; Postfix to cache recent paths. | Med |
| 7 | `PhysicsManager.ServerTick` | `VintagestoryLib/Vintagestory.Server/PhysicsManager.cs:254` | Dispatches physics for tickable entities (load-balanced across threads). | Tune via patch only if profiling points here; mostly off main thread. | High |

## Tier 3 — chunk, lighting, block ticking, save (scale with world size)

| # | Target | Location | Why it's hot | Patch approach | Risk |
|--:|---|---|---|---|---|
| 8 | `ServerSystemBlockSimulation.OnServerTick` | `VintagestoryLib/Vintagestory.Server/ServerSystemBlockSimulation.cs:790` | Random block ticking / scheduled block updates; scales with loaded chunk count. | Prefix to throttle random-tick budget. | Med |
| 9 | `ServerSystemRelight.OnSeparateThreadTick` *(off-thread)* | `VintagestoryLib/Vintagestory.Server/ServerSystemRelight.cs:24` | Lighting recomputation; contends with main thread via locks on heavy edits. | Patch to batch/limit relight work units. | Med |
| 10 | `ServerSystemUnloadChunks.OnServerTick` | `VintagestoryLib/Vintagestory.Server/ServerSystemUnloadChunks.cs:105` | Periodic unload sweep over loaded columns. | Prefix to adjust unload cadence/criteria (interacts with VssAnchor keep-loaded). | Low |
| 11 | `ServerSystemSupplyChunks.OnSeparateThreadTick` *(off-thread)* | `VintagestoryLib/Vintagestory.Server/ServerSystemSupplyChunks.cs:77` (gen queue `tryLoadOrGenerateChunkColumnsInQueue` `:137`) | Chunk load/generate pump; worldgen bursts cause main-thread stalls when results are applied. | Patch to rate-limit gen throughput. | Med |
| 12 | `ServerSystemAutoSaveGame` | `VintagestoryLib/Vintagestory.Server/ServerSystemAutoSaveGame.cs` | Autosave can stall the main thread on large worlds. | Patch to tune cadence / off-thread more work. | Med |

## Tier 4 — main loop instrumentation

| # | Target | Location | Why it's useful | Patch approach | Risk |
|--:|---|---|---|---|---|
| 13 | `ServerMain.ProcessMainThreadTasks` | `VintagestoryLib/Vintagestory.Server/ServerMain.cs:3328` | The main-thread task pump (already wrapped in `FrameProfiler.Enter("mainthreadtasks")`). | Postfix to emit custom TPS/lag telemetry (feeds the future MCP). | Low |
| 14 | `ServerMain.ProcessMain` | `VintagestoryLib/Vintagestory.Server/ServerMain.cs:1563` | Top of the server frame. | Instrument-only; avoid mutating. | High |

## Notes
- **Prefer the public API over a patch** whenever the engine exposes the knob (events, `AlwaysActive`,
  `WorldManager` calls). Patches are version-fragile — re-verify every line here after a VS update.
- **Off-thread targets** (#9, #11) won't shrink the main tick directly; they reduce stalls/contention.
- Cross-reference the ✓/✗ patchability of any target in `patchable-VintagestoryLib.md` /
  `patchable-VSEssentials.md` before writing the patch.
