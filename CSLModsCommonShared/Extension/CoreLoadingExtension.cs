using CSLModsCommon.Logging;
using ICities;
using System;

namespace CSLModsCommon.Extension;

public class CoreLoadingExtension : ILoadingExtension {
    public static event Action<ILoading> GamePreLoad;
    public static event Action<LoadMode> GameLoaded;
    public static event Action GameUnloaded;

    private ILog _logger;

    public void OnCreated(ILoading loading) {
        _logger = LogManager.GetLogger();
        _logger.Info("CoreLoadingExtension OnCreated");
        GamePreLoad?.Invoke(loading);
    }

    public void OnLevelLoaded(LoadMode mode) {
        _logger.Info("CoreLoadingExtension OnLevelLoaded");
        GameLoaded?.Invoke(mode);
    }

    public void OnLevelUnloading() { }

    public void OnReleased() {
        _logger.Info("CoreLoadingExtension OnReleased");
        GameUnloaded?.Invoke();
    }
}