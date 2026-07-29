using ColossalFramework;
using ColossalFramework.UI;
using CSLModsCommon.Utilities;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CSLModsCommon.Manager;

public abstract class ControlPanelManagerBase : PanelManagerBase {
    protected GameObject _panelGameObject;

    public virtual bool UsingAnimation => true;

    public override UIComponent CreatePanel() {
        if (_panelGameObject is not null) return TargetPanel;
        using var performanceCounter = PerformanceCounter.Start(c => Logger.Verbose($"{GetType().FullName} create time: {c.ReportMilliseconds}"));
        _panelGameObject = new GameObject(AssemblyHelper.CurrentAssemblyName + PanelType.Name) {
            transform = {
                parent = UIView.GetAView().transform
            }
        };

        TargetPanel = (UIComponent)_panelGameObject.AddComponent(PanelType);
        if (UsingAnimation) {
            TargetPanel.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            ValueAnimator.Animate($"{PanelType.FullName}ShowAnimate", val => {
                var scale = Mathf.Lerp(0.8f, 1f, val);
                TargetPanel.transform.localScale = new Vector3(scale, scale, 1f);
            }, new AnimatedFloat(0f, 1f, 0.3f, EasingType.ExpoEaseOut));
        }

        if (ShowWhenCreated)
            ShowPanel();
        else
            HidePanel();

        IsCreated = true;
        OnPanelCreated(TargetPanel);
        return TargetPanel;
    }

    public override void DestroyPanel(Action afterDestroyAction = null) {
        if (_panelGameObject is null) {
            afterDestroyAction?.Invoke();
            return;
        }

        using var performanceCounter = PerformanceCounter.Start(c => Logger.Verbose($"{PanelType.FullName} destroy time: {c.ReportMilliseconds}"));
        OnBeforePanelDestroyed();
        IsVisible = false;
        if (UsingAnimation) {
            Utilities.Animation.AnimateOut(TargetPanel, () => {
                InternalDestroyPanel();
                afterDestroyAction?.Invoke();
            });
        }
        else {
            InternalDestroyPanel();
            afterDestroyAction?.Invoke();
        }
    }


    public override void ShowPanel() {
        if (TargetPanel is null) return;
        TargetPanel.Show(true);
        TargetPanel.Focus();
        IsVisible = true;
    }

    public override void HidePanel() {
        if (TargetPanel is null) return;
        TargetPanel.Hide();
        IsVisible = false;
    }

    public override void EnsurePanelOpen() {
        if (!IsVisible) CreatePanel();
    }

    public override void ReloadPanelIfOpen() {
        if (IsVisible) DestroyPanel(() => CreatePanel());
    }

    public override void ForcePanelOpen() => DestroyPanel(() => CreatePanel());

    public override void TogglePanel() {
        if (IsVisible)
            DestroyPanel();
        else
            CreatePanel();
    }

    private void InternalDestroyPanel() {
        Object.Destroy(TargetPanel);
        Object.Destroy(_panelGameObject);
        TargetPanel = null;
        _panelGameObject = null;
        IsCreated = false;
        IsVisible = false;
        OnPanelDestroyed();
    }
}