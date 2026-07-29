using ColossalFramework.UI;
using CSLModsCommon.UI.Labels;
using System;

namespace CSLModsCommon.UI.Dialogs;

public abstract class SimpleDialog : DialogBase {
    public virtual Label AddContent(string title, string content, Action<Label> onContentLabelAdded = null) {
        TitleText = title;
        return AddContent(content, onContentLabelAdded);
    }

    public virtual Label AddContent(string content, Action<Label> onContentLabelAdded = null) {
        var label = AddLabelElement();
        label.Text = content;
        onContentLabelAdded?.Invoke(label);
        return label;
    }

    protected Label AddLabelElement() {
        var label = ContentContainer.AddUIComponent<Label>();
        label.width = ElementWidth;
        label.SizeMode = TextSizeMode.AutoHeight;
        label.TextHorizontalAlignment = UIHorizontalAlignment.Center;
        label.WordWrap = true;
        return label;
    }
}