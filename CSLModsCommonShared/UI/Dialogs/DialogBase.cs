using ColossalFramework.UI;
using CSLModsCommon.Manager;
using CSLModsCommon.UI.Atlas;
using CSLModsCommon.UI.Buttons;
using CSLModsCommon.UI.Containers;
using CSLModsCommon.UI.Labels;
using System;
using System.Collections.Generic;
using UnityEngine;
using Animation = CSLModsCommon.Utilities.Animation;

namespace CSLModsCommon.UI.Dialogs;

public abstract class DialogBase : LiteContainer {
    protected LiteContainer _buttonPanel;
    protected DragArea _dragArea;
    protected Label _title;
    protected Domain _domain;

    protected virtual int DefaultHeight => 200;
    protected virtual int DefaultWidth => 600;
    protected virtual float DragBarHeight => 80;
    protected virtual float ButtonHeight => 34f;
    protected virtual float ButtonSpacing => 10f;
    protected virtual float ElementMargin => 20;
    public virtual bool UsingAnimation { get; set; } = true;
    public Vector2 DefaultSize => new(DefaultWidth, DefaultHeight);
    public AutoFitScrollContainer ContentContainer { get; protected set; }

    public string TitleText {
        set => _title.Text = value;
    }

    protected float ElementWidth => DefaultWidth - 2 * ElementMargin;
    protected float ButtonPanelHeight => ButtonHeight + 2 * ElementMargin;
    protected List<NormalButton> Buttons { get; set; } = new();
    protected float MaxScrollableContentHeight => GetUIView().GetScreenResolution().y - 600f;
    private Vector2 SizeBefore { get; set; }

    public DialogBase AddContent(Action<AutoFitScrollContainer> builder) {
        builder(ContentContainer);
        return this;
    }

    protected virtual void Resize() {
        var newHeight = DragBarHeight + ContentContainer.height + ButtonPanelHeight;
        if (Mathf.Approximately(height, newHeight)) return;

        height = newHeight;
        ContentContainer.relativePosition = new Vector2(0f, DragBarHeight);
        ContentContainer.VerticalScrollbar.relativePosition = new Vector3(width - 8, 80);
        _buttonPanel.relativePosition = new Vector2(0f, DragBarHeight + ContentContainer.height);
    }

    protected virtual void OnAwake() {
        _title = AddUIComponent<Label>();
        _title.size = new Vector2(DefaultWidth, DragBarHeight);
        _title.SizeMode = TextSizeMode.Fixed;
        _title.TextHorizontalAlignment = UIHorizontalAlignment.Center;
        _title.TextVerticalAlignment = UIVerticalAlignment.Middle;
        _title.TextScale = 1.3f;
        _title.TextPadding.Top = 16;
        _title.Font = FontHelper.SemiBold;
        _title.WordWrap = true;
        _title.relativePosition = Vector2.zero;

        _dragArea = AddUIComponent<DragArea>();
        _dragArea.size = new Vector2(DefaultWidth, DragBarHeight);
        _dragArea.relativePosition = Vector2.zero;

        ContentContainer = AddUIComponent<AutoFitScrollContainer>();
        ContentContainer.size = new Vector2(DefaultWidth, 200);
        ContentContainer.AutoLayout = true;
        ContentContainer.AutoLayoutDirection = LayoutDirection.Vertical;
        ContentContainer.RowGap = 10;
        ContentContainer.AutoLayoutPadding.SetLeft(20);
        Scrollbar.AddScrollbar(this, ContentContainer, new Vector2(8, 20));
        ContentContainer.AutoFitToContents = true;
        ContentContainer.MaxAutoSize = new Vector2(DefaultWidth, MaxScrollableContentHeight);
        ContentContainer.VerticalScrollbar.ThumbObject.color = UIColors.GroupBgNormal;
        ContentContainer.VerticalScrollbar.relativePosition = new Vector3(width - 8, 80);
        ContentContainer.eventSizeChanged += (_, _) => Resize();

        _buttonPanel = AddUIComponent<LiteContainer>();
        _buttonPanel.size = new Vector2(DefaultWidth, ButtonPanelHeight);
    }

    public sealed override void Awake() {
        base.Awake();
        canFocus = true;
        isInteractive = true;
        _bgAtlas = Atlases.Shared;
        _bgSprites.SetValues(SharedAtlasKeys.CustomBackground);
        SizeBefore = size = DefaultSize;
        _domain = Domain.DefaultDomain;
        pivot = UIPivotPoint.MiddleCenter;
        OnAwake();
        Resize();

        CenterToParent();
    }

    protected override void OnKeyDown(UIKeyEventParameter p) {
        if (p.used || p.keycode != KeyCode.Escape) return;
        p.Use();
        Close();
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        var resolution = GetUIView().GetScreenResolution();
        var delta = (size - SizeBefore) / 2f;
        SizeBefore = size;

        var newPos = (Vector2)relativePosition - delta;
        newPos.x = Mathf.Clamp(newPos.x, 0, resolution.x - size.x);
        newPos.y = Mathf.Clamp(newPos.y, 0, resolution.y - size.y);

        relativePosition = newPos;
    }

    protected void ShowWithAnimation() => Animation.AnimateIn(this);

    public NormalButton AddButton(string text, UIElementEventHandler onButtonClicked) {
        if (_buttonPanel is null) return null;
        var button = _buttonPanel.AddUIComponent<NormalButton>();
        Buttons.Add(button);
        button.SetStyle(StyleType.ControlPanelStyle);
        button.BgColors.SetValues(UIColors.BgElementColors);
        button.BgColors.NormalColor = UIColors.GroupBgNormal;
        button.BgColors.HoveredColor = UIColors.GroupBgHovered;
        button.autoSize = false;
        button.height = ButtonHeight;
        button.Text = text;
        button.eventClicked += (_, _) => onButtonClicked?.Invoke();
        ArrangeButtons();
        return button;
    }

    protected string Localize(string value) => LocalizationManager.Localize(value);

    protected void Close() => _domain.GetOrCreateManager<DialogManager>().Hide(this);

    private void ArrangeButtons() {
        if (Buttons.Count == 0) return;
        var count = Buttons.Count;
        var buttonWidth = (ElementWidth - (count - 1) * ButtonSpacing) / count;
        var offsetX = ElementMargin;
        for (var i = 0; i < count; i++) {
            Buttons[i].width = buttonWidth;
            Buttons[i].relativePosition = new Vector2(offsetX, (ButtonPanelHeight - ButtonHeight) / 2);
            offsetX += buttonWidth + ButtonSpacing;
        }
    }
}