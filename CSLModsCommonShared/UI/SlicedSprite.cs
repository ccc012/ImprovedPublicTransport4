using ColossalFramework.UI;
using System;
using UnityEngine;

namespace CSLModsCommon.UI; 
public class SlicedSprite : Sprite {
    private static readonly int[] SlicedTriangleIndices = {
        0, 1, 2,
        2, 3, 0,

        4, 5, 6,
        6, 7, 4,

        8, 9, 10,
        10, 11, 8,

        12, 13, 14,
        14, 15, 12,

        1, 4, 7,
        7, 2, 1,

        9, 12, 15,
        15, 10, 9,

        3, 2, 9,
        9, 8, 3,

        7, 6, 13,
        13, 12, 7,

        2, 7, 12,
        12, 9, 2
    };

    private static readonly int[][] HorizontalFill = {
        new[] { 0, 1, 4, 5 },
        new[] { 3, 2, 7, 6 },
        new[] { 8, 9, 12, 13 },
        new[] { 11, 10, 15, 14 }
    };

    private static readonly int[][] VerticalFill = {
        new[] { 11, 8, 3, 0 },
        new[] { 10, 9, 2, 1 },
        new[] { 15, 12, 7, 4 },
        new[] { 14, 13, 6, 5 }
    };

    private static readonly int[][] FillIndices;
    private static Vector3[] _vertices;
    private static Vector2[] _uVs;

    static SlicedSprite() {
        var array = new int[4][];
        var num = 0;
        var array3 = new int[4];
        array[num] = array3;
        var num2 = 1;
        var array5 = new int[4];
        array[num2] = array5;
        var num3 = 2;
        var array7 = new int[4];
        array[num3] = array7;
        var num4 = 3;
        var array9 = new int[4];
        array[num4] = array9;
        FillIndices = array;
        _vertices = new Vector3[16];
        _uVs = new Vector2[16];
    }

    public static new void RenderSprite(UIRenderData renderData, RenderOptions options) {
        options.BaseIndex = renderData.vertices.Count;
        RebuildTriangles(renderData, options);
        RebuildVertices(renderData, options);
        RebuildUV(renderData, options);
        RebuildColors(renderData, options);
        if (options.FillAmount < 1f) DoFill(renderData, options);
    }

    private static void RebuildTriangles(UIRenderData renderData, RenderOptions options) {
        var baseIndex = options.BaseIndex;
        var triangles = renderData.triangles;
        for (var i = 0; i < SlicedTriangleIndices.Length; i++) triangles.Add(baseIndex + SlicedTriangleIndices[i]);
    }

    private static void RebuildVertices(UIRenderData renderData, RenderOptions options) {
        var x = 0f;
        var y = 0f;
        var num = Mathf.Ceil(options.Size.x);
        var num2 = Mathf.Ceil(-options.Size.y);
        var spriteInfo = options.SpriteInfo;
        var num3 = (float)spriteInfo.border.left;
        var num4 = (float)spriteInfo.border.top;
        var num5 = (float)spriteInfo.border.right;
        var num6 = (float)spriteInfo.border.bottom;
        if (options.Flip.IsFlagSet(UISpriteFlip.FlipHorizontal)) {
            var num7 = num5;
            num5 = num3;
            num3 = num7;
        }

        if (options.Flip.IsFlagSet(UISpriteFlip.FlipVertical)) {
            var num8 = num6;
            num6 = num4;
            num4 = num8;
        }

        _vertices[0] = new Vector3(x, y, 0f) + options.Offset;
        _vertices[1] = _vertices[0] + new Vector3(num3, 0f, 0f);
        _vertices[2] = _vertices[0] + new Vector3(num3, -num4, 0f);
        _vertices[3] = _vertices[0] + new Vector3(0f, -num4, 0f);
        _vertices[4] = new Vector3(num - num5, y, 0f) + options.Offset;
        _vertices[5] = _vertices[4] + new Vector3(num5, 0f, 0f);
        _vertices[6] = _vertices[4] + new Vector3(num5, -num4, 0f);
        _vertices[7] = _vertices[4] + new Vector3(0f, -num4, 0f);
        _vertices[8] = new Vector3(x, num2 + num6, 0f) + options.Offset;
        _vertices[9] = _vertices[8] + new Vector3(num3, 0f, 0f);
        _vertices[10] = _vertices[8] + new Vector3(num3, -num6, 0f);
        _vertices[11] = _vertices[8] + new Vector3(0f, -num6, 0f);
        _vertices[12] = new Vector3(num - num5, num2 + num6, 0f) + options.Offset;
        _vertices[13] = _vertices[12] + new Vector3(num5, 0f, 0f);
        _vertices[14] = _vertices[12] + new Vector3(num5, -num6, 0f);
        _vertices[15] = _vertices[12] + new Vector3(0f, -num6, 0f);
        for (var i = 0; i < _vertices.Length; i++) renderData.vertices.Add((_vertices[i] * options.PixelsToUnits).Quantize(options.PixelsToUnits));
    }

    private static void RebuildUV(UIRenderData renderData, RenderOptions options) {
        var atlas = options.Atlas;
        Vector2 vector = new(atlas.texture.width, atlas.texture.height);
        var spriteInfo = options.SpriteInfo;
        var num = spriteInfo.border.top / vector.y;
        var num2 = spriteInfo.border.bottom / vector.y;
        var num3 = spriteInfo.border.left / vector.x;
        var num4 = spriteInfo.border.right / vector.x;
        var region = spriteInfo.region;
        _uVs[0] = new Vector2(region.x, region.yMax);
        _uVs[1] = new Vector2(region.x + num3, region.yMax);
        _uVs[2] = new Vector2(region.x + num3, region.yMax - num);
        _uVs[3] = new Vector2(region.x, region.yMax - num);
        _uVs[4] = new Vector2(region.xMax - num4, region.yMax);
        _uVs[5] = new Vector2(region.xMax, region.yMax);
        _uVs[6] = new Vector2(region.xMax, region.yMax - num);
        _uVs[7] = new Vector2(region.xMax - num4, region.yMax - num);
        _uVs[8] = new Vector2(region.x, region.y + num2);
        _uVs[9] = new Vector2(region.x + num3, region.y + num2);
        _uVs[10] = new Vector2(region.x + num3, region.y);
        _uVs[11] = new Vector2(region.x, region.y);
        _uVs[12] = new Vector2(region.xMax - num4, region.y + num2);
        _uVs[13] = new Vector2(region.xMax, region.y + num2);
        _uVs[14] = new Vector2(region.xMax, region.y);
        _uVs[15] = new Vector2(region.xMax - num4, region.y);
        if (options.Flip != UISpriteFlip.None) {
            for (var i = 0; i < _uVs.Length; i += 4) {
                Vector2 vector2;
                if (options.Flip.IsFlagSet(UISpriteFlip.FlipHorizontal)) {
                    vector2 = _uVs[i];
                    _uVs[i] = _uVs[i + 1];
                    _uVs[i + 1] = vector2;
                    vector2 = _uVs[i + 2];
                    _uVs[i + 2] = _uVs[i + 3];
                    _uVs[i + 3] = vector2;
                }

                if (options.Flip.IsFlagSet(UISpriteFlip.FlipVertical)) {
                    vector2 = _uVs[i];
                    _uVs[i] = _uVs[i + 3];
                    _uVs[i + 3] = vector2;
                    vector2 = _uVs[i + 1];
                    _uVs[i + 1] = _uVs[i + 2];
                    _uVs[i + 2] = vector2;
                }
            }

            if (options.Flip.IsFlagSet(UISpriteFlip.FlipHorizontal)) {
                var array = new Vector2[_uVs.Length];
                Array.Copy(_uVs, array, _uVs.Length);
                Array.Copy(_uVs, 0, _uVs, 4, 4);
                Array.Copy(array, 4, _uVs, 0, 4);
                Array.Copy(_uVs, 8, _uVs, 12, 4);
                Array.Copy(array, 12, _uVs, 8, 4);
            }

            if (options.Flip.IsFlagSet(UISpriteFlip.FlipVertical)) {
                var array2 = new Vector2[_uVs.Length];
                Array.Copy(_uVs, array2, _uVs.Length);
                Array.Copy(_uVs, 0, _uVs, 8, 4);
                Array.Copy(array2, 8, _uVs, 0, 4);
                Array.Copy(_uVs, 4, _uVs, 12, 4);
                Array.Copy(array2, 12, _uVs, 4, 4);
            }
        }

        for (var j = 0; j < _uVs.Length; j++) renderData.uvs.Add(_uVs[j]);
    }

    private static void RebuildColors(UIRenderData renderData, RenderOptions options) {
        Color32 item = ((Color)options.Color).linear;
        for (var i = 0; i < 16; i++) renderData.colors.Add(item);
    }

    private static int[][] GetFillIndices(UIFillDirection fillDirection, int baseIndex) {
        var array = fillDirection == UIFillDirection.Horizontal ? HorizontalFill : VerticalFill;
        for (var i = 0; i < 4; i++)
            for (var j = 0; j < 4; j++)
                FillIndices[i][j] = baseIndex + array[i][j];

        return FillIndices;
    }

    private static void DoFill(UIRenderData renderData, RenderOptions options) {
        var baseIndex = options.BaseIndex;
        var vertices = renderData.vertices;
        var uvs = renderData.uvs;
        var fillIndices = GetFillIndices(options.FillDirection, baseIndex);
        var invertFill = options.InvertFill;
        if (options.InvertFill)
            for (var i = 0; i < fillIndices.Length; i++)
                Array.Reverse(fillIndices[i]);

        var index = options.FillDirection == UIFillDirection.Horizontal ? 0 : 1;
        var num = vertices[fillIndices[0][invertFill ? 3 : 0]][index];
        var num2 = vertices[fillIndices[0][invertFill ? 0 : 3]][index];
        var num3 = Mathf.Abs(num2 - num);
        var num4 = !invertFill ? num + options.FillAmount * num3 : num2 - options.FillAmount * num3;
        for (var j = 0; j < fillIndices.Length; j++)
            if (!invertFill)
                for (var k = 3; k > 0; k--) {
                    var num5 = vertices[fillIndices[j][k]][index];
                    if (num5 >= num4) {
                        var value = vertices[fillIndices[j][k]];
                        value[index] = num4;
                        vertices[fillIndices[j][k]] = value;
                        var num6 = vertices[fillIndices[j][k - 1]][index];
                        if (num6 <= num4) {
                            var num7 = num5 - num6;
                            var t = (num4 - num6) / num7;
                            var b = uvs[fillIndices[j][k]][index];
                            var a = uvs[fillIndices[j][k - 1]][index];
                            var value2 = uvs[fillIndices[j][k]];
                            value2[index] = Mathf.Lerp(a, b, t);
                            uvs[fillIndices[j][k]] = value2;
                        }
                    }
                }
            else
                for (var l = 1; l < 4; l++) {
                    var num8 = vertices[fillIndices[j][l]][index];
                    if (num8 <= num4) {
                        var value3 = vertices[fillIndices[j][l]];
                        value3[index] = num4;
                        vertices[fillIndices[j][l]] = value3;
                        var num9 = vertices[fillIndices[j][l - 1]][index];
                        if (num9 >= num4) {
                            var num10 = num8 - num9;
                            var t2 = (num4 - num9) / num10;
                            var b2 = uvs[fillIndices[j][l]][index];
                            var a2 = uvs[fillIndices[j][l - 1]][index];
                            var value4 = uvs[fillIndices[j][l]];
                            value4[index] = Mathf.Lerp(a2, b2, t2);
                            uvs[fillIndices[j][l]] = value4;
                        }
                    }
                }
    }

    protected override void OnRebuildRenderData() {
        if (Atlas == null) return;
        var spriteInfo = SpriteInfo;
        if (spriteInfo is null) return;
        renderData.material = Atlas.material;
        if (spriteInfo.border.horizontal == 0 || spriteInfo.border.vertical == 0) {
            base.OnRebuildRenderData();
            return;
        }

        var renderOptions = new RenderOptions {
            Atlas = Atlas,
            Color = ApplyOpacity(isEnabled ? color : disabledColor),
            FillAmount = FillAmount,
            FillDirection = FillDirection,
            Flip = Flip,
            InvertFill = InvertFill,
            Offset = pivot.TransformToUpperLeft(size, arbitraryPivotOffset),
            PixelsToUnits = PixelsToUnits(),
            Size = size,
            SpriteInfo = spriteInfo
        };
        RenderSprite(renderData, renderOptions);
    }
}