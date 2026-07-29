using ColossalFramework.UI;
using CSLModsCommon.UI;
using System;

namespace CSLModsCommon.Manager;

public abstract class PanelManagerBase : ManagerBase {
    private bool _isVisible;
    private Type _panelType;

    public event UIElementEventHandler<UIComponent> PanelCreated;
    public event UIElementEventHandler BeforePanelDestroyed;
    public event UIElementEventHandler PanelDestroyed;
    public event UIElementEventHandler<UIComponent, bool> PanelVisibleChanged;

    public virtual Type PanelType => _panelType ??= ResisterPanelType();
    public virtual bool IsCreated { get; protected set; }
    public virtual bool ShowWhenCreated => true;

    public virtual bool IsVisible {
        get => _isVisible;
        protected set {
            if (_isVisible == value) return;
            _isVisible = value;
            OnPanelVisibleChanged(value);
        }
    }

    public UIComponent TargetPanel { get; protected set; }
    public bool HasPanel => TargetPanel != null;

    public abstract Type ResisterPanelType();
    public abstract UIComponent CreatePanel();
    public abstract void DestroyPanel(Action afterDestroyAction = null);
    public abstract void ShowPanel();
    public abstract void HidePanel();
    public abstract void EnsurePanelOpen();
    public abstract void ReloadPanelIfOpen();
    public abstract void ForcePanelOpen();
    public abstract void TogglePanel();

    public virtual void BringToFront() {
        if (!IsCreated || !IsVisible || TargetPanel is null) return;
        TargetPanel.BringToFront();
    }

    public virtual void SendToBack() {
        if (!IsCreated || !IsVisible || TargetPanel is null) return;
        TargetPanel.SendToBack();
    }

    protected virtual void OnPanelCreated(UIComponent panel) => PanelCreated?.Invoke(panel);

    protected virtual void OnPanelDestroyed() => PanelDestroyed?.Invoke();

    protected virtual void OnPanelVisibleChanged(bool value) => PanelVisibleChanged?.Invoke(TargetPanel, value);

    protected virtual void OnBeforePanelDestroyed() => BeforePanelDestroyed?.Invoke();
}