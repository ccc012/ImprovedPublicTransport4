using CSLModsCommon.Extension;
using ICities;

namespace CSLModsCommon.Manager; 
public abstract class ManagerBase : ComponentManagerBase {
    public GameMode CurrentMode {
        get {
            if (GameLoading is null || !GameLoaded)
                return GameMode.MainMenu;
            return GameLoading.currentMode switch {
                AppMode.Game => GameMode.Game,
                AppMode.AssetEditor => GameMode.AssetEditor,
                AppMode.ScenarioEditor => GameMode.ScenarioEditor,
                AppMode.ThemeEditor => GameMode.ThemeEditor,
                AppMode.MapEditor => GameMode.MapEditor,
                _ => GameMode.MainMenu
            };
        }
    }

    protected ILoading GameLoading { get; private set; }
    protected bool GameLoaded { get; private set;  }

    protected virtual void OnGameInitialized() { }
    protected virtual void OnGamePreLoad() { }
    protected virtual void OnGameLoaded(LoadContext context) { }
    protected virtual void OnGameUnloaded() { }

    protected override void OnCreate() {
        base.OnCreate();
        ModManagerBase.GameInitialized += OnGameInitialized;
        CoreLoadingExtension.GamePreLoad += NotifyGamePreLoad;
        CoreLoadingExtension.GameLoaded += NotifyGameLoaded;
        CoreLoadingExtension.GameUnloaded += NotifyGameUnload;
    }

    protected override void OnDestroy() {
        ModManagerBase.GameInitialized -= OnGameInitialized;
        CoreLoadingExtension.GamePreLoad -= NotifyGamePreLoad;
        CoreLoadingExtension.GameLoaded -= NotifyGameLoaded;
        CoreLoadingExtension.GameUnloaded -= NotifyGameUnload;
        base.OnDestroy();
    }

    private void NotifyGamePreLoad(ILoading loading) {
        GameLoading = loading;
        OnGamePreLoad();
    }

    private void NotifyGameLoaded(LoadMode mode) {
        GameMode gameMode = 0;
        if (GameLoading != null)
            gameMode = GameLoading.currentMode switch {
                AppMode.Game => GameMode.Game,
                AppMode.AssetEditor => GameMode.AssetEditor,
                AppMode.ScenarioEditor => GameMode.ScenarioEditor,
                AppMode.ThemeEditor => GameMode.ThemeEditor,
                AppMode.MapEditor => GameMode.MapEditor,
                _ => GameMode.MainMenu
            };
        GameLoaded = true;
        OnGameLoaded(new LoadContext(mode, gameMode));
    }

    private void NotifyGameUnload() {
        GameLoaded = false;
        OnGameUnloaded();
    }
}