# Harmony — how to patch VS safely

Harmony (`Lib/0Harmony.dll`, [pardeike/Harmony](https://harmony.pardeike.net/)) is runtime
method-patching. Use it **only for engine internals the public API doesn't expose** — for anything the
API offers (events, properties, `WorldManager`, `AlwaysActive`), use the API instead.

> **Reality check:** *no first-party VS mod patches gameplay with Harmony* — the base game exposes
> events/hooks for almost everything. Harmony is a third-party-mod technique. Many tasks that look like
> they need a patch don't: keep chunks loaded with the native `WorldManager.LoadChunkColumn` call, or
> force entity activity via the public `AlwaysActive` property — no patch needed. Reach for Harmony
> only when you've confirmed the API can't do it.

## Patch kinds

| Kind | Runs | Can |
|---|---|---|
| **Prefix** | before the original | read/modify args (`ref`), set the return, **skip the original** (`return false`) |
| **Postfix** | after the original | read/modify the return (`ref __result`), read state, run side effects |
| **Transpiler** | rewrites the IL | surgically alter the method body itself (most powerful, most fragile) |
| **Finalizer** | after, even on exception | observe/swallow exceptions, cleanup |

Magic parameter names in a patch: `__instance` (the `this`), `__result` (return value, `ref` to set),
`__state` (pass data Prefix→Postfix), `___fieldName` (access a private field), `__originalMethod`.

## What Harmony CAN patch

Instance, static, virtual, private methods; property getters/setters; constructors; generic methods
(the patch applies to the shared body). If the catalog (`patchable-*.md`) marks it **✓**, it has an IL
body and is patchable.

## What Harmony CANNOT patch (✗ in the catalog)

- **abstract** methods — no body.
- **`extern` / `[MethodImpl(InternalCall)]` / P/Invoke** — implemented in native/runtime.
- runtime-implemented intrinsics.
- (unpredictable, runtime-only) very small methods the **JIT inlines** — the patch may silently not
  take effect; verify with `Harmony.GetPatchInfo(method)` or a log in the patch.

Check the flag before writing the patch: Grep the method in
`vs-game-reference/docs/generated/harmony/patchable-<assembly>.md`.

## Bootstrap idiom (in your `ModSystem`)

```csharp
private Harmony harmony;

public override void Start(ICoreAPI api)
{
    if (!Harmony.HasAnyPatches(Mod.Info.ModID))   // guard against double-apply
    {
        harmony = new Harmony(Mod.Info.ModID);     // unique id = your mod id
        harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
    }
}

public override void Dispose()
{
    harmony?.UnpatchAll(Mod.Info.ModID);           // ALWAYS unpatch on dispose
}
```

Patches are declared with attributes (`[HarmonyPatch(typeof(T), "Method")]` + `[HarmonyPrefix]` /
`[HarmonyPostfix]`) — see `examples/harmony-patch-server.cs`. Reference `0Harmony.dll` in the
`.csproj` (the mods already do: `HintPath=$(VintagestoryDir)\Lib\0Harmony.dll`, `Private=false`).

## Pitfalls

- **Version fragility:** patches target a method by name+signature. A VS update that renames/reshapes
  the target breaks the patch at load (or, worse, mis-binds). Re-verify targets after every VS update
  — the curated `high-value-targets.md` line numbers are pinned to the current build.
- **Mod conflicts:** two mods patching the same method can fight (Prefix `return false` cancels other
  Prefixes/the original). Keep patches minimal and prefer Postfix.
- **Inlining:** small hot methods may be inlined — confirm the patch actually ran.
- **Threading:** server systems run work off-thread (`OnSeparateThreadTick`, physics pool). A patch on
  an off-thread method must be thread-safe and won't shrink the **main** tick directly.

## For server performance work

Start from `vs-game-reference/docs/generated/harmony/high-value-targets.md` (curated hotspots:
`TickEntities`, `EntityBehaviorTaskAI.OnGameTick`, `UpdateTrackedEntityState`, `SpawnReadyMobs`,
`AStar.FindPathOrEscapePath`, …) and the mechanism write-up in `docs/entity-simulation.md`. Measure
with the engine's `FrameProfiler` (most hot paths are already bracketed with `FrameProfiler.Enter`).
