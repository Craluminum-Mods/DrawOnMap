using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace DrawOnMap;

public class DrawingSystem : ModSystem
{
    public static int currentColor = ColorUtil.WhiteArgb;

    public static double[] currentColorDoubles => ColorUtil.ToRGBADoubles(currentColor);
    public static float[] currentColorFloats => ColorUtil.ToRGBAFloats(currentColor);
    public static Vec4f currentColorVec4f => IntToRGBAVec4f(currentColor);

    public static byte[] currentColorBytes => ColorUtil.ToBGRABytes(currentColor);
    public static byte R => currentColorBytes[(int)EnumColorValue.R];
    public static byte G => currentColorBytes[(int)EnumColorValue.G];
    public static byte B => currentColorBytes[(int)EnumColorValue.B];
    public static byte A => currentColorBytes[(int)EnumColorValue.A];

    public static Vec4f IntToRGBAVec4f(int color)
    {
        Vec4f vecColor = new Vec4f();
        return ColorUtil.ToRGBAVec4f(color, ref vecColor);
    }

    public static void SetColor(byte newValue, EnumColorValue colorValue)
    {
        byte[] newBytes = currentColorBytes;
        newBytes[(int)colorValue] = newValue;
        currentColor = ColorUtil.ColorFromRgba(newBytes);
    }
}

public enum EnumColorValue
{
    R = 0,
    G = 1,
    B = 2,
    A = 3
}