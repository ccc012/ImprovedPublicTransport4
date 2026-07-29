using ColossalFramework.UI;
using CSLModsCommon.UI.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CSLModsCommon.UI.DropDown;

public class FixedDropDownPopup : LiteContainer, IDropDownPopup {
    public event UIElementEventHandler<int> OnItemClicked;

    private readonly List<DropDownPopupButton> _buttons = new();
    private IList<string> _items = new List<string>();

    public UIComponent Self => this;
    public IList<DropDownPopupButton> Buttons => _buttons;

    public override void Awake() {
        base.Awake();
        m_Size = new Vector2(100, 500);
        _autoLayout = true;
        m_IsVisible = false;
        _autoFitChildrenVertically = true;
        _autoFitChildrenHorizontally = true;
        m_CanFocus = true;
    }

    protected override void OnKeyDown(UIKeyEventParameter p) {
        base.OnKeyDown(p);
        if (p.keycode is KeyCode.Escape or KeyCode.Space or KeyCode.Return) HidePopup();
    }

    public void BindItems<T, TPopupButton>(IList<string> items, IList<DropDownItem<T>> itemStates, Action<DropDownPopupButton> onAddComponent = null) where TPopupButton : DropDownPopupButton {
        _buttons.Clear();
        _items = items;
        float maxWidth = 0;
        for (var i = 0; i < items.Count; i++) {
            var btn = AddUIComponent<TPopupButton>();
            btn.TextHorizontalAlignment = UIHorizontalAlignment.Left;
            btn.Text = items[i];
            btn.eventClick += OnButtonClicked;
            _buttons.Add(btn);
            maxWidth = Mathf.Max(maxWidth, btn.width);
            onAddComponent?.Invoke(btn);
            itemStates[i].EnabledChanged += item => btn.isVisible = item.IsEnabled;
        }

        foreach (var btn in _buttons.Where(btn => btn.width < maxWidth)) {
            btn.AutoWidth = false;
            btn.width = maxWidth;
        }
    }

    public void SetButtonsStyle(Action<DropDownPopupButton> buttonsAction) {
        float maxWidth = 0;
        for (var i = 0; i < _items.Count; i++) {
            var btn = _buttons[i];
            buttonsAction?.Invoke(btn);
            maxWidth = Mathf.Max(maxWidth, btn.width);
        }

        foreach (var btn in _buttons.Where(btn => btn.width < maxWidth)) {
            btn.AutoWidth = false;
            btn.width = maxWidth;
        }
    }

    public void SetItemHighLight(int selectedIndex) {
        for (var i = 0; i < _buttons.Count; i++)
            if (selectedIndex == i) {
                _buttons[i].State = UIState.Focused;
                _buttons[i].IsSelected = true;
            }
            else {
                _buttons[i].State = UIState.Normal;
                _buttons[i].IsSelected = false;
            }
    }

    public void Show(int selectedIndex) {
        SetItemHighLight(selectedIndex);
        isVisible = true;
        Focus();
    }

    public void Refresh() {
        for (var i = 0; i < _buttons.Count; i++) _buttons[i].Text = _items[i];
    }

    public void RaiseItemClicked(int index) {
        if (index >= 0 && index < _buttons.Count) OnItemClicked?.Invoke(index);
    }

    public void HidePopup() {
        isVisible = false;
        Unfocus();
    }

    private void OnButtonClicked(UIComponent component, UIMouseEventParameter p) {
        var index = _buttons.IndexOf(component as DropDownPopupButton);
        if (index < 0) return;
        Refresh();
        OnItemClicked?.Invoke(index);
    }
}