using CSLModsCommon.UI.Containers;
using UnityEngine;

namespace CSLModsCommon.UI.SettingsCard;

public class EmptyFlexContainerCard : SettingsCardBase<FlexContainer> {
    public override void Awake() {
        base.Awake();
        Control = AddUIComponent<FlexContainer>();
        size = new Vector2(10, 10);
    }
}