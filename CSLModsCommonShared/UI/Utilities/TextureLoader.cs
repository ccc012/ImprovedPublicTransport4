using ColossalFramework.Importers;
using CSLModsCommon.Logging;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace CSLModsCommon.UI.Utilities;

public static class TextureLoader {
    private static readonly ILog Logger = LogManager.GetLogger();

    public static Texture2D FromAssemblySafe(string resourcePath, Assembly asm = null, int? width = 2, int? height = 2, Color? fallbackColor = null) {
        var tex = FromAssembly(resourcePath, asm);
        if (tex != null) return tex;
        Logger.Warn($"Texture not found: {resourcePath}, using fallback");
        tex = CreateFallbackTexture(width, height, fallbackColor ?? Color.clear);

        return tex;
    }

    public static Texture2D FromFileSafe(string filePath) {
        var tex = FromFile(filePath);
        if (tex != null) return tex;
        Logger.Warn($"Texture not found: {filePath}, using fallback");
        tex = CreateFallbackTexture();

        return tex;
    }

    public static Texture2D FromAssembly(string resourcePath, Assembly asm = null) {
        asm ??= Assembly.GetExecutingAssembly();
        try {
            using var stream = asm.GetManifestResourceStream(resourcePath);
            if (stream == null) {
                Logger.Warn($"TextureLoader: Resource not found: {resourcePath}");
                return null;
            }

            using var reader = new BinaryReader(stream);
            var bytes = reader.ReadBytes((int)stream.Length);
            return ImageFromBytes(bytes);
        }
        catch (Exception ex) {
            Logger.Error(ex, $"TextureLoader: Failed to load texture from {resourcePath}");
            return null;
        }
    }

    public static Texture2D FromFile(string filePath) {
        try {
            var bytes = File.ReadAllBytes(filePath);
            return ImageFromBytes(bytes);
        }
        catch (Exception ex) {
            Logger.Error(ex, $"TextureLoader: Failed to load texture from {filePath}");
            return null;
        }
    }

    public static Texture2D ImageFromBytes(byte[] bytes) => new Image(bytes).CreateTexture();

    public static Texture2D CreateFallbackTexture(int? width = 2, int? height = 2, Color? fallbackColor = null) => CreateTexture(width ?? 2, height ?? 2, fallbackColor ?? Color.clear);

    public static Texture2D CreateTexture(int width, int height, Color color) {
        var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        var pixels = new Color[width * height];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}