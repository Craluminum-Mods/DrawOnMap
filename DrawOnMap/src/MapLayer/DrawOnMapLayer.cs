﻿using Cairo;
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

    private int brushSize = 1;

    private string[] drawingTools
    {
        get
        {
            string[] defaultToolsList = new string[] { "paintbrush", "eraser" };
            string[] debugToolsList = new string[] { "wpX" };
            return Debug() ? debugToolsList.Concat(defaultToolsList).ToArray() : defaultToolsList;
        }
    }

    private bool Debug() => false;

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
            recompose = false;
            composer?.ReCompose();
        }
    }

    public override void Dispose()
    {
        composer?.Dispose();

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

        if (canDraw)
        {
            int steps = Math.Max(Math.Abs(args.DeltaX), Math.Abs(args.DeltaY));
            float stepX = (float)args.DeltaX / steps;
            float stepY = (float)args.DeltaY / steps;

            for (int i = 0; i <= steps; i++)
            {
                int x = (int)(args.X - args.DeltaX + (i * stepX));
                int y = (int)(args.Y - args.DeltaY + (i * stepY));

                if (selectedToolName == "paintbrush")
                {
                    Draw(x, y, mapElem, brushSize);
                }

                if (selectedToolName == "eraser")
                {
                    Erase(x, y, mapElem, brushSize);
                }
            }
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

    private void Draw(int x, int y, GuiElementMap mapElem, int brushSize = 1)
    {
        Vec2f viewPos = new Vec2f(x, y);
        viewPos.X = viewPos.X - (float)mapElem.Bounds.renderX;
        viewPos.Y = viewPos.Y - (float)mapElem.Bounds.renderY;

        Vec3d worldPos = new Vec3d();
        mapElem.TranslateViewPosToWorldPos(viewPos, ref worldPos);
        BlockPos blockPos = new BlockPos(worldPos.AsBlockPos.X, 0, worldPos.AsBlockPos.Z);

        Vec4f color = DrawingSystem.currentColorVec4f;

        if (brushSize == 1)
        {
            SetPixel(blockPos, color);
            return;
        }

        capi.World.BlockAccessor.WalkBlocks(
            minPos: blockPos.AddCopy(0, 0, 0),
            maxPos: blockPos.AddCopy(brushSize - 1, 0, brushSize - 1),
            onBlock: (block, x, y, z) =>
        {
            BlockPos newBlockPos = new BlockPos(x, y, z);
            SetPixel(newBlockPos, color);
        });
    }

    private void Erase(int x, int y, GuiElementMap mapElem, int brushSize = 1)
    {
        Vec2f viewPos = new Vec2f(x, y);
        viewPos.X = viewPos.X - (float)mapElem.Bounds.renderX;
        viewPos.Y = viewPos.Y - (float)mapElem.Bounds.renderY;

        Vec3d worldPos = new Vec3d();
        mapElem.TranslateViewPosToWorldPos(viewPos, ref worldPos);
        BlockPos blockPos = new BlockPos(worldPos.AsBlockPos.X, 0, worldPos.AsBlockPos.Z);

        if (brushSize == 1)
        {
            RemovePixel(blockPos);
            return;
        }

        capi.World.BlockAccessor.WalkBlocks(
            minPos: blockPos.AddCopy(0, 0, 0),
            maxPos: blockPos.AddCopy(brushSize - 1, 0, brushSize - 1),
            onBlock: (block, x, y, z) =>
        {
            RemovePixel(new BlockPos(x, y, z));
        });
    }

    private void SetPixel(BlockPos blockPos, Vec4f color)
    {
        if (loadedMapData.TryGetValue(blockPos, out DrawOnMapComponent value))
        {
            value.Color = color;
        }
        else
        {
            loadedMapData[blockPos] = new DrawOnMapComponent(capi, blockPos, color);
        }
    }

    private void RemovePixel(BlockPos blockPos)
    {
        loadedMapData.Remove(blockPos);
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

        ElementBounds sliderB = sliderRTextBounds.CopyOffsetedSibling(offsetY, fixedDeltaWidth: indent * 3);
        ElementBounds sliderG = sliderB.CopyOffsetedSibling(0, offsetY);
        ElementBounds sliderR = sliderG.CopyOffsetedSibling(0, offsetY);
        ElementBounds sliderA = sliderR.CopyOffsetedSibling(0, offsetY);

        ElementBounds drawBounds = sliderATextBounds.CopyOffsetedSibling(0, offsetY, fixedDeltaWidth: sliderA.fixedWidth + gap);

        ElementBounds brushSizeIconBounds = sliderATextBounds.CopyOffsetedSibling(0, offsetY * 2).WithFixedWidth(indent * 2);
        ElementBounds sliderBrushSizeBounds = brushSizeIconBounds.CopyOffsetedSibling(brushSizeIconBounds.fixedWidth).WithFixedSize((indent * 3) + gap, indent);

        ElementBounds dropdownIconBounds = sliderATextBounds.CopyOffsetedSibling(0, offsetY * 3);
        ElementBounds dropdownBounds = dropdownIconBounds.CopyOffsetedSibling(offsetY).WithFixedWidth(sliderB.fixedWidth);

        ElementBounds toolPickerBounds = dropdownIconBounds.CopyOffsetedSibling(0, offsetY).WithFixedWidth(dropdownIconBounds.fixedWidth);

        try
        {
            composer = guiDialogWorldMap.Composers[key] = capi.Gui.CreateCompo(key, dlgBounds)
                .AddShadedDialogBG(bgBounds, withTitleBar: false)
                .AddDialogTitleBar(Lang.Get("drawonmap:drawing"), () => guiDialogWorldMap.Composers[key].Enabled = false, font: CairoFont.WhiteSmallText())
            .BeginChildElements(bgBounds)
                .AddStaticText("B:", CairoFont.WhiteMediumText().WithFontSize(28).WithOrientation(EnumTextOrientation.Left), sliderBTextBounds)
                .AddStaticText("G:", CairoFont.WhiteMediumText().WithFontSize(28).WithOrientation(EnumTextOrientation.Left), sliderGTextBounds)
                .AddStaticText("R:", CairoFont.WhiteMediumText().WithFontSize(28).WithOrientation(EnumTextOrientation.Left), sliderRTextBounds)
                .AddStaticText("A:", CairoFont.WhiteMediumText().WithFontSize(28).WithOrientation(EnumTextOrientation.Left), sliderATextBounds)
                .AddSlider((newVal) => OnSlider(newVal, EnumColorValue.B), sliderB, "sliderB")
                .AddSlider((newVal) => OnSlider(newVal, EnumColorValue.G), sliderG, "sliderG")
                .AddSlider((newVal) => OnSlider(newVal, EnumColorValue.R), sliderR, "sliderR")
                .AddSlider((newVal) => OnSlider(newVal, EnumColorValue.A), sliderA, "sliderA")

                .AddDynamicText("", CairoFont.WhiteMediumText().WithFontSize(28), brushSizeIconBounds, key: "textBrushSize")
                .AddSlider(OnSliderBrushSize, sliderBrushSizeBounds, "sliderBrushSize")

                .AddDynamicCustomDraw(drawBounds, OnDrawColor)
                .AddInset(drawBounds)

                .AddStaticCustomDraw(dropdownIconBounds, OnDrawMouse)
                .AddDropDown(values, names, (int)buttonForDrawing, OnSelectionChanged, dropdownBounds)

                .AddToolListPicker(drawingTools, OnPickTool, toolPickerBounds, (int)toolPickerBounds.fixedWidth * 5, "toolPicker")
            .EndChildElements()
            .Compose();
        }
        catch (Exception) { }

        if (composer != null) composer.Enabled = false;
        composer?.GetSlider("sliderR").SetValues(currentValue: DrawingSystem.R, minValue: 0, maxValue: 255, step: 1);
        composer?.GetSlider("sliderG").SetValues(currentValue: DrawingSystem.G, minValue: 0, maxValue: 255, step: 1);
        composer?.GetSlider("sliderB").SetValues(currentValue: DrawingSystem.B, minValue: 0, maxValue: 255, step: 1);
        composer?.GetSlider("sliderA").SetValues(currentValue: DrawingSystem.A, minValue: 0, maxValue: 255, step: 1);
        composer?.GetSlider("sliderBrushSize").SetValues(currentValue: brushSize, minValue: 1, maxValue: 72, step: 1);
        composer?.GetDynamicText("textBrushSize").SetNewText(text: brushSize.ToString());
        composer?.IconListPickerSetValue("toolPicker", selectedTool);
    }

    private void OnPickTool(int toolId)
    {
        selectedTool = toolId;
        switch (selectedToolName)
        {
            case "wpX":
                loadedMapData.Clear();
                break;
        }
    }

    private void OnSelectionChanged(string code, bool selected)
    {
        EnumMouseButton newButton = buttonForDrawing;
        if (Enum.TryParse(code, out newButton))
        {
            buttonForDrawing = newButton;
        }
    }

    private bool OnSlider(int newValue, EnumColorValue colorValue)
    {
        DrawingSystem.SetColor((byte)newValue, colorValue);
        recompose = true;
        return true;
    }

    private bool OnSliderBrushSize(int newValue)
    {
        brushSize = newValue;
        composer?.GetDynamicText("textBrushSize").SetNewText(text: brushSize.ToString(), forceRedraw: true);
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