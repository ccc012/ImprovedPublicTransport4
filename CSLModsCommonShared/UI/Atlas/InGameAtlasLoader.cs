using ColossalFramework.UI;

namespace CSLModsCommon.UI.Atlas;

public class InGameAtlasLoader : AtlasLoader {
    public override string AtlasName => "InGameAtlas";
    public override string ResourcePath => string.Empty;

    protected override UITextureAtlas BuildAtlas() {
        var atlas = GetAtlas("Ingame") ?? UIView.GetAView().defaultAtlas;
        _logger.Info($"[AtlasLoader] Built atlas: {AtlasName}");
        return atlas;
    }
}