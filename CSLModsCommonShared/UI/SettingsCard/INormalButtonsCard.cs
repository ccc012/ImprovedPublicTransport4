using System;
using CSLModsCommon.Collections;
using CSLModsCommon.UI.Buttons;

namespace CSLModsCommon.UI.SettingsCard;

public interface INormalButtonsCard {
    CSLModsCommon.Collections.IReadOnlyDictionary<string, NormalButton> Buttons { get; }
    NormalButton RegisterButton(string text, Action<NormalButton> onClicked = null, Action<NormalButton> onRender = null);
    NormalButton RegisterButton(string text, Action onClicked = null, Action<NormalButton> onRender = null);
}