using CSLModsCommon.UI.Containers;
using System.Collections.Generic;
using UnityEngine;

namespace CSLModsCommon.UI.Buttons.RadioButtons;

public class RadioGroup : LiteContainer {
    public List<RadioButton> Buttons { get; private set; }

    public override void Awake() {
        base.Awake();
        size = new Vector2(200, 30);
        _autoLayout = true;
        _autoFitChildrenVertically = true;
        Buttons = new List<RadioButton>();
    }

    public RadioButton AddOption(string label) {
        var radioButton = AddUIComponent<RadioButton>();
        radioButton.SetText(label);
        radioButton.width = width;
        Buttons.Add(radioButton);
        return radioButton;
    }
}