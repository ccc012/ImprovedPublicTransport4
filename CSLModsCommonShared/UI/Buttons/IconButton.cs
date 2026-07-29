using UnityEngine;

namespace CSLModsCommon.UI.Buttons;

public abstract class IconButton : UIStateElement {
    public override Vector2 CalculateMinimumSize() {
        var backgroundSprite = GetBgSprite();
        if (backgroundSprite == null) return base.CalculateMinimumSize();

        var border = backgroundSprite.border;
        if (border.horizontal > 0 || border.vertical > 0) return Vector2.Max(base.CalculateMinimumSize(), new Vector2(border.horizontal, border.vertical));

        return base.CalculateMinimumSize();
    }
}