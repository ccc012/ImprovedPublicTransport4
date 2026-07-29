using UnityEngine;

namespace CSLModsCommon.UI.Extensions;

public static class Color32Extensions {
    public static bool EqualsColor(this Color32 color1, Color32 color2) => color1.r == color2.r && color1.g == color2.g && color1.b == color2.b && color1.a == color2.a;

    public static Color32 CopyFrom(this ref Color32 target, Color32 source) {
        target.r = source.r;
        target.g = source.g;
        target.b = source.b;
        target.a = source.a;
        return target;
    }

    public static Color32 SetAlpha(this ref Color32 target, byte alpha) {
        target.a = alpha;
        return target;
    }
}