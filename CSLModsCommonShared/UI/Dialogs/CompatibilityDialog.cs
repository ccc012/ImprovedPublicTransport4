using CSLModsCommon.Compatibility;
using CSLModsCommon.Localization;
using CSLModsCommon.Manager;
using CSLModsCommon.UI.Atlas;
using CSLModsCommon.UI.Containers;
using CSLModsCommon.UI.Labels;
using CSLModsCommon.UI.SettingsCard;
using System;
using System.Linq;
using UnityEngine;

namespace CSLModsCommon.UI.Dialogs;

public class CompatibilityDialog : SimpleDialog {
    private ModManagerBase _modManagerBase;
    private CompatibilityManager _compatibilityManager;

    protected override void OnAwake() {
        base.OnAwake();
        _modManagerBase = _domain.GetModManager();
        _compatibilityManager = _domain.GetOrCreateManager<CompatibilityManager>();

        TitleText = $"{_modManagerBase.ModName} {SharedTranslations.CompatibilityCheck}";

        AddDependenciesContent();
        AddGameVersionContent();
        AddCompatibilityContent();

        if (_compatibilityManager.IncompatibleRule.IsMatched && !_compatibilityManager.ShouldRestartGame)
            AddButton(SharedTranslations.HandleIncompatibleMods, () => {
                _domain.GetOrCreateManager<DialogManager>().Hide(this);
                _compatibilityManager.OnHandleIncompatibleMods();
            });

        if (_compatibilityManager.ShouldRestartGame) AddContent(SharedTranslations.RestartGame);

        AddButton(SharedTranslations.OK, Close);

        ShowWithAnimation();
    }

    public override void Start() {
        base.Start();
        CenterToParent();
    }

    private void AddCompatibilityContent() {
        var isMatched = _compatibilityManager.IncompatibleRule.IsMatched;
        var shouldRestartGame = _compatibilityManager.ShouldRestartGame;
        var description = isMatched ? SharedTranslations.CompatibilityWarning : shouldRestartGame ? SharedTranslations.ShouldRestartGame : SharedTranslations.NoIncompatibleModsDetected;
        AddItem(isMatched, SharedTranslations.IncompatibleMods, description, v => v.SpriteName = isMatched ? SharedAtlasKeys.Warn : shouldRestartGame ? SharedAtlasKeys.Warn : SharedAtlasKeys.Correct);

        if (!isMatched) return;

        foreach (var incompatibleModItem in _compatibilityManager.IncompatibleRule.Lookup.Values)
            if (incompatibleModItem.IsMatched)
                AddIncompatibleModItem(incompatibleModItem);
    }

    private void AddIncompatibleModItem(IncompatibleModItem incompatibleModItem) {
        if (incompatibleModItem is null) return;
        var card = ContentContainer.AddUIComponent<LabelCard>();
        card.BgAtlas = Atlases.Shared;
        card.BgSprites.SetValues(SharedAtlasKeys.RoundRect8);
        card.BgColors.SetValues(UIColors.GroupBgNormal);
        card.BgColors.HoveredColor = UIColors.GroupBgHovered;
        card.LayoutPadding.SetAll(10);
        card.width = ElementWidth;
        card.ColumnGap = 10;
        card.TextElementGap = 4;
        card.Control.BgAtlas = Atlases.Shared;
        card.Control.BgSprites.SetValues(SharedAtlasKeys.RoundRect8);
        card.Control.BgColors.SetValues(incompatibleModItem.IncompatibilityLevel == IncompatibilityModLevel.EnableNotAllowed ? new Color32(216, 136, 30, 255) : new Color32(146, 4, 10, 255));
        card.Control.TextPadding.SetAll(10, 10, 5, 5);
        card.Control.Text = incompatibleModItem.IncompatibilityLevel == IncompatibilityModLevel.EnableNotAllowed ? SharedTranslations.EnableNotAllowed : SharedTranslations.LoadNotAllowed;
        card.Header = incompatibleModItem.DisplayName ?? string.Empty;
        if (string.IsNullOrEmpty(incompatibleModItem.CustomWarningMessage))
            card.Description = (incompatibleModItem.IsDuplicateFunctionality ? SharedTranslations.DuplicateFunctionality : SharedTranslations.IncompatibleWithMod) + " " + (string.IsNullOrEmpty(incompatibleModItem.AlternativeModName) ? string.Empty : SharedTranslations.AlternativeMod + $": {incompatibleModItem.AlternativeModName}");
        else
            card.Description = incompatibleModItem.CustomWarningMessage;
    }

    private void AddGameVersionContent() {
        var isMatched = _compatibilityManager.VersionRule.IsMatched;
        var infoContainer = AddItem(isMatched, SharedTranslations.MakeForCurrentGameVersion, isMatched ? $"{SharedTranslations.NotMakeForCurrentGameVersion}! {SharedTranslations.CompatibleGameVersions}:" : SharedTranslations.CompatibleCurrentGameVersion);

        if (!isMatched) return;
        var versionCompatibility = _compatibilityManager.VersionRule.VersionCompatibility;
        var dependenciesItems = infoContainer.AddUIComponent<Label>();
        dependenciesItems.width = infoContainer.width;
        dependenciesItems.WordWrap = true;
        dependenciesItems.SizeMode = TextSizeMode.AutoHeight;
        dependenciesItems.Text = versionCompatibility.ToString();
    }


    private void AddDependenciesContent() {
        var isMatched = _compatibilityManager.DependencyRule.IsMatched;
        var infoContainer = AddItem(isMatched, SharedTranslations.Dependencies, isMatched ? $"{SharedTranslations.MissingDependencies}! {SharedTranslations.SubscribeDependencies}:" : SharedTranslations.NoMissingDependencies);

        if (!isMatched) return;
        var matchedDependencyNames = _compatibilityManager.DependencyRule.Lookup.Select(v => v.DisplayName).ToArray();
        var dependenciesItems = infoContainer.AddUIComponent<Label>();
        dependenciesItems.width = infoContainer.width;
        dependenciesItems.WordWrap = true;
        dependenciesItems.SizeMode = TextSizeMode.AutoHeight;
        dependenciesItems.Text = string.Join(", ", matchedDependencyNames);
    }

    private LiteContainer AddItem(bool isMatched, string header, string description, Action<Sprite> flagSpriteAction = null) {
        var container = ContentContainer.AddUIComponent<LiteContainer>();
        container.width = ElementWidth;
        container.Direction = FlexDirection.Row;
        container.AutoLayout = true;
        container.AutoFitChildrenVertically = true;
        container.ColumnGap = 10;
        container.BgAtlas = Atlases.Shared;
        container.BgSprites.SetValues(SharedAtlasKeys.RoundRect8);
        container.BgColors.SetValues(UIColors.GroupBgNormal);
        container.BgColors.HoveredColor = UIColors.GroupBgHovered;
        container.LayoutPadding.SetAll(10);

        var flagSprite = container.AddUIComponent<Sprite>();
        flagSprite.size = new Vector2(32, 32);
        flagSprite.Atlas = Atlases.Shared;
        flagSprite.SpriteName = isMatched ? SharedAtlasKeys.Warn : SharedAtlasKeys.Correct;
        flagSpriteAction?.Invoke(flagSprite);

        var infoContainer = container.AddUIComponent<LiteContainer>();
        infoContainer.width = container.Width - flagSprite.width - container.ColumnGap - container.LayoutPadding.Horizontal;
        infoContainer.AutoLayout = true;
        infoContainer.Direction = FlexDirection.Column;
        infoContainer.AutoFitChildrenVertically = true;
        infoContainer.RowGap = 10;

        var headerElement = infoContainer.AddUIComponent<Label>();
        headerElement.width = infoContainer.width;
        headerElement.SizeMode = TextSizeMode.AutoHeight;
        headerElement.Text = header;
        headerElement.TextScale = 1.1f;

        var descriptionElement = infoContainer.AddUIComponent<Label>();
        descriptionElement.width = infoContainer.width;
        descriptionElement.WordWrap = true;
        descriptionElement.SizeMode = TextSizeMode.AutoHeight;
        descriptionElement.Text = description;
        descriptionElement.TextColors.SetValues(UIColors.MinorTextElementColors);

        return infoContainer;
    }
}