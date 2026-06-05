// A server-side Harmony patch — ONLY when the public API can't do the job.
// See references/harmony-usage.md. Check the target is ✓ patchable in
// docs/generated/harmony/patchable-VintagestoryLib.md before relying on it.
//
// .csproj needs: <Reference Include="0Harmony"><HintPath>$(VintagestoryDir)\Lib\0Harmony.dll</HintPath>
//                <Private>false</Private></Reference>  (plus VintagestoryLib for the target type).

using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.Server;            // PhysicsManager lives in VintagestoryLib / Vintagestory.Server

public class ExamplePatchModSystem : ModSystem
{
    private Harmony harmony;

    public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

    // Register patches side-agnostically in Start (guard against double-apply).
    public override void Start(ICoreAPI api)
    {
        if (!Harmony.HasAnyPatches(Mod.Info.ModID))
        {
            harmony = new Harmony(Mod.Info.ModID);                 // unique id = mod id
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
        }
    }

    public override void Dispose()
    {
        harmony?.UnpatchAll(Mod.Info.ModID);                       // ALWAYS unpatch
    }
}

// Example: observe the active-state predicate (PhysicsManager.UpdateTrackedEntityState).
// This is illustrative — for the v0.3 bench we instead set entity.AlwaysActive via the public API,
// which the engine itself honors (no patch). Use a patch only when there is no such knob.
[HarmonyPatch(typeof(PhysicsManager), "UpdateTrackedEntityState")]
public static class UpdateTrackedEntityStatePatch
{
    // Postfix: runs after the original; can read args, the entity, and the bool result (ref __result).
    static void Postfix(Entity entity, bool __result)
    {
        // __result == true means the original flipped this entity's State this call.
        // e.g. count activations, or (carefully) override policy. Keep it cheap — runs per entity.
        if (__result && entity.State == EnumEntityState.Active)
        {
            // entity.World.Logger.VerboseDebug("[Example] activated {0}", entity.Code);
        }
    }

    // A Prefix instead could short-circuit the original:
    //   static bool Prefix(Entity entity, ref bool __result) { __result = false; return false; }
    // (return false = skip original). Mind mod conflicts — prefer Postfix.
}
