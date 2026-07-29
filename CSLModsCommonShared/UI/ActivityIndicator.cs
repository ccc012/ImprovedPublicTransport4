using ColossalFramework.UI;
using UnityEngine;

namespace CSLModsCommon.UI;

public class ActivityIndicator : Sprite {
    private float _rotationSpeed = 180f;

    public float RotationSpeed {
        get => _rotationSpeed;
        set {
            _rotationSpeed = value;
            Invalidate();
        }
    }

    public override void Awake() {
        base.Awake();
        size = new Vector2(20, 20);
        pivot = UIPivotPoint.MiddleCenter;
    }

    public override void Update() {
        base.Update();
        if (isVisible)
            transform.Rotate(Vector3.forward, _rotationSpeed * Time.deltaTime);
    }

    protected override void OnRebuildRenderData() {
        if (!(Atlas != null) || !(Atlas.material != null) || !isVisible || SpriteInfo == null)
            return;
        renderData.material = Atlas.material;
        RenderSprite(renderData, new RenderOptions {
            Atlas = Atlas,
            Color = isEnabled ? color : disabledColor,
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
}