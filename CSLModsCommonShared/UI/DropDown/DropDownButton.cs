using ColossalFramework.UI;
using CSLModsCommon.UI.Buttons;
using System;
using UnityEngine;

namespace CSLModsCommon.UI.DropDown; 
public class DropDownButton : NormalButton {
    private IDropDownLogic _logic;
    private IDropDownPopup _popup;

    public bool CanWheel { get; set; }
    public bool ClampPopupToScreen { get; set; } = true;
    public DrpDownPopupPosition ListPosition { get; set; } = DrpDownPopupPosition.Below;
    public Func<DropDownButton, IDropDownPopup> PopupFactory { get; set; }

    protected virtual void SetPopupPosition() {
        if (_popup == null) return;
        var popupComp = _popup.Self;
        if (popupComp == null) return;

        var popupParent = popupComp.parent;
        if (popupParent == null) return;

        var dropdownAbs = absolutePosition;

        Vector3 desiredWorldPos;
        var uiView = GetUIView();

        switch (ListPosition) {
            case DrpDownPopupPosition.Below:
                desiredWorldPos = new Vector3(dropdownAbs.x, dropdownAbs.y + height, 0f);
                break;
            case DrpDownPopupPosition.Above:
                desiredWorldPos = new Vector3(dropdownAbs.x, dropdownAbs.y - popupComp.height, 0f);
                break;
            case DrpDownPopupPosition.Automatic:
                var dropdownBottomY = dropdownAbs.y + height;
                var placeAbove = dropdownBottomY + popupComp.height > uiView.fixedHeight;
                desiredWorldPos = placeAbove
                    ? new Vector3(dropdownAbs.x, dropdownAbs.y - popupComp.height, 0f)
                    : new Vector3(dropdownAbs.x, dropdownAbs.y + height, 0f);
                break;
            default:
                desiredWorldPos = new Vector3(dropdownAbs.x, dropdownAbs.y + height, 0f);
                break;
        }

        var parentAbs = popupParent.absolutePosition;
        var relative = new Vector3(
            desiredWorldPos.x - parentAbs.x,
            desiredWorldPos.y - parentAbs.y,
            desiredWorldPos.z - parentAbs.z
        );

        popupComp.relativePosition = relative;

        if (ClampPopupToScreen) {
            var worldPos = popupComp.absolutePosition;
            var clampedX = Mathf.Clamp(worldPos.x, 0f, uiView.fixedWidth - popupComp.width);
            var clampedY = Mathf.Clamp(worldPos.y, 0f, uiView.fixedHeight - popupComp.height);
            popupComp.absolutePosition = new Vector3(clampedX, clampedY, worldPos.z);
        }
    }

    public override void Update() {
        base.Update();
        CheckForPopupClose();
    }

    public override void OnDestroy() {
        if (_popup != null) {
            _popup.OnItemClicked -= PopupItemClicked;
            Destroy(_popup.Self.gameObject);
            _popup = null;
        }

        base.OnDestroy();
    }

    protected override void OnMouseWheel(UIMouseEventParameter p) {
        if (CanWheel) {
            if (_logic == null || _logic.VisibleLabels.Count == 0)
                return;

            var dir = Math.Sign(p.wheelDelta);
            var newIndex = Mathf.Clamp(_logic.SelectedIndex - dir, 0, _logic.VisibleLabels.Count - 1);

            _logic.SelectIndex(newIndex);
            p.Use();
        }

        base.OnMouseWheel(p);
    }

    protected override void OnClick(UIMouseEventParameter p) {
        base.OnClick(p);
        if (p.used)
            return;
        if (_popup != null && _popup.Self.isVisible) {
            p.Use();
            HidePopup();
        }
        else {
            p.Use();
            OpenPopup();
        }
    }

    public void BindLogic(IDropDownLogic logic) {
        _logic = logic;
        _logic.ItemEnabledChanged += () => _popup?.Refresh();
        _logic.SelectionChanged += (index, label) => {
            _popup?.SetItemHighLight(index);
            UpdateLabel(label);
        };
        if (_logic.SelectedIndex >= 0) UpdateLabel(_logic.VisibleLabels[_logic.SelectedIndex]);
    }

    public void OpenPopup() {
        EnsurePopup();
        if (_popup == null) return;

        _popup.Show(_logic.SelectedIndex);
        SetPopupPosition();
    }

    public void HidePopup() => _popup?.HidePopup();

    private void EnsurePopup() {
        if (_popup != null) return;
        if (PopupFactory == null) return;
        _popup = PopupFactory(this);
        if (_popup != null) _popup.OnItemClicked += PopupItemClicked;
    }

    private void UpdateLabel(string text) => Text = text;

    private void PopupItemClicked(int index) {
        _logic.SelectIndex(index);
        HidePopup();
    }

    private void CheckForPopupClose() {
        if (_popup == null || !Input.GetMouseButtonDown(0)) return;

        if (!_popup.Self.isVisible) return;

        var camera = GetCamera();
        var ray = camera.ScreenPointToRay(Input.mousePosition);
        if (_popup.Raycast(ray)) return;

        if (Raycast(ray))
            return;

        HidePopup();
    }
}