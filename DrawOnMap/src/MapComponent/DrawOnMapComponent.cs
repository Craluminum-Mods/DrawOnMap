using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace DrawOnMap;

public class DrawOnMapComponent : MapComponent
{
    public BlockPos Pos { get; set; }

    public Vec4f Color { get; set; }

    public DrawOnMapComponent(ICoreClientAPI capi, BlockPos pos, Vec4f color) : base(capi)
    {
        Pos = pos;
        Color = color;
    }

    public override void Render(GuiElementMap map, float dt)
    {
        var viewPos = new Vec2f();
        map.TranslateWorldPosToViewPos(Pos.ToVec3d(), ref viewPos);
        double x = map.Bounds.renderX + viewPos.X;
        double y = map.Bounds.renderY + viewPos.Y;

        capi.Render.RenderTexture(
            textureid: 1,
            posX: x,
            posY: y,
            width: 1.5 * map.ZoomLevel,
            height: 1.5 * map.ZoomLevel,
            color: Color);
    }
}
