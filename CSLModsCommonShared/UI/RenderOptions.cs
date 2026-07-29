using ColossalFramework.UI;
using UnityEngine;

namespace CSLModsCommon.UI;

public struct RenderOptions {
    public UITextureAtlas Atlas { get; set; }
    public UITextureAtlas.SpriteInfo SpriteInfo { get; set; }
    public Color32 Color { get; set; }
    public float PixelsToUnits { get; set; }
    public Vector2 Size { get; set; }
    public UISpriteFlip Flip { get; set; }
    public bool InvertFill { get; set; }
    public UIFillDirection FillDirection { get; set; }
    public float FillAmount { get; set; }
    public Vector3 Offset { get; set; }
    public int BaseIndex { get; set; }
}