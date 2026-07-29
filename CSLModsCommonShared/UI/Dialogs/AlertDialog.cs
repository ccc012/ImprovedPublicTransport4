using ColossalFramework.UI;
using CSLModsCommon.Localization;
using CSLModsCommon.UI.Atlas;
using CSLModsCommon.UI.Containers;
using CSLModsCommon.UI.Labels;
using UnityEngine;

namespace CSLModsCommon.UI.Dialogs;

public abstract class AlertDialog : DialogBase {
    protected Sprite _iconElement;
    protected LiteContainer _messageContainerElement;
    protected Label _baseInformationElement;
    protected ScrollContainer _scrollContainer;
    protected Label _detailedInformationElement;

    public override bool UsingAnimation { get; set; }

    protected abstract void SetTitleText();
    protected abstract void SetIconSpriteName();

    protected virtual void AddButtons() {
        AddButton(SharedTranslations.Close, Close);
        AddButton(SharedTranslations.Copy, OnCopy);
    }

    protected override void OnAwake() {
        base.OnAwake();
        ContentContainer.AutoLayoutDirection = LayoutDirection.Horizontal;
        ContentContainer.ColumnGap = 10;
        ContentContainer.AutoLayoutPadding.SetRight(10);

        _iconElement = ContentContainer.AddUIComponent<Sprite>();
        _iconElement.Atlas = Atlases.Shared;
        _iconElement.size = new Vector2(64, 64);
        SetIconSpriteName();

        _messageContainerElement = ContentContainer.AddUIComponent<LiteContainer>();
        _messageContainerElement.size = new Vector2(DefaultWidth - 64 - 50, 300);
        _messageContainerElement.RowGap = 10;
        _baseInformationElement = _messageContainerElement.AddUIComponent<Label>();
        _baseInformationElement.SizeMode = TextSizeMode.AutoHeight;
        _baseInformationElement.WordWrap = true;
        _baseInformationElement.width = _messageContainerElement.width;
        _baseInformationElement.TextChanged += OnBaseInformationElementTextChanged;
        _baseInformationElement.relativePosition = Vector3.zero;

        var scrollablePanelHeight = 300 - _baseInformationElement.Height - 10;
        _scrollContainer = _messageContainerElement.AddUIComponent<ScrollContainer>();
        _scrollContainer.size = new Vector2(_messageContainerElement.Width, scrollablePanelHeight);
        _scrollContainer.relativePosition = new Vector3(0, _baseInformationElement.height + 10);
        _scrollContainer.VerticalScrollbar = Scrollbar.AddScrollbar(_messageContainerElement, _scrollContainer, new Vector2(8, 20));

        _scrollContainer.VerticalScrollbar.ThumbObject.color = UIColors.GroupBgNormal;
        _scrollContainer.BgAtlas = Atlases.Shared;
        _scrollContainer.BgSprites.SetValues(SharedAtlasKeys.RoundRect6);
        _scrollContainer.BgColors.SetValues(UIColors.GroupBgNormal);
        _scrollContainer.AutoLayoutPadding.SetAll(6);
        _scrollContainer.VerticalScrollbar.relativePosition = new Vector3(_messageContainerElement.width - 8, _scrollContainer.relativePosition.y);

        _detailedInformationElement = _scrollContainer.AddUIComponent<Label>();
        _detailedInformationElement.width = _messageContainerElement.Width;
        _detailedInformationElement.SizeMode = TextSizeMode.AutoHeight;
        _detailedInformationElement.WordWrap = true;
        _detailedInformationElement.TextPadding.SetAll(16);
        _detailedInformationElement.WordWrap = true;
    }

    public void AddContent(string baseInformation, string detailedInformation) {
        SetTitleText();
        _baseInformationElement.Text = baseInformation;
        _detailedInformationElement.Text = detailedInformation;
        AddButtons();
    }

    public void OnCopy() => GUIUtility.systemCopyBuffer = _detailedInformationElement.Text;

    private void OnBaseInformationElementTextChanged(Label element, string arg) {
        if (_scrollContainer is null) return;
        var scrollablePanelHeight = 300 - _baseInformationElement.Height - 10;
        _scrollContainer.size = new Vector2(_messageContainerElement.Width, scrollablePanelHeight);
        _scrollContainer.relativePosition = new Vector3(0, _baseInformationElement.height + 10);
        _scrollContainer.VerticalScrollbar.relativePosition = new Vector3(_messageContainerElement.width - 8, _scrollContainer.relativePosition.y);
    }
}