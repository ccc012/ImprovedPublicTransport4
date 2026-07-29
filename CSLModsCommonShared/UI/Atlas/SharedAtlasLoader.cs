using CSLModsCommon.Utilities;
using UnityEngine;

namespace CSLModsCommon.UI.Atlas;

public sealed class SharedAtlasLoader : AtlasLoader {
    public override string AtlasName => $"{AssemblyHelper.CurrentAssemblyName}SharedAtlas";
    public override string ResourcePath => $"{AssemblyHelper.CurrentAssemblyName}.UI.Resources";

    protected override void RegisterSprites() {
        base.RegisterSprites();
        SpriteParams[SharedAtlasKeys.CustomBackground] = new RectOffset(12, 12, 12, 12);
        SpriteParams[SharedAtlasKeys.RoundRect4] = new RectOffset(4, 4, 4, 4);
        SpriteParams[SharedAtlasKeys.RoundRect6] = new RectOffset(6, 6, 6, 6);
        SpriteParams[SharedAtlasKeys.RoundRect8] = new RectOffset(8, 8, 8, 8);
        SpriteParams[SharedAtlasKeys.RoundRect10] = new RectOffset(10, 10, 10, 10);
        SpriteParams[SharedAtlasKeys.RoundRect12] = new RectOffset(12, 12, 12, 12);

        SpriteParams[SharedAtlasKeys.XClose] = new RectOffset();
        SpriteParams[SharedAtlasKeys.Rectangle] = new RectOffset(1, 1, 1, 1);
        SpriteParams[SharedAtlasKeys.Circle] = new RectOffset();
        SpriteParams[SharedAtlasKeys.ToggleOnFg] = new RectOffset();
        SpriteParams[SharedAtlasKeys.ToggleOffFg] = new RectOffset(12, 12, 12, 12);

        SpriteParams[SharedAtlasKeys.LineBottom] = new RectOffset(1, 1, 0, 0);
        SpriteParams[SharedAtlasKeys.GradientSlider] = new RectOffset(8, 8, 8, 8);
        SpriteParams[SharedAtlasKeys.EmptySprite] = new RectOffset(1, 1, 0, 0);
        SpriteParams[SharedAtlasKeys.TransparencySprite] = new RectOffset();

        SpriteParams[SharedAtlasKeys.Copy] = new RectOffset(1, 1, 1, 1);
        SpriteParams[SharedAtlasKeys.Paste] = new RectOffset(1, 1, 1, 1);
        SpriteParams[SharedAtlasKeys.Clean] = new RectOffset(1, 1, 1, 1);

        SpriteParams[SharedAtlasKeys.ToggleBg] = new RectOffset();
        SpriteParams[SharedAtlasKeys.ResetOutline] = new RectOffset();
        SpriteParams[SharedAtlasKeys.Checked] = new RectOffset();
        SpriteParams[SharedAtlasKeys.DownArrow] = new RectOffset();
        SpriteParams[SharedAtlasKeys.UpArrow] = new RectOffset();
        SpriteParams[SharedAtlasKeys.LeftArrow] = new RectOffset();
        SpriteParams[SharedAtlasKeys.RightArrow] = new RectOffset();
        SpriteParams[SharedAtlasKeys.Loading] = new RectOffset();
        SpriteParams[SharedAtlasKeys.Error] = new RectOffset();
        SpriteParams[SharedAtlasKeys.Warning] = new RectOffset();
        SpriteParams[SharedAtlasKeys.Warn] = new RectOffset();
        SpriteParams[SharedAtlasKeys.Correct] = new RectOffset();
        SpriteParams[SharedAtlasKeys.Incorrect] = new RectOffset();
        SpriteParams[SharedAtlasKeys.RadioButtonOn] = new RectOffset();
    }
}