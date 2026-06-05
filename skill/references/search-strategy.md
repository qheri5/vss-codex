# Search recipes over the reference

Concrete Grep/Glob patterns. All paths are relative to the **reference root** (the absolute path is in
`SKILL.md`). Prefer the generated docs for "what/where", drop to `decompiled/` for "how".

## Find a type's source

A type lives at `decompiled/<assembly>/<namespace-as-folders>/<TypeName>.cs`. If you don't know the
assembly:
- `Glob` `decompiled/**/EntityAgent.cs`
- or `Grep` for the declaration: pattern `class EntityAgent\b` / `interface ICoreServerAPI\b` /
  `enum EnumEntityState\b`, path `decompiled/`.

## Look up an API signature / summary

- `Grep` the type name with `## ` prefix in `docs/generated/api/`, e.g.
  pattern `## BlockEntity\b`. Each namespace is one file; `api/INDEX.md` maps namespace → file.
- For a member: `Grep` the method name inside the namespace file (it lists `signature — summary`).
- Summaries are official (from `VintagestoryAPI.xml`) or `*(inherited)*` from a base/interface;
  `⚠️ [Obsolete]` flags deprecated members.

## Events / enums / engine internals

- **Event** to subscribe to → `Grep` it in `api/events.md` (events + delegate signatures, consolidated).
- **Enum** values → `Grep` it in `api/enums.md`.
- **Engine type** (not in the public API — `ServerMain`, `PhysicsManager`, `ServerSystem*`) →
  `api/lib/` (signatures only, engine-internal). For the patch flag use `harmony/patchable-*.md`.
- After a VS update, what changed → `api/../CHANGELOG-<old>-to-<new>.md` (added/removed/flag flips).

## Check Harmony patchability

- `Grep` the method name in `docs/generated/harmony/patchable-<assembly>.md`;
  the row starts with `✓` (patchable) or `✗` (+ reason). Engine internals → `patchable-VintagestoryLib.md`;
  AI/pathfinding → `patchable-VSEssentials.md`; survival content → `patchable-VSSurvivalMod.md`.

## Find where something happens (behavior)

- Server systems & tick: `decompiled/VintagestoryLib/Vintagestory.Server/` (`ServerMain.cs`,
  `ServerSystem*.cs`, `PhysicsManager.cs`).
- AI / pathfinding: `decompiled/VSEssentials/Vintagestory.GameContent/` (`AiTaskManager.cs`,
  `EntityBehaviorTaskAI.cs`, AI tasks) and `decompiled/VSEssentials/Vintagestory.Essentials/`
  (`AStar.cs`, `WaypointsTraverser.cs`).
- Survival blocks/items/entities: `decompiled/VSSurvivalMod/Vintagestory.GameContent/`.
- Find an enum's members: `Grep` pattern `enum <Name>` then read that file.
- Find who reads/writes a field: `Grep` the field name across `decompiled/` (e.g. `\.State =`,
  `AlwaysActive`).

## Performance surface (the hot loops)

`Grep` for `RegisterGameTickListener`, `OnServerTick`, `OnSeparateThreadTick`, or `FrameProfiler.Enter`
in `decompiled/VintagestoryLib/Vintagestory.Server/` to find tick entry points and what the engine
already profiles.

## Tips

- The generated docs are large per-namespace/per-assembly files — **Grep, don't Read** them whole.
- Decompiled `.cs` retains official `///` XML doc comments (ILSpy merged `VintagestoryAPI.xml`).
- Everything reflects one VS version (stamped in every generated file). After a VS update the tree is
  stale until re-decompiled + regenerated — check `README.md` in the reference root for the version.
