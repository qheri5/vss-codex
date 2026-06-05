# Reference map — subsystem → decompiled path

Where each subsystem lives in `vs-game-reference/decompiled/`. (Extends `docs/README.md`'s map.)

## Assemblies

| Assembly | Role | Key namespaces |
|---|---|---|
| `VintagestoryAPI` | **public modding contract** (the only one in the API doc) | `Vintagestory.API.Common[.Entities]`, `.Server`, `.Client`, `.Datastructures`, `.MathTools`, `.Config`, `.Util`, `.Net` |
| `VintagestoryLib` | **engine internals** (server + client + common impl) | `Vintagestory.Server`, `Vintagestory.Common`, `Vintagestory.Client.NoObf`, `Vintagestory.ServerMods.NoObf` |
| `VintagestoryServer` | dedicated-server entry point (~2 methods; logic is in Lib) | — |
| `Vintagestory` | client launcher (thin; client logic is in Lib) — lower priority | — |
| `VSEssentials` | first-party mod: **AI, pathfinding, physics** | `Vintagestory.GameContent` (AI tasks), `Vintagestory.Essentials` (A*, traversers) |
| `VSSurvivalMod` | first-party mod: survival content | `Vintagestory.GameContent`, `Vintagestory.ServerMods` |
| `VSCreativeMod` | first-party mod: creative tools | `Vintagestory.ServerMods`, `Vintagestory.GameContent` |
| `VSCrashReporter`/`Lib`, `ModMaker` | tooling | — |

## Subsystems → file

| Subsystem | Path |
|---|---|
| Server bootstrap & main loop | `VintagestoryLib/Vintagestory.Server/ServerMain.cs` |
| Server systems (tick units) | `VintagestoryLib/Vintagestory.Server/ServerSystem*.cs` |
| Entity tick / despawn | `VintagestoryLib/Vintagestory.Server/ServerSystemEntitySimulation.cs` |
| Entity active-state predicate / physics | `VintagestoryLib/Vintagestory.Server/PhysicsManager.cs` |
| Mob spawning | `VintagestoryLib/Vintagestory.Server/ServerSystemEntitySpawner.cs` |
| Chunk load/gen/unload/save/relight | `VintagestoryLib/Vintagestory.Server/ServerSystem{SupplyChunks,UnloadChunks,LoadAndSaveGame,AutoSaveGame,Relight,SendChunks}.cs` |
| Block ticking | `VintagestoryLib/Vintagestory.Server/ServerSystemBlockSimulation.cs` |
| Entity base + State enum | `VintagestoryAPI/Vintagestory.API.Common.Entities/Entity.cs`, `VintagestoryAPI/Vintagestory.API.Common/EnumEntityState.cs` |
| AI task manager + behavior | `VSEssentials/Vintagestory.GameContent/AiTaskManager.cs`, `EntityBehaviorTaskAI.cs` |
| AI tasks (wander, seek, flee…) | `VSEssentials/Vintagestory.GameContent/AiTask*.cs` |
| Pathfinding (A*) | `VSEssentials/Vintagestory.Essentials/AStar.cs`, `WaypointsTraverser.cs`, `PathfindSystem.cs` |
| Modding entry points | `VintagestoryAPI/Vintagestory.API.Common/ModSystem.cs`, `Vintagestory.API.Server/ICoreServerAPI.cs`, `IServerEventAPI.cs`, `IWorldManagerAPI.cs` |

## The builder (committable, not proprietary)

| What | Path |
|---|---|
| Generator + formatter | `vss-codex/` (run `vss-codex/vss-codex.ps1` to rebuild the reference) |
| MCP scaffold | `vss-codex/mcp/` |
