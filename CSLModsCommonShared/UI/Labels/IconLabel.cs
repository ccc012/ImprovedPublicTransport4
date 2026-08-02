using ColossalFramework.UI;
using CSLModsCommon.UI.Containers;
using UnityEngine;

namespace CSLModsCommon.UI.Labels;

/// <summary>A horizontal icon + text row, e.g. for status rows in dialogs.</summary>
public class IconLabel : LiteContainer {
    public Sprite IconElement { get; private set; }

    public Label TextElement { get; private set; }

    public string Text {
        get => TextElement?.Text;
        set {
            if (TextElement != null) TextElement.Text = value;
        }
    }

    public Vector2 IconSize {
        get => IconElement?.size ?? Vector2.zero;
        set {
            if (IconElement == null) return;
            IconElement.size = value;
        }
    }

    public override void Awake() {
        base.Awake();
        size = new Vector2(100, 24);
        _direction = FlexDirection.Row;
        _autoLayout = true;
        _columnGap = 6;
        _autoFitChildrenHorizontally = true;

        IconElement = AddUIComponent<Sprite>();
        IconElement.size = new Vector2(20, 20);

        TextElement = AddUIComponent<Label>();
        TextElement.SizeMode = TextSizeMode.AutoSize;
        TextElement.TextVerticalAlignment = UIVerticalAlignment.Middle;
    }

    public IconLabel SetIcon(UITextureAtlas atlas, string spriteName) {
        if (IconElement == null) return this;
        IconElement.Atlas = atlas;
        IconElement.SpriteName = spriteName;
        return this;
    }
}
