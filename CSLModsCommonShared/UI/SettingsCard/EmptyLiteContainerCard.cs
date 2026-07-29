using CSLModsCommon.UI.Containers;
using UnityEngine;

namespace CSLModsCommon.UI.SettingsCard;

public class EmptyLiteContainerCard : SettingsCardBase<LiteContainer> {
    public override void Awake() {
        base.Awake();
        Control = AddUIComponent<LiteContainer>();
        size = new Vector2(10, 10);
    }
}