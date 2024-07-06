using Cairo;
using System;
using System.Collections.Generic;
using System.Linq;
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

    private bool canDraw;
    private EnumMouseButton buttonForDrawing = EnumMouseButton.Middle;

    private int selectedTool = 0;
    private string selectedToolName => drawingTools[selectedTool];
    private string[] drawingTools => new string[] { "paintbrush", "eraser" };

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
        capi.Event.KeyDown += Event_KeyDown;
        capi.Event.KeyUp += Event_KeyUp;
        capi.Event.RegisterGameTickListener(Every100ms, 100);
    }

    private void Event_KeyDown(KeyEvent args)
    {
        if (!Active)
        {
            return;
        }
    }

    private void Event_KeyUp(KeyEvent args)
    {
        if (!Active)
        {
            return;
        }
    }

    private void Event_MouseDown(MouseEvent args)
    {
        if (!Active)
        {
            return;
        }
        if (args.Button == buttonForDrawing)
        {
            // universe itself collapses when handled
            //args.Handled = true;
            canDraw = true;
        }
    }

    private void Event_MouseMove(MouseEvent args)
    {
        if (!Active)
        {
            return;
        }
    }

    private void Event_MouseUp(MouseEvent args)
    {
        if (!Active)
        {
            return;
        }
        if (args.Button == buttonForDrawing)
        {
            // universe itself collapses when handled
            //args.Handled = true;
            canDraw = false;
        }
    }

    private void Every100ms(float dt)
    {
        if (!Active)
        {
            return;
        }
        if (recompose)
        {
        composer?.ReCompose();
            recompose = false;
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

        if (canDraw && selectedToolName == "paintbrush")
        {
            Draw(args.X, args.Y, mapElem);
        }

        if (canDraw && selectedToolName == "eraser")
        {
            Erase(args.X, args.Y, mapElem);
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

    private void Draw(int x, int y, GuiElementMap mapElem)
    {
        Vec2f viewPos = new Vec2f(x, y);
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

    private void Erase(int x, int y, GuiElementMap mapElem)
    {
        Vec2f viewPos = new Vec2f(x, y);
        viewPos.X = viewPos.X - (float)mapElem.Bounds.renderX;
        viewPos.Y = viewPos.Y - (float)mapElem.Bounds.renderY;

        Vec3d worldPos = new Vec3d();
        mapElem.TranslateViewPosToWorldPos(viewPos, ref worldPos);
        BlockPos blockPos = worldPos.AsBlockPos;

        if (loadedMapData.ContainsKey(blockPos))
        {
            loadedMapData.Remove(blockPos);
        }
    }

    public override void ComposeDialogExtras(GuiDialogWorldMap guiDialogWorldMap, GuiComposer compo)
    {
        string key = "worldmap-layer-" + LayerGroupCode;
        ElementBounds dlgBounds = ElementStdBounds.AutosizedMainDialog.WithFixedPosition(
            x: ((compo.Bounds.renderX + compo.Bounds.OuterWidth) / RuntimeEnv.GUIScale) + 30.0,
            y: compo.Bounds.renderY / (double)RuntimeEnv.GUIScale)
            .WithFixedOffset(0, 200)
            .WithAlignment(EnumDialogArea.None);

        string[] names = Enum.GetNames(typeof(EnumMouseButton));
        string[] values = Enum.GetValues<EnumMouseButton>().Select(x => x.ToString()).ToArray();

        double indent = 30;
        double gap = 10;
        double offsetY = indent + gap;

        ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;

        ElementBounds sliderRTextBounds = ElementBounds.Fixed(0, indent, indent, indent);
        ElementBounds sliderGTextBounds = sliderRTextBounds.CopyOffsetedSibling(0, offsetY);
        ElementBounds sliderBTextBounds = sliderGTextBounds.CopyOffsetedSibling(0, offsetY);
        ElementBounds sliderATextBounds = sliderBTextBounds.CopyOffsetedSibling(0, offsetY);

        ElementBounds dropdownIconBounds = sliderATextBounds.CopyOffsetedSibling(0, offsetY * 2);

        ElementBounds sliderB = sliderRTextBounds.FlatCopy().WithFixedOffset(offsetY, 0).WithFixedSize(120, sliderRTextBounds.fixedHeight);
        ElementBounds sliderG = sliderB.CopyOffsetedSibling(0, offsetY);
        ElementBounds sliderR = sliderG.CopyOffsetedSibling(0, offsetY);
        ElementBounds sliderA = sliderR.CopyOffsetedSibling(0, offsetY);

        ElementBounds drawBounds = sliderA.CopyOffsetedSibling(0, offsetY);

        ElementBounds dropdownBounds = drawBounds.CopyOffsetedSibling(0, offsetY);

        ElementBounds toolPickerBounds = dropdownIconBounds.CopyOffsetedSibling(0, offsetY).WithFixedWidth(dropdownIconBounds.fixedWidth);

        try
        {
            composer = guiDialogWorldMap.Composers[key] = capi.Gui.CreateCompo(key, dlgBounds)
                .AddShadedDialogBG(bgBounds, withTitleBar: true)
                .AddDialogTitleBar(Lang.Get("drawonmap:drawing"), () => OnClose(guiDialogWorldMap, key), font: CairoFont.WhiteSmallText())
            .BeginChildElements(bgBounds)
                .AddDynamicText("B:", CairoFont.WhiteMediumText().WithFontSize(28).WithOrientation(EnumTextOrientation.Left), sliderBTextBounds)
                .AddDynamicText("G:", CairoFont.WhiteMediumText().WithFontSize(28).WithOrientation(EnumTextOrientation.Left), sliderGTextBounds)
                .AddDynamicText("R:", CairoFont.WhiteMediumText().WithFontSize(28).WithOrientation(EnumTextOrientation.Left), sliderRTextBounds)
                .AddDynamicText("A:", CairoFont.WhiteMediumText().WithFontSize(28).WithOrientation(EnumTextOrientation.Left), sliderATextBounds)
                .AddSlider((newVal) => OnSlider(newVal, EnumColorValue.B), sliderB, "sliderB")
                .AddSlider((newVal) => OnSlider(newVal, EnumColorValue.G), sliderG, "sliderG")
                .AddSlider((newVal) => OnSlider(newVal, EnumColorValue.R), sliderR, "sliderR")
                .AddSlider((newVal) => OnSlider(newVal, EnumColorValue.A), sliderA, "sliderA")

                .AddDynamicCustomDraw(drawBounds, OnDrawColor)
                .AddInset(drawBounds)

                .AddStaticCustomDraw(dropdownIconBounds, OnDrawMouse)
                .AddDropDown(values, names, (int)buttonForDrawing, OnSelectionChanged, dropdownBounds)

                .AddToolListPicker(drawingTools, OnPickTool, toolPickerBounds, (int)toolPickerBounds.fixedWidth * 5, "toolPicker")
            .EndChildElements()
            .Compose();
        }
        catch (Exception) { }

        OnClose(guiDialogWorldMap, key);
        guiDialogWorldMap.Composers[key].GetSlider("sliderR").SetValues(currentValue: DrawingSystem.R, minValue: 0, maxValue: 255, step: 1);
        guiDialogWorldMap.Composers[key].GetSlider("sliderG").SetValues(currentValue: DrawingSystem.G, minValue: 0, maxValue: 255, step: 1);
        guiDialogWorldMap.Composers[key].GetSlider("sliderB").SetValues(currentValue: DrawingSystem.B, minValue: 0, maxValue: 255, step: 1);
        guiDialogWorldMap.Composers[key].GetSlider("sliderA").SetValues(currentValue: DrawingSystem.A, minValue: 0, maxValue: 255, step: 1);

        guiDialogWorldMap.Composers[key].IconListPickerSetValue("toolPicker", selectedTool);
    }

    private void OnPickTool(int toolId)
    {
        selectedTool = toolId;
    }

    private void OnSelectionChanged(string code, bool selected)
    {
        EnumMouseButton newButton = buttonForDrawing;
        if (Enum.TryParse(code, out newButton))
        {
            buttonForDrawing = newButton;
        }
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

    private void OnDrawMouse(Context ctx, ImageSurface surface, ElementBounds currentBounds)
    {
        new IconUtil(capi).DrawLeftMouseButton(ctx, (int)currentBounds.drawX, (int)currentBounds.drawY, (float)currentBounds.fixedWidth, (float)currentBounds.fixedHeight, ColorUtil.WhiteArgbDouble);
    }
}