using ColossalFramework.UI;
using UnityEngine;

namespace CSLModsCommon.UI;

public class DragArea : UIComponent {
    private Vector3 _lastPosition;

    public bool ConstrainToScreen { get; set; } = true;
    public UIElement Target { get; protected set; }

    public override void Start() {
        base.Start();
        if (Target == null && parent != null)
            Target = (UIElement)parent;
        if (size != Vector2.zero) return;
        if (parent != null) {
            size = new Vector2(Target.width, 30f);
            anchor = UIAnchorStyle.Top | UIAnchorStyle.Left | UIAnchorStyle.Right;
            relativePosition = Vector2.zero;
        }
        else {
            size = new Vector2(200f, 25f);
        }
    }

    protected override void OnMouseDown(UIMouseEventParameter p) {
        if (Target != null)
            Target.BringToFront();
        else
            GetRootContainer().BringToFront();
        p.Use();
        var plane = new Plane(Target.transform.TransformDirection(Vector3.back), Target.transform.position);
        if (plane.Raycast(p.ray, out var distance)) _lastPosition = p.ray.GetPoint(distance);

        base.OnMouseDown(p);
    }

    protected override void OnMouseMove(UIMouseEventParameter p) {
        if (!p.buttons.IsFlagSet(UIMouseButton.Left)) {
            base.OnMouseMove(p);
            return;
        }

        p.Use();

        var dragDelta = CalculateDragDelta(p);
        ApplyDrag(Target, dragDelta);

        base.OnMouseMove(p);
    }

    protected override void OnMouseUp(UIMouseEventParameter p) {
        base.OnMouseUp(p);
        Target.MakePixelPerfect();
    }

    private Vector3 CalculateDragDelta(UIMouseEventParameter p) {
        var ray = p.ray;
        var uiCamera = GetUIView().uiCamera;

        var planeNormal = uiCamera.transform.TransformDirection(Vector3.back);
        var plane = new Plane(planeNormal, _lastPosition);

        if (!plane.Raycast(ray, out var distance)) return Vector3.zero;
        var current = ray.GetPoint(distance).Quantize(Target.PixelSize);
        var delta = current - _lastPosition;
        _lastPosition = current;
        return delta;
    }

    private void ApplyDrag(UIElement target, Vector3 delta) {
        if (target == null) return;

        var newPosition = (target.transform.position + delta).Quantize(target.PixelSize);

        if (ConstrainToScreen)
            newPosition = ConstrainToScreenBounds(target, newPosition);

        target.transform.position = newPosition;
    }

    private Vector3 ConstrainToScreenBounds(UIElement target, Vector3 oldPosition) {
        var corners = GetUIView().GetCorners();
        var topLeft = target.pivot.TransformToUpperLeft(target.size, target.arbitraryPivotOffset);
        var bottomRight = topLeft + new Vector3(target.size.x, -target.size.y);
        topLeft *= target.PixelSize;
        bottomRight *= target.PixelSize;

        // Left bound
        if (oldPosition.x + topLeft.x < corners[0].x)
            oldPosition.x = corners[0].x - topLeft.x;

        // Right bound
        if (oldPosition.x + bottomRight.x > corners[1].x)
            oldPosition.x = corners[1].x - bottomRight.x;

        // Top bound
        if (oldPosition.y + topLeft.y > corners[0].y)
            oldPosition.y = corners[0].y - topLeft.y;

        // Bottom bound
        if (oldPosition.y + bottomRight.y < corners[2].y)
            oldPosition.y = corners[2].y - bottomRight.y;

        return oldPosition;
    }
}