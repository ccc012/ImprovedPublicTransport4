using ColossalFramework.UI;
using CSLModsCommon.UI.Containers;
using CSLModsCommon.UI.Labels;
using UnityEngine;

namespace CSLModsCommon.UI.SettingsCard; 
public abstract class SettingsCardBase<TControl> : UIStateElement, ISettingsCard where TControl : UIComponent {
    protected FlexDirection _direction;
    protected float _textElementGap;
    protected float _columnGap;
    protected float _rowGap;
    protected TControl _control;

    public UIComponent Self => this;
    public virtual Padding LayoutPadding { get; protected set; }
    public virtual Label HeaderElement { get; protected set; }
    public virtual Label DescriptionElement { get; protected set; }
    public int LayoutCount { get; protected set; }

    public float TextElementGap {
        get => _textElementGap;
        set {
            if (Equals(value, _textElementGap)) return;
            _textElementGap = value;
            ChildInvalidatedLayout();
        }
    }

    public float ColumnGap {
        get => _columnGap;
        set {
            if (Equals(value, _columnGap)) return;
            _columnGap = value;
            ChildInvalidatedLayout();
        }
    }

    public float RowGap {
        get => _rowGap;
        set {
            if (Equals(value, _rowGap)) return;
            _rowGap = value;
            ChildInvalidatedLayout();
        }
    }

    public virtual FlexDirection Direction {
        get => _direction;
        set {
            if (value.Equals(_direction)) return;
            _direction = value;
            ChildInvalidatedLayout();
        }
    }

    public virtual string Header {
        get => HeaderElement?.Text;
        set {
            if (HeaderElement is null)
                AddHeaderElement();
            if (HeaderElement is not null)
                HeaderElement.Text = value;
        }
    }

    public virtual string Description {
        get => DescriptionElement?.Text;
        set {
            if (DescriptionElement is null)
                AddDescriptionElement();
            if (DescriptionElement is not null)
                DescriptionElement.Text = value;
        }
    }

    public virtual TControl Control {
        get => _control;
        protected set {
            if (value == null) return;
            if (_control is not null) DetachControlEvents(_control);

            _control = value;
            AttachControlEvents(_control);
        }
    }

    public virtual void Arrange() {
        switch (_direction) {
            case FlexDirection.Row:
                ArrangeRow();
                break;
            case FlexDirection.RowReverse:
                ArrangeRowReverse();
                break;
            case FlexDirection.Column:
                ArrangeColumn();
                break;
            case FlexDirection.ColumnReverse:
                ArrangeColumnReverse();
                break;
            default:
                ArrangeRow();
                break;
        }
    }

    protected virtual void ChildInvalidatedLayout() {
        Arrange();
        Invalidate();
        LayoutCount++;
    }

    protected virtual void AddHeaderElement() {
        HeaderElement = AddUIComponent<Label>();
        HeaderElement.Width = width;
        HeaderElement.SizeMode = TextSizeMode.AutoHeight;
        HeaderElement.WordWrap = true;
        HeaderElement.ProcessMarkup = true;
        HeaderElement.TextColors.SetValues(UIColors.MajorTextElementColors);
        AttachTextBlockEvents(HeaderElement);
    }

    protected virtual void AddDescriptionElement() {
        DescriptionElement = AddUIComponent<Label>();
        DescriptionElement.Width = width;
        DescriptionElement.SizeMode = TextSizeMode.AutoHeight;
        DescriptionElement.WordWrap = true;
        DescriptionElement.ProcessMarkup = true;
        DescriptionElement.TextScale = 0.8f;
        DescriptionElement.TextColors.SetValues(UIColors.MinorTextElementColors);
        AttachTextBlockEvents(DescriptionElement);
    }

    public override void Awake() {
        base.Awake();
        LayoutPadding = Padding.GetZeroPadding(this, OnPaddingChanged);
    }

    public override void OnDestroy() {
        if (HeaderElement is not null)
            DetachTextBlockEvents(HeaderElement);
        if (DescriptionElement is not null)
            DetachTextBlockEvents(DescriptionElement);
        LayoutPadding.DetachParent(OnPaddingChanged);
        base.OnDestroy();
    }

    protected override Plane[] GetClippingPlanes() {
        if (!clipChildren) return null;

        var corners = GetCorners();
        var vector = transform.TransformDirection(Vector3.right);
        var vector2 = transform.TransformDirection(Vector3.left);
        var vector3 = transform.TransformDirection(Vector3.up);
        var vector4 = transform.TransformDirection(Vector3.down);
        var d = PixelsToUnits();
        var padding = LayoutPadding;
        corners[0] += vector * padding.Left * d + vector4 * padding.Top * d;
        corners[1] += vector2 * padding.Right * d + vector4 * padding.Top * d;
        corners[2] += vector2 * padding.Right * d + vector3 * padding.Bottom * d;
        m_CachedClippingPlanes[0] = new Plane(vector, corners[0]);
        m_CachedClippingPlanes[1] = new Plane(vector2, corners[1]);
        m_CachedClippingPlanes[2] = new Plane(vector3, corners[2]);
        m_CachedClippingPlanes[3] = new Plane(vector4, corners[0]);
        return m_CachedClippingPlanes;
    }

    protected override void OnRebuildRenderData() {
        RenderBackground();
        RenderForeground();
    }

    public UIComponent GetControl() => Control;

    private void AttachControlEvents(UIComponent control) {
        control.eventVisibilityChanged += ChildIsVisibleChanged;
        control.eventSizeChanged += ChildInvalidated;
    }

    private void DetachControlEvents(UIComponent control) {
        control.eventVisibilityChanged -= ChildIsVisibleChanged;
        control.eventSizeChanged -= ChildInvalidated;
    }

    private void DetachTextBlockEvents(UIComponent child) {
        child.eventVisibilityChanged -= ChildIsVisibleChanged;
        child.eventPositionChanged -= ChildInvalidated;
        child.eventSizeChanged -= ChildInvalidated;
        child.eventZOrderChanged -= ChildZOrderChanged;
    }

    private void AttachTextBlockEvents(UIComponent child) {
        child.eventVisibilityChanged += ChildIsVisibleChanged;
        child.eventPositionChanged += ChildInvalidated;
        child.eventSizeChanged += ChildInvalidated;
        child.eventZOrderChanged += ChildZOrderChanged;
    }

    private void ChildIsVisibleChanged(UIComponent child, bool value) => ChildInvalidatedLayout();

    private void ChildZOrderChanged(UIComponent child, int value) => ChildInvalidatedLayout();

    private void ChildInvalidated(UIComponent child, Vector2 value) => ChildInvalidatedLayout();

    private void ArrangeRow() {
        if (Control is null) {
            ArrangeLabelsOnly();
            return;
        }

        var textBlockHeight = 0f;
        if (HeaderElement is not null) {
            HeaderElement.width = width - Control.width - LayoutPadding.Horizontal - _columnGap;
            textBlockHeight += HeaderElement.height;

            if (DescriptionElement is not null) {
                DescriptionElement.width = HeaderElement.width;
                textBlockHeight += _textElementGap + DescriptionElement.height;
            }
        }

        var totalHeight = Mathf.Max(textBlockHeight, Control.height);
        var baseY = LayoutPadding.Top + totalHeight / 2f;

        if (HeaderElement is not null) {
            var currentY = baseY - textBlockHeight / 2f;
            HeaderElement.relativePosition = new Vector3(LayoutPadding.Left, currentY);
            currentY += HeaderElement.height;

            if (DescriptionElement is not null) {
                currentY += _textElementGap;
                DescriptionElement.relativePosition = new Vector3(LayoutPadding.Left, currentY);
            }
        }

        float controlX;
        if (HeaderElement is null)
            controlX = width - Control.width - LayoutPadding.Right;
        else
            controlX = LayoutPadding.Left + HeaderElement.width + _columnGap;

        Control.relativePosition = new Vector3(controlX, baseY - Control.height / 2f);
        height = totalHeight + LayoutPadding.Vertical;
    }

    private void ArrangeRowReverse() {
        if (Control is null) {
            ArrangeLabelsOnly();
            return;
        }

        var textBlockHeight = 0f;
        if (HeaderElement is not null) {
            textBlockHeight += HeaderElement.height;
            if (DescriptionElement is not null)
                textBlockHeight += _textElementGap + DescriptionElement.height;
        }

        var totalHeight = Mathf.Max(textBlockHeight, Control.height);
        var baseY = LayoutPadding.Top + totalHeight / 2f;
        Control.relativePosition = new Vector3(LayoutPadding.Left, baseY - Control.height / 2f);

        if (HeaderElement is not null) {
            var textX = LayoutPadding.Left + Control.width + _columnGap;
            var currentY = baseY - textBlockHeight / 2f;

            HeaderElement.width = width - Control.width - LayoutPadding.Horizontal - _columnGap;
            HeaderElement.relativePosition = new Vector3(textX, currentY);
            currentY += HeaderElement.height;

            if (DescriptionElement is not null) {
                currentY += _textElementGap;
                DescriptionElement.width = HeaderElement.width;
                DescriptionElement.relativePosition = new Vector3(textX, currentY);
            }
        }

        height = totalHeight + LayoutPadding.Vertical;
    }

    private void ArrangeColumn() {
        float currentY = LayoutPadding.Top;

        if (HeaderElement is not null) {
            HeaderElement.width = width - LayoutPadding.Horizontal;
            HeaderElement.relativePosition = new Vector3(LayoutPadding.Left, currentY);
            currentY += HeaderElement.height;

            if (DescriptionElement is not null) {
                currentY += _textElementGap;
                DescriptionElement.width = width - LayoutPadding.Horizontal;
                DescriptionElement.relativePosition = new Vector3(LayoutPadding.Left, currentY);
                currentY += DescriptionElement.height;
            }

            currentY += _rowGap;
        }

        if (Control is not null) {
            Control.relativePosition = new Vector3(LayoutPadding.Left, currentY);
            currentY += Control.height;
        }

        height = currentY + LayoutPadding.Bottom;
    }

    private void ArrangeColumnReverse() {
        float currentY = LayoutPadding.Top;
        if (Control is not null) {
            Control.relativePosition = new Vector3(LayoutPadding.Left, currentY);
            currentY += Control.height;
            var hasNext = HeaderElement is not null;
            if (hasNext) currentY += _rowGap;
        }

        if (HeaderElement is not null) {
            HeaderElement.width = width - LayoutPadding.Horizontal;
            HeaderElement.relativePosition = new Vector3(LayoutPadding.Left, currentY);
            currentY += HeaderElement.height;
            if (DescriptionElement is not null) {
                currentY += _textElementGap;
                DescriptionElement.width = width - LayoutPadding.Horizontal;
                DescriptionElement.relativePosition = new Vector3(LayoutPadding.Left, currentY);
                currentY += DescriptionElement.height;
            }
        }

        height = currentY + LayoutPadding.Bottom;
    }

    private void ArrangeLabelsOnly() {
        float currentY = LayoutPadding.Top;
        if (HeaderElement is not null) {
            HeaderElement.width = width - LayoutPadding.Horizontal;
            HeaderElement.relativePosition = new Vector3(LayoutPadding.Left, currentY);
            currentY += HeaderElement.height + _rowGap;
        }

        if (DescriptionElement is not null) {
            DescriptionElement.width = width - LayoutPadding.Horizontal;
            DescriptionElement.relativePosition = new Vector3(LayoutPadding.Left, currentY);
            currentY += DescriptionElement.height;
        }

        height = currentY + LayoutPadding.Bottom;
    }

    private void OnPaddingChanged(Padding padding) => Arrange();
}