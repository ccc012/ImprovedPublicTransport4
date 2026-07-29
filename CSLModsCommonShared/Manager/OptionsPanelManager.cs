using ColossalFramework.UI;
using CSLModsCommon.Extension;
using CSLModsCommon.Logging;
using CSLModsCommon.UI.OptionsPanel;
using CSLModsCommon.Utilities;
using ICities;
using System;
using System.Linq;
using UnityEngine;

namespace CSLModsCommon.Manager;

public static class OptionsPanelManager {
    private static readonly ILog Logger = LogManager.GetLogger();
    private static readonly Type PanelType;
    private static OptionsPanelBase Panel;
    private static UIPanel Container;
    private static SettingManager _settingManager;

    private static SettingManager SettingManager => _settingManager ??= Domain.DefaultDomain.GetOrCreateManager<SettingManager>();


    static OptionsPanelManager() {
        using var pc = PerformanceCounter.Start(c => Logger.Verbose($"OptionsPanelManager static constructor: {c.ReportMilliseconds}"));
        try {
            PanelType = typeof(OptionsPanelBase).Assembly
                .GetTypes()
                .FirstOrDefault(t => !t.IsAbstract && typeof(OptionsPanelBase).IsAssignableFrom(t));
            if (PanelType is not null)
                Logger.Verbose($"Detected options panel: {PanelType.FullName}");
            else
                Logger.Warn("No class inheriting OptionsPanelBase found");
        }
        catch (Exception e) {
            Logger.Error(e, "Failed to discover options panel");
        }
    }

    public static void OnLocaleChanged() {
        if (Container is null || !Container.isVisible) return;
        Destroy();
        Create();
    }

    private static void Create() {
        try {
            if (PanelType is null || Panel is not null) return;
            using var pc = PerformanceCounter.Start(c => Logger.Verbose($"OptionsPanelManager create panel: {c.ReportMilliseconds}"));
            Panel = (OptionsPanelBase)Container.AddUIComponent(PanelType);
            Panel.relativePosition = Vector2.zero;
        }
        catch (Exception e) {
            Logger.Error(e, "Create options panel object failed");
        }
    }

    private static void Destroy() {
        if (PanelType == null || Panel is null) return;
        using var pc = PerformanceCounter.Start(c => Logger.Verbose($"OptionsPanelManager destroy panel: {c.ReportMilliseconds}"));
        SettingManager?.SaveDefaultSetting();
        Panel.DestroySelf();
        Panel = null;
    }

    public static void SettingsUI(UIHelperBase helper) {
        if (((UIHelper)helper).self is not UIScrollablePanel scrollablePanel) {
            Logger.Error("OptionsPanelManager.SettingsUI: UIScrollablePanel is null");
            return;
        }

        scrollablePanel.autoLayout = false;
        Container = scrollablePanel.parent as UIPanel;
        if (Container is null) {
            Logger.Error("OptionsPanelManager.SettingsUI: Container is null");
            return;
        }

        LinqExtensions.ForEach(Container.components, component => component.isVisible = false);
        Container.autoLayout = false;
        Container.eventVisibilityChanged += OnContainerVisibilityChanged;
    }

    private static void OnContainerVisibilityChanged(UIComponent _, bool isVisible) {
        if (isVisible)
            Create();
        else
            Destroy();
    }
}