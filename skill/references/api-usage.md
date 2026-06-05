# VS API — how to use it

How a Vintage Story mod plugs into the engine. Look types up in
`docs/generated/api/`; read ground truth in
`decompiled/VintagestoryAPI/`.

## A mod = a `ModSystem` (plus optional content)

There is **no Bukkit/Spigot-style plugin split** — everything is a mod. Code mods subclass
`ModSystem` (`Vintagestory.API.Common`). The engine instantiates it on the relevant side(s) and calls
lifecycle methods **in this order**:

| Method | When / use |
|---|---|
| `bool ShouldLoad(EnumAppSide)` | gate the side. Server-only mods: `return side == EnumAppSide.Server;` |
| `double ExecuteOrder()` | load-order weight (lower = earlier); default 0.1 for content, higher for systems |
| `void StartPre(ICoreAPI)` | earliest hook, before `Start` |
| `void Start(ICoreAPI)` | **side-agnostic** registration: classes, behaviors, blocks/items, network channels. Register Harmony here if patching (see harmony-usage.md). |
| `void AssetsLoaded(ICoreAPI)` | assets (JSON/lang/shapes) are now available — **not before** |
| `void AssetsFinalize(ICoreAPI)` | post-asset fixups |
| `void StartServerSide(ICoreServerAPI)` | server-only setup: commands, event subscriptions, tick listeners |
| `void StartClientSide(ICoreClientAPI)` | client-only setup: GUI, input, rendering |
| `void Dispose()` | teardown — unsubscribe, `UnpatchAll(harmonyId)`, release resources |

**Rule:** don't touch `api.Assets` / look up blocks before `AssetsLoaded`. Register event handlers
that need assets in `Start()` (the `AssetsFinalizers` event fires before `StartServerSide`).

Embed metadata with `[assembly: ModInfo(...)]` on the assembly — a server-only code mod then deploys
as a single DLL dropped into the server's `Mods/` folder.

## The API tree

`ICoreAPI` (common) is split into `ICoreServerAPI` (`sapi`) and `ICoreClientAPI` (`capi`). Sub-APIs:

| Sub-API (`sapi.…`) | Interface | What it does |
|---|---|---|
| `World` | `IServerWorldAccessor` | the game world: `LoadedEntities`, `SpawnEntity`, `BlockAccessor`, `GetEntitiesAround`, `Rand` |
| `Event` | `IServerEventAPI` | subscribe to engine events (see below) + `RegisterGameTickListener` |
| `WorldManager` | `IWorldManagerAPI` | chunks: `LoadChunkColumn(keepLoaded)`, `UnloadChunkColumn`, `AllLoadedChunks`, `ChunkSize` |
| `Network` | `IServerNetworkAPI` | register channels, send/broadcast packets |
| `ChatCommands` | `IChatCommandApi` | register `/commands` (see command-registration.cs) |
| `Permissions` | `IPermissionManager` | roles & privileges |
| `Groups` | `IGroupManager` | player groups |
| `PlayerData` | `IPlayerDataManager` | world-agnostic player data (queryable offline) |
| `Server` | `IServerAPI` | server config, run phases, `Logger` |
| `ModLoader` (common) | `IModLoader` | get other mod systems |

**Common events** (`sapi.Event.…`): `PlayerJoin`, `PlayerNowPlaying`, `PlayerReady`, `PlayerLeave`,
`PlayerChat`, `PlayerDeath`, `OnTrySpawnEntity`, `ChunkColumnLoaded/Unloaded`, `SaveGameLoaded`,
`GameWorldSave`, plus `RegisterGameTickListener(dt => …, intervalMs)` and
`ServerRunPhase(EnumServerRunPhase.RunGame, …)`. (See `examples/event-handler.cs`.)

## Content base classes (subclass + override behavior)

- `CollectibleObject` → `Block`, `Item` (`Vintagestory.API.Common`). Override `OnBlockInteractStart`,
  `OnHeldInteractStart`, etc.
- `BlockEntity` — per-position persistent logic (tick via `RegisterGameTickListener`, save/load via
  `ToTreeAttributes`/`FromTreeAttributes`).
- `Entity` → `EntityAgent` (mobs/players) — has `WatchedAttributes`, `Pos`, `State`, behaviors.
- **Behaviors** are the composition mechanism: `EntityBehavior`, `BlockBehavior`,
  `CollectibleBehavior`, `BlockEntityBehavior`. Prefer adding a behavior over subclassing when you can.

Attach data with `WatchedAttributes` (synced) / `Attributes` (server-only); both are `TreeAttribute`.

## When to use the generated API doc vs raw source

- **API doc** (`docs/generated/api/`): signatures + official summaries — fastest for "what's the
  method / what does it mean". Only ~37% of public types carry an official summary; a type flagged
  **⚠ No official summary — signature-only** has none. For those, the doc gives you the *shape* (the
  signature) but **not** the behavior — say so and read the decompiled `.cs` instead of inferring
  meaning from the name. "Has a signature" is not "is documented".
- **Raw decompiled** (`decompiled/`): when you need the actual implementation, default values,
  call sites, or exact behavior. The API doc only covers `VintagestoryAPI.dll` (the public contract);
  engine internals (ServerMain, PhysicsManager, server systems) live in `VintagestoryLib` and are
  only in the Harmony catalog / raw source.
