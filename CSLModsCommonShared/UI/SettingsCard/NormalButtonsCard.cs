using System;
using System.Collections.Generic;
using CSLModsCommon.Collections;
using CSLModsCommon.Extension;
using CSLModsCommon.UI.Buttons;
using CSLModsCommon.UI.Containers;

namespace CSLModsCommon.UI.SettingsCard;

public class NormalButtonsCard : SettingsCardBase<LiteContainer>, INormalButtonsCard {
    private Dictionary<string, NormalButton> _buttons;

    public CSLModsCommon.Collections.IReadOnlyDictionary<string, NormalButton> Buttons { get; private set; }

    public override void Awake() {
        base.Awake();
        _control = AddUIComponent<LiteContainer>();
        _control.AutoLayout = true;
        _control.AutoFitChildrenHorizontally = true;
        _control.AutoFitChildrenVertically = true;
        _control.Direction = FlexDirection.Row;
        _buttons = new Dictionary<string, NormalButton>();
        Buttons = _buttons.AsReadOnly();
    }

    public NormalButton RegisterButton(string text, Action onClicked = null, Action<NormalButton> onRender = null) => RegisterButton(text, (_) => onClicked?.Invoke(), onRender);

    public NormalButton RegisterButton(string text, Action<NormalButton> onClicked = null, Action<NormalButton> onRender = null) {
        var button = _control.AddUIComponent<NormalButton>();
        onRender?.Invoke(button);
        button.Text = text;
        button.eventClicked += (_, _) => onClicked?.Invoke(button);
        _buttons.Add(text, button);
        return button;
    }
}