using ColossalFramework.UI;
using UnityEngine;

namespace CSLModsCommon.UI.Containers;

public class AutoFitScrollContainer : ScrollContainer {
    private bool _autoFitToContents;
    private Vector2 _maxAutoSize = new(float.MaxValue, float.MaxValue);

    public bool AutoFitToContents {
        get => _autoFitToContents;
        set {
            if (_autoFitToContents == value) return;
            _autoFitToContents = value;
            if (value) FitToContentsWithLimit();

            Invalidate();
        }
    }

    public Vector2 MaxAutoSize {
        get => _maxAutoSize;
        set {
            if (_maxAutoSize == value) return;
            _maxAutoSize = value;
            if (_autoFitToContents) FitToContentsWithLimit();

            Invalidate();
        }
    }

    public void FitToContentsWithLimit() {
        if (isLayoutSuspended || !AutoFitToContents) return;
        if (childCount == 0) {
            size = new Vector2(AutoLayoutPadding.Horizontal, AutoLayoutPadding.Vertical);
            return;
        }

        var contentSize = Vector2.zero;
        for (var i = 0; i < childCount; i++) {
            var uiComponent = m_ChildComponents[i];
            if (uiComponent == null) continue;
            var bottomRight = (Vector2)uiComponent.relativePosition + uiComponent.size;
            contentSize = Vector2.Max(contentSize, bottomRight);
        }

        contentSize += new Vector2(ScrollPadding.Right, ScrollPadding.Bottom);
        var newSize = Vector2.Min(contentSize, _maxAutoSize);
        if (size != newSize) size = newSize;

        UpdateScrollbars();
        Invalidate();
    }

    protected override void OnComponentAdded(UIComponent child) {
        base.OnComponentAdded(child);
        AttachEvents(child);
        // if (_autoFitToContents) {
        //     FitToContentsWithLimit();
        // }
    }

    protected override void OnComponentRemoved(UIComponent child) {
        base.OnComponentRemoved(child);
        DetachEvents(child);
        // if (_autoFitToContents) {
        //     FitToContentsWithLimit();
        // }
    }

    private void AttachEvents(UIComponent child) {
        child.eventVisibilityChanged += ChildIsVisibleChanged;
        child.eventSizeChanged += ChildInvalidated;
    }

    private void ChildInvalidated(UIComponent component, Vector2 value) => FitToContentsWithLimit();

    private void DetachEvents(UIComponent child) {
        child.eventVisibilityChanged -= ChildIsVisibleChanged;
        child.eventSizeChanged -= ChildInvalidated;
    }

    private void ChildIsVisibleChanged(UIComponent child, bool value) => FitToContentsWithLimit();

    protected override void OnSizeChanged() {
        base.OnSizeChanged();
        if (_verticalScrollbar is not null) _verticalScrollbar.height = height;
    }
}