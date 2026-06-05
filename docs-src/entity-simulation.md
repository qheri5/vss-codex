# Entity simulation & the "entity active" predicate

> File:line citations point into `../decompiled/`.
> This is the single most important system for server load-benchmarking / optimization work: AI is
> the dominant per-entity cost, and it is gated by the "entity active" state described below.

## TL;DR

On the server, **every loaded entity is ticked every frame**, but the expensive part —
**AI (pathfinding + tasks) only runs when `entity.State == EnumEntityState.Active`**.
An entity is `Active` *only* when a player is within its `SimulationRange`, **or** when
`entity.AlwaysActive == true`. With **zero connected clients**:

1. the entity spawner is fully disabled → **no mobs spawn** in anchored chunks, and
2. any entity that did exist would be `Inactive` → **its AI never ticks**.

That is exactly why a chunk-anchoring bench can measure chunk/block load yet see **0 active
entities / no AI**. To exercise AI headless you must **spawn mobs yourself and force them
`Active`** — which the public API allows with no Harmony patch.

## The call chain (who ticks what)

1. **`ServerSystemEntitySimulation.OnServerTick` → `TickEntities(dt)`**
   `VintagestoryLib/Vintagestory.Server/ServerSystemEntitySimulation.cs:188`
   Iterates **all** `server.LoadedEntities.Values` and calls `value.OnGameTick(dt)`.
   The *only* gate here is `Dimensions.ShouldNotTick(pos, api)` — **not** the player
   distance and **not** the State. So the tick loop itself is state-agnostic.

2. **`Entity.OnGameTick`** → runs every server-side behavior's `OnGameTick`
   `VintagestoryAPI/Vintagestory.API.Common.Entities/Entity.cs:1005`
   Behaviors run regardless of State; the State check is *inside* the AI behavior.

3. **`EntityBehaviorTaskAI.OnGameTick`** — the real cost gate
   `VSEssentials/Vintagestory.GameContent/EntityBehaviorTaskAI.cs:114`
   It runs the AI work **only when `entity.State == Active` and the entity is alive** — and only then
   ticks the `PathTraverser` (pathfinding) and the `TaskManager` (AI tasks). `Active` is value `0` of
   the enum (`VintagestoryAPI/.../EnumEntityState.cs`: `Active, Inactive, Despawned`). So **no Active →
   no pathfinding, no tasks**. (Read the exact guard at the cited line in the local decompiled tree.)

## The predicate that sets State: `PhysicsManager.UpdateTrackedEntityState`

`VintagestoryLib/Vintagestory.Server/PhysicsManager.cs:465`

The method computes the squared distance from the entity to the **nearest connected player** and
compares it against `entity.SimulationRange²` (with no players, that distance is `double.MaxValue`).
**Unless `entity.AlwaysActive` is set** (the guard at `PhysicsManager.cs:508`), it flips
`entity.State` to `Active` when a player is within range, else `Inactive`, firing `OnStateChanged`
on a change. (Exact code at the cited lines in the local decompiled tree.)

- `State` is otherwise written in only two places: `Entity.DoInitialActiveCheck`
  (at spawn, `Entity.cs:698`) and despawn (`ServerMain.cs:2982`).
- `DoInitialActiveCheck` also honors `AlwaysActive` → spawns `Active` immediately
  (`Entity.cs:700`), else `Inactive` unless a player is already in range.
- **`AlwaysActive` is the engine's own escape hatch**: when true, the predicate skips the
  entity entirely (line 508 guard) so it *stays* whatever we set. `EntityPlayer` overrides
  `AlwaysActive => true` (`VintagestoryAPI/.../EntityPlayer.cs:261`) — players are always active.

### "Out of range" despawn is client-only — does NOT remove our mobs

`PhysicsManager.cs:570` adds far entities to `client.entitiesNowOutOfRange` → that's a
*per-client* despawn **packet** (client stops rendering). The server-side despawn gate is
`Entity.ShouldDespawn => !Alive` (`Entity.cs:418`). So a forced-active mob with no client
nearby is **not** removed server-side. Good — a headless bench is sustainable.

## Why headless = 0 mobs: the spawner is player-gated

`VintagestoryLib/Vintagestory.Server/ServerSystemEntitySpawner.cs`
- `:193` — `if (server.Clients.IsEmpty) return;` → **spawning is skipped entirely** with 0 clients.
- Spawn caps scale per player (`:665` `server.AllOnlinePlayers.Length`, `:674` `SpawnCapPlayerScaling`).
- Candidate positions are chosen around players (`:479` `server.NearestPlayer(...)`).

So natural population can't fill an anchored bubble headless. For a deterministic
benchmark we want explicit control over the mob count anyway.

## Implications for a headless entity-AI bench

To exercise entity AI with no connected client, **no Harmony is required** — the public API is enough:

| Need | API surface |
|------|-------------|
| enumerate loaded entities | `sapi.World.LoadedEntities` → `ConcurrentDictionary<long,Entity>` (`IServerWorldAccessor.cs:20`) |
| create a mob | `sapi.World.GetEntityType(AssetLocation)` + `world.ClassRegistry.CreateEntity(props)` then `sapi.World.SpawnEntity(entity)` (`IWorldAccessor.cs:280`) |
| force AI on with no player | set `entity.AlwaysActive = true` **and** `entity.State = EnumEntityState.Active` (both public) |

Design: spawn a configurable number/type of test mobs at a chosen location and mark them
`AlwaysActive` + `Active`. `EntityBehaviorTaskAI.OnGameTick` then runs pathfinding + tasks headless,
exercising exactly the entity-tick that entity-optimization mods target.

Caveat to verify in-game: some AI tasks need *targets* (other entities/players) to do real work —
wander/pathfinding will run regardless, but to load the seek/flee tasks you may want to spawn a mix
(e.g. a parked target or predator+prey pairs). Measure first.
