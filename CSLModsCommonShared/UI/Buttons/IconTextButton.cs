using ColossalFramework.UI;
using UnityEngine;

namespace CSLModsCommon.UI.Buttons;

/// <summary>A <see cref="NormalButton"/> with a left-aligned foreground icon and text pushed clear of it.</summary>
public class IconTextButton : NormalButton {
    private const float DefaultMargin = 10f;

    protected float _iconSize = 20f;
    protected float _iconTextGap = 6f;

    public float IconSize {
        get => _iconSize;
        set {
            if (Mathf.Approximately(value, _iconSize)) return;
            _iconSize = value;
            UpdateLayout();
        }
    }

    public float IconTextGap {
        get => _iconTextGap;
        set {
            if (Mathf.Approximately(value, _iconTextGap)) return;
            _iconTextGap = value;
            UpdateLayout();
        }
    }

    public override void Awake() {
        base.Awake();
        RenderFg = true;
        FgSpriteMode = ForegroundSpriteMode.Custom;
        FgHorizontalAlignment = UIHorizontalAlignment.Left;
        FgSpritePadding.SetLeft((int)DefaultMargin);
        UpdateLayout();
    }

    public IconTextButton SetIcon(UITextureAtlas atlas, string spriteName) {
        FgAtlas = atlas;
        FgSprites.SetValues(spriteName);
        return this;
    }

    private void UpdateLayout() {
        FgCustomSize = new Vector2(_iconSize, _iconSize);
        TextPadding.SetLeft((int)(FgSpritePadding.Left + _iconSize + _iconTextGap));
    }
}
