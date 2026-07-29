using UnityEngine;

namespace CSLModsCommon.UI.OptionsPanel; 
public struct OptionsPanelLayout {
    public static readonly Vector2 Size = new(764, 773);
    public static readonly float Padding = 16;
    public static float SectionWidth => Size.x - 2 * Padding;
    private const float TabHeight = 30;
    public static Vector2 TabSize => new(SectionWidth, TabHeight);
    public static Vector2 ContainerSize => new(SectionWidth, Size.y - 2 * Padding - TabHeight - 10);
}