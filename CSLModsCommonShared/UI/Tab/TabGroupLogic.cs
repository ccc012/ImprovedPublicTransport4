using ColossalFramework.UI;
using CSLModsCommon.UI.Buttons;
using System;
using System.Collections.Generic;

namespace CSLModsCommon.UI.Tab; 
public class TabGroupLogic {
    private readonly Dictionary<string, UIComponent> _pages = new();

    public TabBar TabBar { get; private set; }

    private TabGroupLogic() { }

    public static TabGroupLogic Create() => new TabGroupLogic();

    public TabGroupLogic BindingTabBar(TabBar tabBar) {
        TabBar = tabBar;
        TabBar.TabClicked += OnTabClicked;
        return this;
    }

    public TabGroupLogic AddTab(string id, string text, UIComponent page, Action<TabButton> onTabButtonAdded = null) {
        if (TabBar is null) return this;
        TabBar.AddTab(id, text, onTabButtonAdded);
        if (page is null) return this;
        _pages[id] = page;
        page.isVisible = false;
        return this;
    }

    public TabGroupLogic SelectPageOnly(string id) {
        if (TabBar is null) return this;
        foreach (var page in _pages) page.Value.isVisible = page.Key == id;

        return this;
    }

    public TabGroupLogic SelectPage(int index) {
        if (TabBar is null || index < 0 || index >= TabBar.Buttons.Count) return this;

        var button = TabBar.GetButton(index);
        if (button == null) return this;

        var id = button.stringUserData;

        return SelectPage(id);
    }

    public TabGroupLogic SelectPage(string id) {
        SelectPageOnly(id);
        if (TabBar is null) return this;
        TabBar.SetSelected(id);
        return this;
    }

    private void OnTabClicked(string id) => SelectPageOnly(id);
}