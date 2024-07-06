using Cairo;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace DrawOnMap;

public class DrawOnMapLayer : MapLayer
{
    private Dictionary<BlockPos, DrawOnMapComponent> loadedMapData = new Dictionary<BlockPos, DrawOnMapComponent>();
    private ICoreClientAPI capi;
    private bool leftDraw;

    private bool recompose;
    private GuiComposer composer;

    public override string Title => "Drawing Surface";
    public override EnumMapAppSide DataSide => EnumMapAppSide.Client;
    public override string LayerGroupCode => "drawonmap:drawingsurface";
    public override bool RequireChunkLoaded => false;

    public DrawOnMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        capi = api as ICoreClientAPI;
        capi.Event.MouseUp += Event_MouseUp;
        capi.Event.MouseMove += Event_MouseMove;
        capi.Event.MouseDown += Event_MouseDown;
        capi.Event.RegisterGameTickListener(Every100ms, 100);
    }

    private void Every100ms(float dt)
    {
        if (composer != null)
        {
            composer.ReCompose();
        }
    }

    private void Event_MouseDown(MouseEvent args)
    {
        if (args.Button == EnumMouseButton.Middle)
        {
            leftDraw = true;
        }
    }

    private void Event_MouseMove(MouseEvent args)
    {

    }

    private void Event_MouseUp(MouseEvent e)
    {
        if (e.Button == EnumMouseButton.Middle)
        {
            leftDraw = false;
        }
    }

    public override void Dispose()
    {
        foreach (DrawOnMapComponent value in loadedMapData.Values)
        {
            value?.Dispose();
        }
    }

    public override void Render(GuiElementMap mapElem, float dt)
    {
        if (!Active)
        {
            return;
        }
        foreach (DrawOnMapComponent loadedMapDatum in loadedMapData.Values)
        {
            loadedMapDatum.Render(mapElem, dt);
        }
    }

    public override void OnMouseMoveClient(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
    {
        if (!Active)
        {
            return;
        }

        if (leftDraw)
        {
            Draw(args, mapElem);
        }

        foreach (DrawOnMapComponent loadedMapDatum in loadedMapData.Values)
        {
            loadedMapDatum.OnMouseMove(args, mapElem, hoverText);
        }
    }

    public override void OnMouseUpClient(MouseEvent args, GuiElementMap mapElem)
    {
        if (!Active)
        {
            return;
        }

        foreach (DrawOnMapComponent loadedMapDatum in loadedMapData.Values)
        {
            loadedMapDatum.OnMouseUpOnElement(args, mapElem);
        }
    }

    private void Draw(MouseEvent args, GuiElementMap mapElem)
    {
        Vec2f viewPos = new Vec2f(args.X, args.Y);
        viewPos.X = viewPos.X - (float)mapElem.Bounds.renderX;
        viewPos.Y = viewPos.Y - (float)mapElem.Bounds.renderY;

        Vec3d worldPos = new Vec3d();
        mapElem.TranslateViewPosToWorldPos(viewPos, ref worldPos);
        BlockPos blockPos = worldPos.AsBlockPos;

        if (loadedMapData.ContainsKey(blockPos))
        {
            loadedMapData[blockPos].Color = DrawingSystem.currentColorVec4f;
            return;
        }
        Vec4f color = DrawingSystem.currentColorVec4f;
        loadedMapData[blockPos] = new DrawOnMapComponent(capi, blockPos, color);
    }

    public override void ComposeDialogExtras(GuiDialogWorldMap guiDialogWorldMap, GuiComposer compo)
    {
        string key = "worldmap-layer-" + LayerGroupCode; 
        ElementBounds dlgBounds = ElementStdBounds.AutosizedMainDialog.WithFixedPosition(
            x: ((compo.Bounds.renderX + compo.Bounds.OuterWidth) / RuntimeEnv.GUIScale) + 30.0,
            y: compo.Bounds.renderY / (double)RuntimeEnv.GUIScale)
            .WithFixedOffset(0, 200)
            .WithAlignment(EnumDialogArea.None);

        ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;

        ElementBounds sliderB = ElementBounds.Fixed(0.0, 30.0, 120.0, 35.0);
        ElementBounds sliderG = sliderB.CopyOffsetedSibling(0, 40);
        ElementBounds sliderR = sliderG.CopyOffsetedSibling(0, 40);
        ElementBounds sliderA = sliderR.CopyOffsetedSibling(0, 40);
        ElementBounds drawBounds = sliderA.CopyOffsetedSibling(0, 40).WithFixedHeight(sliderB.fixedWidth);
        composer = guiDialogWorldMap.Composers[key] = capi.Gui.CreateCompo(key, dlgBounds)
            .AddShadedDialogBG(bgBounds, withTitleBar: true)
            .AddDialogTitleBar(Lang.Get("drawonmap:color-picker"), () => OnClose(guiDialogWorldMap, key), font: CairoFont.WhiteSmallText())
        .BeginChildElements(bgBounds)
            .AddSlider((newVal) => OnSlider(newVal, EnumColorValue.R), sliderR, "sliderR")
            .AddSlider((newVal) => OnSlider(newVal, EnumColorValue.G), sliderG, "sliderG")
            .AddSlider((newVal) => OnSlider(newVal, EnumColorValue.B), sliderB, "sliderB")
            .AddSlider((newVal) => OnSlider(newVal, EnumColorValue.A), sliderA, "sliderA")
            .AddDynamicCustomDraw(drawBounds, OnDrawColor)
        .EndChildElements()
        .Compose();

        OnClose(guiDialogWorldMap, key);
        guiDialogWorldMap.Composers[key].GetSlider("sliderR").SetValues(currentValue: DrawingSystem.R, minValue: 0, maxValue: 255, step: 1);
        guiDialogWorldMap.Composers[key].GetSlider("sliderG").SetValues(currentValue: DrawingSystem.G, minValue: 0, maxValue: 255, step: 1);
        guiDialogWorldMap.Composers[key].GetSlider("sliderB").SetValues(currentValue: DrawingSystem.B, minValue: 0, maxValue: 255, step: 1);
        guiDialogWorldMap.Composers[key].GetSlider("sliderA").SetValues(currentValue: DrawingSystem.A, minValue: 0, maxValue: 255, step: 1);
    }

    private static void OnClose(GuiDialogWorldMap guiDialogWorldMap, string key)
    {
        guiDialogWorldMap.Composers[key].Enabled = true;
    }

    private bool OnSlider(int newValue, EnumColorValue colorValue)
    {
        DrawingSystem.SetColor((byte)newValue, colorValue);
        recompose = true;
        return true;
    }

    private void OnDrawColor(Context ctx, ImageSurface surface, ElementBounds currentBounds)
    {
        ctx.SetSourceRGBA(DrawingSystem.currentColorDoubles);
        ctx.Paint();
    }
}