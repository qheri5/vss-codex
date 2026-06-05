// Subscribing to server events + a tick listener.
// Events live on sapi.Event (IServerEventAPI). Tick listeners + run-phase hooks too.
// Verified event names against decompiled IServerEventAPI.cs.

using Vintagestory.API.Common;
using Vintagestory.API.Server;

public class ExampleEventsModSystem : ModSystem
{
    private ICoreServerAPI sapi;

    public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;

        // Player lifecycle
        sapi.Event.PlayerJoin       += p => sapi.Logger.Notification("[Example] {0} joined", p.PlayerName);
        sapi.Event.PlayerNowPlaying += p => sapi.Logger.Notification("[Example] {0} ready to play", p.PlayerName);
        sapi.Event.PlayerLeave      += p => sapi.Logger.Notification("[Example] {0} left", p.PlayerName);

        // World lifecycle: WorldManager (chunks, save data) is ready at RunGame.
        sapi.Event.ServerRunPhase(EnumServerRunPhase.RunGame, OnRunGame);

        // Periodic work: dt in seconds, interval in ms. Keep it cheap — it runs on the main thread.
        sapi.Event.RegisterGameTickListener(OnEverySecond, 1000);
    }

    private void OnRunGame()
    {
        sapi.Logger.Notification("[Example] world ready — {0} chunks loaded",
            sapi.WorldManager.AllLoadedChunks.Count);
    }

    private void OnEverySecond(float dt)
    {
        // e.g. sample entity/chunk counts for telemetry
        int entities = sapi.World.LoadedEntities.Count;
        // ... do something light
    }

    public override void Dispose()
    {
        // Tick listeners and events are torn down with the server; for long-lived mods that
        // re-init, unsubscribe explicitly here to avoid leaks.
    }
}
