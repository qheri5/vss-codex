// Registering a server chat command — the modern fluent ChatCommands API.
// Verified against vs-mod-vssanchor/src/AnchorModSystem.cs (deployed, working).
// Register in StartServerSide. Privileges come from Vintagestory.API.Server.Privilege.

using Vintagestory.API.Common;
using Vintagestory.API.Server;

public class ExampleModSystem : ModSystem
{
    public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

    public override void StartServerSide(ICoreServerAPI sapi)
    {
        var p = sapi.ChatCommands.Parsers;

        sapi.ChatCommands.Create("example")
            .WithDescription("Demo command")
            .RequiresPrivilege(Privilege.controlserver)   // gate by privilege

            .BeginSubCommand("greet")
                .WithDescription("Greet a target")
                .WithArgs(p.Word("name"), p.OptionalInt("times", 1))
                .HandleWith(OnGreet)
            .EndSubCommand()

            .BeginSubCommand("count")
                .WithDescription("Report loaded chunk count")
                .HandleWith(args =>
                    TextCommandResult.Success(
                        $"Loaded chunks: {sapi.WorldManager.AllLoadedChunks.Count}"))
            .EndSubCommand();
    }

    private TextCommandResult OnGreet(TextCommandCallingArgs args)
    {
        string name = (string)args[0];
        int times = (int)args[1];
        return TextCommandResult.Success(string.Concat(System.Linq.Enumerable.Repeat($"Hi {name}! ", times)));
    }
}
