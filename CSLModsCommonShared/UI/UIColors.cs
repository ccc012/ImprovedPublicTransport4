using CSLModsCommon.UI.Rendering;
using UnityEngine;

namespace CSLModsCommon.UI; 
public static class UIColors {
    public static Color32 Transparency { get; } = new(255, 255, 255, 0);
    public static Color32 White { get; } = new(255, 255, 255, 255);
    public static Color32 White90 { get; } = new(230, 230, 230, 255);
    public static Color32 White80 { get; } = new(204, 204, 204, 255);
    public static Color32 White70 { get; } = new(179, 179, 179, 255);
    public static Color32 White60 { get; } = new(153, 153, 153, 255);
    public static Color32 White50 { get; } = new(128, 128, 128, 255);
    public static Color32 White40 { get; } = new(102, 102, 102, 255);
    public static Color32 BlueNormal { get; } = new(10, 92, 186, 255);
    public static Color32 BlueHovered { get; } = new(40, 126, 226, 255);
    public static Color32 BluePressed { get; } = new(18, 102, 200, 255);
    public static Color32 BlueFocused { get; } = BlueNormal;
    public static Color32 BlueDisabled { get; } = new(22, 42, 76, 255);
    public static Color32 GreenNormal { get; } = new(76, 164, 50, 255);
    public static Color32 GreenHovered { get; } = new(86, 184, 58, 255);
    public static Color32 GreenPressed { get; } = new(72, 154, 46, 255);
    public static Color32 GreenFocused => GreenNormal;
    public static Color32 GreenDisabled { get; } = new(44, 86, 44, 255);

    public static Color32 YellowNormal => new(188, 120, 6, 255);
    public static Color32 YellowHovered => new(198, 140, 16, 255);
    public static Color32 YellowPressed => new(178, 110, 0, 255);
    public static Color32 YellowFocused => GreenNormal;
    public static Color32 YellowDisabled => new(128, 100, 0, 255);

    public static Color32 GroupBgNormal { get; } = new(70, 94, 114, 255);
    public static Color32 GroupBgHovered { get; } = new(76, 102, 122, 255);
    public static Color32 GroupFgNormal => new(146, 162, 174, 255);
    public static Color32 BgElementNormal { get; } = new(92, 120, 140, 255);
    public static Color32 BgElementHovered { get; } = new(104, 134, 156, 255);
    public static Color32 BgElementPressed { get; } = new(86, 114, 132, 255);
    public static Color32 BgElementFocused => GreenNormal;
    public static Color32 BgElementDisabled { get; } = new(52, 70, 86, 255);

    public static Color32 GroupBg1 { get; } = new(36, 48, 62, 255);
    public static Color32 GroupFg1 { get; } = new(78, 86, 100, 255);

    public static Color32 Bg1ElementNormal { get; } = new(56, 70, 86, 255);
    public static Color32 Bg1ElementHovered { get; } = new(70, 84, 102, 255);
    public static Color32 Bg1ElementPressed { get; } = new(80, 94, 112, 255);
    public static Color32 Bg1ElementFocused => BlueNormal;
    public static Color32 Bg1ElementDisabled { get; } = new(46, 58, 70, 255);

    public static Color32 ToggleFgNormal { get; } = new(220, 220, 220, 255);
    public static Color32 ToggleFgDisabled { get; } = new(110, 110, 110, 255);

    public static Color32 RedNormal { get; } = new(150, 24, 26, 255);

    public static GreenColorSetter GreenColors { get; } = new();
    public static BlueColorSetter BlueColors { get; } = new();
    public static YellowColorSetter YellowColors { get; } = new();
    public static MajorTextElementColorSetter MajorTextElementColors { get; } = new();
    public static MinorTextElementColorSetter MinorTextElementColors { get; } = new();
    public static BgElementColorsSetter BgElementColors { get; } = new();
    public static Bg1ElementColorsSetter Bg1ElementColors { get; } = new();
    public static ControlPanelMinorTextElementColorSetter ControlPanelMinorTextElementColors { get; } = new();

    public class Bg1ElementColorsSetter : IColorSetter {
        public void ApplyColors(ColorStateRenderer render) => render.SetValues(Bg1ElementNormal, Bg1ElementHovered, Bg1ElementPressed, Bg1ElementFocused, Bg1ElementDisabled);
    }

    public class BgElementColorsSetter : IColorSetter {
        public void ApplyColors(ColorStateRenderer render) => render.SetValues(BgElementNormal, BgElementHovered, BgElementPressed, BgElementFocused, BgElementDisabled);
    }

    public class ControlPanelMinorTextElementColorSetter : IColorSetter {
        public void ApplyColors(ColorStateRenderer render) {
            render.SetValues(White80);
            render.DisabledColor = White50;
        }
    }

    public class MinorTextElementColorSetter : IColorSetter {
        public void ApplyColors(ColorStateRenderer render) => render.SetValues(White80, White80, White80, White80, White40);
    }

    public class MajorTextElementColorSetter : IColorSetter {
        public void ApplyColors(ColorStateRenderer render) => render.DisabledColor = White60;
    }

    public class YellowColorSetter : IColorSetter {
        public void ApplyColors(ColorStateRenderer render) => render.SetValues(YellowNormal, YellowHovered, YellowPressed, YellowFocused, YellowDisabled);
    }

    public class BlueColorSetter : IColorSetter {
        public void ApplyColors(ColorStateRenderer render) => render.SetValues(BlueNormal, BlueHovered, BluePressed, BlueFocused, BlueDisabled);
    }

    public class GreenColorSetter : IColorSetter {
        public void ApplyColors(ColorStateRenderer render) => render.SetValues(GreenNormal, GreenHovered, GreenPressed, GreenFocused, GreenDisabled);
    }
}