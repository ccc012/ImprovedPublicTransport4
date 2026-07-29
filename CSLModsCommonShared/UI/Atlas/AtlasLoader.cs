using ColossalFramework.UI;
using CSLModsCommon.Logging;
using CSLModsCommon.UI.Utilities;
using CSLModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CSLModsCommon.UI.Atlas;

public abstract class AtlasLoader {
    protected ILog _logger;
    private UITextureAtlas _atlas;

    public abstract string AtlasName { get; }
    public abstract string ResourcePath { get; }
    public virtual Dictionary<string, RectOffset> SpriteParams { get; } = new();
    protected virtual AtlasResourceSource ResourceSource => AtlasResourceSource.Assembly;

    public UITextureAtlas Atlas {
        get {
            if (_atlas != null) return _atlas;
            _atlas = BuildAtlas();

            return _atlas;
        }
    }

    protected AtlasLoader() {
        _logger = LogManager.GetLogger();
        RegisterSprites();
    }

    public static UITextureAtlas GetAtlas(string atlasName) {
        var atlas = GetAtlas();
        if (atlas is null) return null;
        foreach (var item in atlas) {
            if (item.name != atlasName) continue;
            LogManager.GetLogger().Info($"Obtained {atlasName} UITextureAtlas.");
            return item;
        }

        return null;
    }

    public static IEnumerable<UITextureAtlas> GetAtlas() {
        if (Resources.FindObjectsOfTypeAll(typeof(UITextureAtlas)) is not UITextureAtlas[] atlases) yield break;
        foreach (var t in atlases) yield return t;
    }

    protected virtual void RegisterSprites() { }

    protected virtual UITextureAtlas BuildAtlas() {
        using var t = PerformanceCounter.Start(v => _logger.Debug($"Building atlas {AtlasName} took {v.ReportMilliseconds}"));
        var atlas = CreateTextureAtlas();
        _logger.Info($"[AtlasLoader] Built atlas: {AtlasName} from {ResourcePath}");
        return atlas;
    }

    public virtual void Unload() {
        if (_atlas == null) return;
        Object.Destroy(_atlas.material.mainTexture);
        Object.Destroy(_atlas.material);
        Object.Destroy(_atlas);
        _atlas = null;
    }

    public virtual void Reload() {
        Unload();
        _atlas = BuildAtlas();
    }

    public UITextureAtlas CreateTextureAtlas(int maxSpriteSize = 1024) {
        var keys = SpriteParams.Keys.ToArray();
        var value = SpriteParams.Values.ToArray();
        Texture2D texture2D = new(maxSpriteSize, maxSpriteSize, TextureFormat.ARGB32, false);
        var textures = new Texture2D[SpriteParams.Count];
        for (var i = 0; i < SpriteParams.Count; i++) textures[i] = LoadTexture(keys[i]);

        var regions = texture2D.PackTextures(textures, 2, maxSpriteSize);
        var uITextureAtlas = ScriptableObject.CreateInstance<UITextureAtlas>();
        var material = Object.Instantiate(UIView.GetAView().defaultAtlas.material);
        material.mainTexture = texture2D;
        uITextureAtlas.material = material;
        uITextureAtlas.name = AtlasName;
        for (var j = 0; j < SpriteParams.Count; j++) {
            UITextureAtlas.SpriteInfo item = new() {
                name = keys[j],
                texture = textures[j],
                region = regions[j],
                border = value[j]
            };
            uITextureAtlas.AddSprite(item);
        }

        return uITextureAtlas;
    }

    protected Texture2D LoadTexture(string spriteName) {
        try {
            switch (ResourceSource) {
                case AtlasResourceSource.Assembly:
                default:
                    var assemblyResource = $"{ResourcePath}.{spriteName}.png";
                    var tex = TextureLoader.FromAssembly(assemblyResource);
                    if (tex != null) return tex;
                    _logger.Warn($"[AtlasLoader] Missing embedded resource: {assemblyResource}, using placeholder.");
                    tex = CreatePlaceholderTexture();

                    return tex;

                case AtlasResourceSource.File:
                    var filePath = Path.Combine(ResourcePath, spriteName + ".png");
                    if (File.Exists(filePath))
                        return TextureLoader.FromFileSafe(filePath);

                    _logger.Warn($"[AtlasLoader] Missing file resource: {filePath}, using placeholder.");
                    return CreatePlaceholderTexture();
            }
        }
        catch (Exception ex) {
            _logger.Error(ex, $"[AtlasLoader] Failed to load sprite '{spriteName}', using placeholder.");
            return CreatePlaceholderTexture();
        }
    }

    protected Texture2D CreatePlaceholderTexture() => TextureLoader.CreateTexture(32, 32, Color.magenta);
}