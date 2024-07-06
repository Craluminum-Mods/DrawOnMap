using Cairo;
using System;
using Vintagestory.API.Client;

namespace DrawOnMap;

public class ToolListPicker : GuiElementIconListPicker
{
    public ToolListPicker(ICoreClientAPI capi, string elem, ElementBounds bounds) : base(capi, elem, bounds)
    {
    }

    public override void DrawElement(string icon, Context ctx, ImageSurface surface)
    {
        ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.2);
        RoundRectangle(ctx, Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight, 1.0);
        ctx.Fill();
        api.Gui.Icons.DrawIcon(ctx, icon, Bounds.drawX + 2.0, Bounds.drawY + 2.0, Bounds.InnerWidth - 4.0, Bounds.InnerHeight - 4.0, new double[4] { 1.0, 1.0, 1.0, 1.0 });
    }
}

public static class ToolListPickerExtensions
{
    public static GuiComposer AddToolListPicker(this GuiComposer composer, string[] icons, Action<int> onToggle, ElementBounds startBounds, int maxLineWidth, string key = null)
    {
        return composer.AddElementListPicker(typeof(ToolListPicker), icons, onToggle, startBounds, maxLineWidth, key);
    }
}