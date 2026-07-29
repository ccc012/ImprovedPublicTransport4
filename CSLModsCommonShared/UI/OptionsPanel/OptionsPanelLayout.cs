using UnityEngine;

namespace CSLModsCommon.UI.OptionsPanel; 
public struct OptionsPanelLayout {
    public static readonly Vector2 Size = new(764, 773);
    public static readonly float Padding = 16;
    public static float SectionWidth => Size.x - 2 * Padding;
    // Tall enough for two lines of wrapped tab text - some languages (and longer English
    // captions) don't fit a single line at this panel's tab width.
    private const float TabHeight = 44;
    public static Vector2 TabSize => new(SectionWidth, TabHeight);
    public static Vector2 ContainerSize => new(SectionWidth, Size.y - 2 * Padding - TabHeight - 10);
}