using ICities;

namespace CSLModsCommon; 
public struct LoadContext {
    public LoadMode LoadMode;
    public GameMode GameMode;

    public LoadContext(LoadMode loadMode, GameMode gameMode) {
        LoadMode = loadMode;
        GameMode = gameMode;
    }
}