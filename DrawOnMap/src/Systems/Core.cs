using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace DrawOnMap;

public class Core : ModSystem
{
    public override void StartClientSide(ICoreClientAPI api)
    {
        api.ModLoader.GetModSystem<WorldMapManager>().RegisterMapLayer<DrawOnMapLayer>("drawonmap:drawingsurface", 0.01);
        api.Logger.Event("started '{0}' mod", Mod.Info.Name);
    }
}
