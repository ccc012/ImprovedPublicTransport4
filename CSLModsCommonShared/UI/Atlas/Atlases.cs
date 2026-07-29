using ColossalFramework.UI;
using CSLModsCommon.Logging;
using CSLModsCommon.Utilities;

namespace CSLModsCommon.UI.Atlas;

public static class Atlases {
    private static SharedAtlasLoader _sharedLoader;
    private static InGameAtlasLoader _inGameLoader;

    public static UITextureAtlas Shared => (_sharedLoader ??= new SharedAtlasLoader()).Atlas;
    public static UITextureAtlas InGame => (_inGameLoader ??= new InGameAtlasLoader()).Atlas;

    public static void Preload() {
        if (_sharedLoader is not null && _inGameLoader is not null) return;
        using var pc = PerformanceCounter.Start(c => LogManager.GetLogger().Verbose($"Atlases preload completed in {c.ReportMilliseconds}"));
        _sharedLoader ??= new SharedAtlasLoader();
        _inGameLoader ??= new InGameAtlasLoader();
    }
}