// Custom content: a Block backed by a BlockEntity that counts right-clicks, persists across saves,
// and ticks on the server. Register the classes in ModSystem.Start (side-agnostic). Content also
// needs JSON assets (a blocktype whose "class" + "entityClass" match the names below) -- see
// references/mod-setup.md for the asset/project layout.

using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

public class ContentModSystem : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        // These names match "class"/"entityClass" in the block's JSON blocktype.
        api.RegisterBlockClass("Counter", typeof(BlockCounter));
        api.RegisterBlockEntityClass("Counter", typeof(BlockEntityCounter));
    }
}

// Override block behaviour. Block instances are shared (one per blocktype) - per-position state lives
// in the BlockEntity, never in fields on the Block.
public class BlockCounter : Block
{
    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection bs)
    {
        if (world.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityCounter be)
        {
            be.Bump();
            if (world.Side == EnumAppSide.Server)
                (byPlayer as IServerPlayer)?.SendMessage(GlobalConstants.GeneralChatGroup,
                    $"count = {be.Count}", EnumChatType.Notification);
            return true; // handled
        }
        return base.OnBlockInteractStart(world, byPlayer, bs);
    }
}

// Per-position persistent state + ticking.
public class BlockEntityCounter : BlockEntity
{
    public int Count { get; private set; }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        if (api.Side == EnumAppSide.Server)
            RegisterGameTickListener(OnServerTick, 1000); // ms
    }

    public void Bump()
    {
        Count++;
        MarkDirty(true); // persist to disk + sync to nearby clients
    }

    private void OnServerTick(float dt) { /* periodic per-block logic */ }

    // Save/load: write to and read from the block-entity's attribute tree.
    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetInt("count", Count);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world)
    {
        base.FromTreeAttributes(tree, world);
        Count = tree.GetInt("count");
    }
}
