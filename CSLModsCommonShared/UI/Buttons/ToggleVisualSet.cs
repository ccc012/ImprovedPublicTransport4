using ColossalFramework.UI;
using CSLModsCommon.UI.Rendering;

namespace CSLModsCommon.UI.Buttons;

public struct ToggleVisualSet {
    public SpriteStateRenderer BgSprites { get; }
    public SpriteStateRenderer FgSprites { get; }
    public ColorStateRenderer BgColors { get; }
    public ColorStateRenderer FgColors { get; }

    public ToggleVisualSet(UIComponent owner) {
        BgSprites = new SpriteStateRenderer(owner);
        FgSprites = new SpriteStateRenderer(owner);
        BgColors = new ColorStateRenderer(owner);
        FgColors = new ColorStateRenderer(owner);
    }
}