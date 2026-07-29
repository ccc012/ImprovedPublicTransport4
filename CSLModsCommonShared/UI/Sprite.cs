using ColossalFramework.UI;
using UnityEngine;

namespace CSLModsCommon.UI;

public class Sprite : UIComponent {
    public static readonly int[] TriangleIndices = { 0, 1, 3, 3, 1, 2 };

    public event PropertyChangedEventHandler<string> SpriteNameChanged;

    protected UITextureAtlas _atlas;
    protected string _spriteName;
    protected UISpriteFlip _flip;
    protected UIFillDirection _fillDirection;
    protected float _fillAmount = 1f;
    protected bool _invertFill;
    protected Padding _spritePadding;
    protected UIHorizontalAlignment _horizontalAlignment;
    protected UIVerticalAlignment _verticalAlignment;

    public virtual UIHorizontalAlignment HorizontalAlignment {
        get => _horizontalAlignment;
        set {
            if (value == _horizontalAlignment) return;
            _horizontalAlignment = value;
            Invalidate();
        }
    }

    public virtual UIVerticalAlignment VerticalAlignment {
        get => _verticalAlignment;
        set {
            if (value == _verticalAlignment) return;
            _verticalAlignment = value;
            Invalidate();
        }
    }

    public Padding SpritePadding {
        get => _spritePadding;
        set {
            if (Equals(value, _spritePadding)) return;
            _spritePadding = value;
            Invalidate();
        }
    }

    public UITextureAtlas Atlas {
        get {
            if (_atlas != null) return _atlas;
            var uiView = GetUIView();
            if (uiView != null)
                _atlas = uiView.defaultAtlas;
            return _atlas;
        }
        set {
            if (Equals(value, _atlas))
                return;
            _atlas = value;
            Invalidate();
        }
    }

    public string SpriteName {
        get => _spriteName;
        set {
            if (value == _spriteName) return;
            _spriteName = value;
            Invalidate();
            OnSpriteNameChanged(value);
        }
    }

    public UITextureAtlas.SpriteInfo SpriteInfo {
        get {
            if (Atlas == null) return null;
            var spriteInfo = Atlas[SpriteName];
            if (m_Size == Vector2.zero && spriteInfo != null)
                m_Size = spriteInfo.pixelSize;
            return spriteInfo;
        }
    }

    public UISpriteFlip Flip {
        get => _flip;
        set {
            if (value == _flip) return;
            _flip = value;
            Invalidate();
        }
    }

    public UIFillDirection FillDirection {
        get => _fillDirection;
        set {
            if (value == _fillDirection) return;
            _fillDirection = value;
            Invalidate();
        }
    }

    public float FillAmount {
        get => _fillAmount;
        set {
            if (Mathf.Approximately(value, _fillAmount)) return;
            _fillAmount = Mathf.Max(0.0f, Mathf.Min(1f, value));
            Invalidate();
        }
    }

    public bool InvertFill {
        get => _invertFill;
        set {
            if (value == _invertFill) return;
            _invertFill = value;
            Invalidate();
        }
    }

    public override void Awake() {
        base.Awake();
        _spritePadding = Padding.GetZeroPadding(this);
    }

    protected internal virtual void OnSpriteNameChanged(string value) {
        SpriteNameChanged?.Invoke(this, value);
        Invoke(nameof(OnSpriteNameChanged), value);
    }

    public override Vector2 CalculateMinimumSize() {
        var spriteInfo = SpriteInfo;
        if (spriteInfo == null)
            return Vector2.zero;
        var border = spriteInfo.border;
        return border is { horizontal: > 0, vertical: > 0 } ? Vector2.Max(base.CalculateMinimumSize(), new Vector2(border.horizontal, border.vertical)) : base.CalculateMinimumSize();
    }

    protected override void OnRebuildRenderData() {
        if (!(Atlas != null) || !(Atlas.material != null) || !isVisible || SpriteInfo == null)
            return;
        renderData.material = Atlas.material;
        var color32 = ApplyOpacity(isEnabled ? color : disabledColor);
        RenderSprite(renderData, new RenderOptions {
            Atlas = Atlas,
            Color = color32,
            FillAmount = FillAmount,
            FillDirection = FillDirection,
            Flip = Flip,
            InvertFill = InvertFill,
            Offset = GetRenderOffset(),
            PixelsToUnits = PixelsToUnits(),
            Size = size,
            SpriteInfo = SpriteInfo
        });
    }

    protected virtual Vector2 GetRenderOffset() {
        Vector2 result = pivot.TransformToUpperLeft(size, arbitraryPivotOffset);
        if (_horizontalAlignment == UIHorizontalAlignment.Left)
            result.x += _spritePadding.Left;
        else if (_horizontalAlignment == UIHorizontalAlignment.Center)
            result.x += _spritePadding.Left - _spritePadding.Right;
        else if (_horizontalAlignment == UIHorizontalAlignment.Right) result.x -= _spritePadding.Right;

        if (_verticalAlignment == UIVerticalAlignment.Bottom)
            result.y += _spritePadding.Bottom;
        else if (_verticalAlignment == UIVerticalAlignment.Middle)
            result.y -= _spritePadding.Top - _spritePadding.Bottom;
        else if (_verticalAlignment == UIVerticalAlignment.Top) result.y -= _spritePadding.Top;

        return result;
    }

    public static void RenderSprite(UIRenderData data, RenderOptions options) {
        options.BaseIndex = data.vertices.Count;
        RebuildTriangles(data, options);
        RebuildVertices(data, options);
        RebuildUV(data, options);
        RebuildColors(data, options);
        if (options.FillAmount >= 1.0)
            return;
        DoFill(data, options);
    }

    private static void RebuildTriangles(UIRenderData renderData, RenderOptions options) {
        var baseIndex = options.BaseIndex;
        var triangles = renderData.triangles;
        triangles.EnsureCapacity(triangles.Count + TriangleIndices.Length);
        for (var index = 0; index < TriangleIndices.Length; ++index)
            triangles.Add(baseIndex + TriangleIndices[index]);
    }

    private static void RebuildVertices(UIRenderData renderData, RenderOptions options) {
        var vertices = renderData.vertices;
        var baseIndex = options.BaseIndex;
        var x1 = 0.0f;
        var y1 = 0.0f;
        var x2 = Mathf.Ceil(options.Size.x);
        var y2 = Mathf.Ceil(-options.Size.y);
        vertices.Add(new Vector3(x1, y1, 0.0f) * options.PixelsToUnits);
        vertices.Add(new Vector3(x2, y1, 0.0f) * options.PixelsToUnits);
        vertices.Add(new Vector3(x2, y2, 0.0f) * options.PixelsToUnits);
        vertices.Add(new Vector3(x1, y2, 0.0f) * options.PixelsToUnits);
        var vector3 = options.Offset.RoundToInt() * options.PixelsToUnits;
        for (var index = 0; index < 4; ++index)
            vertices[baseIndex + index] = (vertices[baseIndex + index] + vector3).Quantize(options.PixelsToUnits);
    }

    private static void RebuildColors(UIRenderData renderData, RenderOptions options) {
        Color32 linear = ((Color)options.Color).linear;
        var colors = renderData.colors;
        for (var index = 0; index < 4; ++index)
            colors.Add(linear);
    }

    private static void RebuildUV(UIRenderData renderData, RenderOptions options) {
        var region = options.SpriteInfo.region;
        var uvs = renderData.uvs;
        uvs.Add(new Vector2(region.x, region.yMax));
        uvs.Add(new Vector2(region.xMax, region.yMax));
        uvs.Add(new Vector2(region.xMax, region.y));
        uvs.Add(new Vector2(region.x, region.y));
        if (options.Flip.IsFlagSet(UISpriteFlip.FlipHorizontal)) {
            var vector21 = uvs[1];
            uvs[1] = uvs[0];
            uvs[0] = vector21;
            var vector22 = uvs[3];
            uvs[3] = uvs[2];
            uvs[2] = vector22;
        }

        if (!options.Flip.IsFlagSet(UISpriteFlip.FlipVertical))
            return;
        var vector23 = uvs[0];
        uvs[0] = uvs[3];
        uvs[3] = vector23;
        var vector24 = uvs[1];
        uvs[1] = uvs[2];
        uvs[2] = vector24;
    }

    private static void DoFill(UIRenderData renderData, RenderOptions options) {
        var baseIndex = options.BaseIndex;
        var vertices = renderData.vertices;
        var uvs = renderData.uvs;
        var index1 = baseIndex + 3;
        var index2 = baseIndex + 2;
        var index3 = baseIndex;
        var index4 = baseIndex + 1;
        if (options.InvertFill) {
            if (options.FillDirection == UIFillDirection.Horizontal) {
                index1 = baseIndex + 1;
                index2 = baseIndex;
                index3 = baseIndex + 2;
                index4 = baseIndex + 3;
            }
            else {
                index1 = baseIndex;
                index2 = baseIndex + 1;
                index3 = baseIndex + 3;
                index4 = baseIndex + 2;
            }
        }

        if (options.FillDirection == UIFillDirection.Horizontal) {
            vertices[index2] = Vector3.Lerp(vertices[index2], vertices[index1], 1f - options.FillAmount);
            vertices[index4] = Vector3.Lerp(vertices[index4], vertices[index3], 1f - options.FillAmount);
            uvs[index2] = Vector2.Lerp(uvs[index2], uvs[index1], 1f - options.FillAmount);
            uvs[index4] = Vector2.Lerp(uvs[index4], uvs[index3], 1f - options.FillAmount);
        }
        else {
            vertices[index3] = Vector3.Lerp(vertices[index3], vertices[index1], 1f - options.FillAmount);
            vertices[index4] = Vector3.Lerp(vertices[index4], vertices[index2], 1f - options.FillAmount);
            uvs[index3] = Vector2.Lerp(uvs[index3], uvs[index1], 1f - options.FillAmount);
            uvs[index4] = Vector2.Lerp(uvs[index4], uvs[index2], 1f - options.FillAmount);
        }
    }
}