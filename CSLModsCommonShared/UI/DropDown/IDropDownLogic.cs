using System;
using System.Collections.Generic;

namespace CSLModsCommon.UI.DropDown;

public interface IDropDownLogic {
    event Action ItemEnabledChanged;
    event Action<int, string> SelectionChanged;

    IList<string> VisibleLabels { get; }
    int SelectedIndex { get; }
    void SelectIndex(int index);
}