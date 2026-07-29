using ColossalFramework.UI;
using CSLModsCommon.UI.Labels;

namespace CSLModsCommon.UI.SettingsCard; 
public interface ISettingsCard {
    UIComponent Self { get; }
    Label HeaderElement { get; }
    Label DescriptionElement { get; }
    Padding LayoutPadding { get; }
    UIComponent GetControl();
    void Arrange();
    bool RenderFg { get; set; }
}