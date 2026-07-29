using ColossalFramework.UI;
using CSLModsCommon.UI.Buttons;
using CSLModsCommon.UI.Containers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CSLModsCommon.UI.Tab; 
public class TabBar : LiteContainer {
    public event UIElementEventHandler<string> TabClicked;

    public List<TabButton> Buttons { get; } = new();

    public override void Awake() {
        base.Awake();
        _autoLayout = true;
        _direction = FlexDirection.Row;
        _layoutPadding.SetAll(4);
        _columnGap = 4;
    }

    public TabButton AddTab(string id, string text, Action<TabButton> onAddButton = null) {
        var btn = AddUIComponent<TabButton>();
        btn.Text = text;
        btn.TextPadding.SetLeft(4).SetRight(4);
        onAddButton?.Invoke(btn);
        btn.stringUserData = id;
        btn.eventClick += OnButtonClick;
        Buttons.Add(btn);
        RepositionButtons();
        return btn;
    }

    public void MoveTabButton(int fromIndex, int toIndex) {
        if (fromIndex < 0 || fromIndex >= Buttons.Count ||
            toIndex < 0 || toIndex >= Buttons.Count || fromIndex == toIndex)
            return;

        var button = Buttons[fromIndex];
        Buttons.RemoveAt(fromIndex);
        Buttons.Insert(toIndex, button);
        RepositionButtons();
    }

    public TabButton GetButton(int index) {
        if (index < 0 || index >= Buttons.Count) return null;
        return Buttons[index];
    }

    private void OnButtonClick(UIComponent component, UIMouseEventParameter eventParam) {
        var index = Buttons.FindIndex(tabButton => component.stringUserData == tabButton.stringUserData);
        if (index == -1) return;
        SetSelected(component.stringUserData);
        TabClicked?.Invoke(Buttons[index].stringUserData);
    }

    public void SetSelected(string id) {
        foreach (var b in Buttons) b.IsSelected = id == b.stringUserData;
    }

    public void RepositionButtons() {
        if (Buttons.Count == 0) return;

        var totalWidth = width - _layoutPadding.Horizontal;
        var buttonWidth = (totalWidth - _columnGap * (Buttons.Count - 1)) / Buttons.Count;
        var buttonHeight = height - _layoutPadding.Vertical;
        for (var i = 0; i < Buttons.Count; i++) {
            var button = Buttons[i];
            button.size = new Vector2(buttonWidth, buttonHeight);
        }
    }
}