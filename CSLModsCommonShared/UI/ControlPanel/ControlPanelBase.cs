using ColossalFramework.UI;
using CSLModsCommon.Logging;
using CSLModsCommon.Manager;
using CSLModsCommon.UI.Atlas;
using CSLModsCommon.UI.Buttons;
using CSLModsCommon.UI.Containers;
using CSLModsCommon.UI.Labels;
using CSLModsCommon.UI.Tab;
using CSLModsCommon.Utilities;
using UnityEngine;

namespace CSLModsCommon.UI.ControlPanel; 
public abstract partial class ControlPanelBase : LiteContainer {
    public static Vector3 PanelPosition { get; set; }

    protected Domain _domain;
    protected DragArea _dragBar;
    protected Label _title;
    protected NormalButton _closeButton;
    protected ModManagerBase _modManager;
    protected ILog _logger;
    protected TabBar _tabBar;
    protected TabGroupLogic _tabGroupLogic;

    public virtual float PanelWidth => 440;
    public virtual float PanelHeight => 600;
    public virtual float ElementOffset => 16;
    public virtual float ElementPanelWidth => PanelWidth - 2 * ElementOffset;
    public float CaptionHeight => 40;

    protected abstract void OnCloseButtonClicked(UIComponent component, UIMouseEventParameter eventParam);

    protected virtual void OnAwake() { }
    protected virtual void CacheManagers() { }
    protected virtual void SetSelectedTab() { }

    protected virtual void SetPosition() {
        if (PanelPosition == Vector3.zero) {
            var vector = GetUIView().GetScreenResolution();
            var x = vector.x - PanelWidth - 200;
            PanelPosition = relativePosition = new Vector3(x, 80);
            _logger.Verbose($"Control Panel position set to {PanelPosition}.");
        }
        else {
            relativePosition = PanelPosition;
            _logger.Verbose($"Control Panel position loaded from saved position {PanelPosition}.");
        }
    }

    protected virtual void AddTabBar() {
        _tabBar = AddUIComponent<TabBar>();
        _tabBar.size = new Vector2(ElementPanelWidth, 24);
        _tabBar.ColumnGap = 2;
        _tabBar.BgAtlas = Atlases.Shared;
        _tabBar.BgSprites.SetValues(SharedAtlasKeys.RoundRect6);
        _tabBar.BgColors.SetValues(UIColors.GroupBgNormal);
        _tabBar.relativePosition = new Vector3(16, CaptionHeight);
        _tabGroupLogic = TabGroupLogic.Create().BindingTabBar(_tabBar);
    }

    public sealed override void Awake() {
        base.Awake();
        _domain = Domain.DefaultDomain;
        _logger = LogManager.GetLogger();
        _modManager = _domain.GetManager<ModManagerBase>();
        CacheManagers();

        name = AssemblyHelper.CurrentAssemblyName + "ControlPanel";
        size = new Vector2(PanelWidth, PanelHeight);
        _bgAtlas = Atlases.Shared;
        _bgSprites.SetValues(SharedAtlasKeys.CustomBackground);
        pivot = UIPivotPoint.MiddleCenter;

        AddCloseButton();
        AddDragArea();
        AddTabBar();

        OnAwake();

        SetSelectedTab();
        SetPosition();
    }

    public override void OnDisable() {
        base.OnDisable();
        PanelPosition = relativePosition;
    }

    protected SettingsSection AddSection(UIComponent container, string header = null, string description = null) {
        var section = container.AddUIComponent<SettingsSection>();
        section.width = ElementPanelWidth;
        if (string.IsNullOrEmpty(header)) return section;
        section.Header = header;
        if (!string.IsNullOrEmpty(description))
            section.Description = description;
        return section;
    }

    protected ScrollContainer AddPage(string id, string text) {
        var page = AddUIComponent<ScrollContainer>();
        page.size = new Vector2(ElementPanelWidth, size.y - _tabBar.height - _dragBar.height - ElementOffset * 2);
        page.AutoLayout = true;
        page.RowGap = 16;
        page.relativePosition = new Vector3(16, _tabBar.relativePosition.y + _tabBar.height + ElementOffset);

        var scrollbar = Scrollbar.AddScrollbar(this, page, new Vector2(8, page.height));
        scrollbar.ThumbObject.color = UIColors.GroupBgNormal;
        scrollbar.relativePosition = new Vector3(width - 8, page.relativePosition.y);

        _tabGroupLogic.AddTab(id, text, page, b => {
            b.TextScale = 0.8f;
            b.TextPadding.Top = 1;
            b.SetStyle(StyleType.ControlPanelStyle);
        });

        return page;
    }

    protected string Localize(string key) => LocalizationManager.Localize(key);

    private void AddCloseButton() {
        _closeButton = AddUIComponent<NormalButton>();
        _closeButton.size = new Vector2(24, 24);
        _closeButton.FgAtlas = _closeButton.BgAtlas = Atlases.Shared;
        _closeButton.FgSprites.SetValues(SharedAtlasKeys.XClose);
        _closeButton.BgSprites.SetValues(string.Empty, SharedAtlasKeys.Circle, SharedAtlasKeys.Circle, SharedAtlasKeys.Circle, string.Empty);
        _closeButton.BgColors.SetValues(UIColors.GroupBgNormal);
        _closeButton.BgColors.PressedColor = UIColors.GroupFgNormal;
        _closeButton.FgScaleFactor = 0.65f;
        _closeButton.RenderFg = true;
        _closeButton.relativePosition = new Vector3(width - ElementOffset - _closeButton.width, (CaptionHeight - _closeButton.height) / 2);
        _closeButton.eventClicked += OnCloseButtonClicked;
    }

    private void AddDragArea() {
        var dragBarWidth = width - ElementOffset - _closeButton.width;

        _title = AddUIComponent<Label>();
        _title.size = new Vector2(dragBarWidth, CaptionHeight);
        _title.SizeMode = TextSizeMode.Fixed;
        _title.TextHorizontalAlignment = UIHorizontalAlignment.Center;
        _title.TextVerticalAlignment = UIVerticalAlignment.Middle;
        _title.Text = _modManager.ModName;
        _title.relativePosition = Vector3.zero;

        _dragBar = AddUIComponent<DragArea>();
        _dragBar.width = dragBarWidth;
        _dragBar.height = CaptionHeight;
        _dragBar.relativePosition = Vector3.zero;
    }
}