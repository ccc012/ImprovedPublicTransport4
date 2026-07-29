using ColossalFramework.UI;
using UnityEngine;

namespace CSLModsCommon.UI.DropDown;

public interface IDropDownPopup {
    event UIElementEventHandler<int> OnItemClicked;

    UIComponent Self { get; }
    void Show(int selectedIndex);
    void HidePopup();
    void SetItemHighLight(int index);
    void Refresh();
    bool Raycast(Ray ray);
    void RaiseItemClicked(int index);
}