using ColossalFramework.UI;
using CSLModsCommon.Localization;
using CSLModsCommon.Manager;
using CSLModsCommon.UI.Atlas;
using CSLModsCommon.UI.Containers;
using CSLModsCommon.UI.Labels;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CSLModsCommon.UI.Dialogs;

public class ChangelogDialog : OkDialog {
    private const float LayoutWidth = 560f;

    protected override void OnSizeChanged() {
        base.OnSizeChanged();
        CenterToParent();
    }

    public void Init(bool maximizeFirst = true) {
        var modManager = _domain.GetManager<ModManagerBase>();
        TitleText = $"{modManager.ModName} {SharedTranslations.Changelog}";
        if (modManager.Changelogs is null || modManager.Changelogs.Count == 0) {
            ContentContainer.AddUIComponent<LiteContainer>().size = new Vector2(ElementWidth, 30);
            return;
        }

        var first = default(VersionPanel);
        foreach (var list in modManager.Changelogs) {
            var versionPanel = ContentContainer.AddUIComponent<VersionPanel>();
            versionPanel.VersionChangeLog = list;
            if (first == null) first = versionPanel;
        }

        if (!maximizeFirst) return;
        if (first != null)
            first.IsMinimize = false;
        ShowWithAnimation();
    }

    public class VersionPanel : LiteContainer {
        private LiteContainer _versionContainer;
        private Label _versionLabel;
        private Label _dateLabel;
        private Label _minimumLabel;
        private LiteContainer _logsContainer;

        private readonly List<LogPanel> _logPanels = new();
        private ChangelogCollection _versionChangeLog;

        public ChangelogCollection VersionChangeLog {
            get => _versionChangeLog;
            set {
                if (value.Equals(_versionChangeLog)) return;
                _versionChangeLog = value;
                OnVersionChangeLogChanged(value);
            }
        }

        public bool IsMinimize {
            get => !_logsContainer.isVisible;
            set {
                _logsContainer.isVisible = !value;
                if (_minimumLabel is not null) _minimumLabel.Text = value ? "▲" : "▼";
            }
        }

        public override void Awake() {
            base.Awake();
            width = LayoutWidth;
            AutoLayout = true;
            AutoFitChildrenVertically = true;
            BgAtlas = Atlases.Shared;
            BgSprites.SetValues(SharedAtlasKeys.RoundRect8);
            BgColors.SetValues(UIColors.GroupBgNormal);
            BgColors.HoveredColor = UIColors.GroupBgHovered;
            CreateVersionInfo();
            CreateLogContainer();
        }

        private void OnVersionChangeLogChanged(ChangelogCollection value) {
            var version = value.Version;
            string text;
            if (version.Revision != -1 && version.Build != -1)
                text = version.ToString(4);
            else if (version.Revision == -1 && version.Build != -1)
                text = version.ToString(3);
            else
                text = version.ToString(3);

            _versionLabel.Text = text;
            _dateLabel.Text = value.Date.ToString("yyyy/MM/dd");
            _minimumLabel.Text = "▲";
            OnVersionContainerTextChanged();
            FillLogs(value);
        }

        private void FillLogs(ChangelogCollection value) {
            if (_logPanels.Any()) {
                _logPanels.Where(p => p is not null).ForEach(p => _logsContainer.RemoveUIComponent(p));
                _logPanels.Clear();
            }

            value.GenerateFromLocalization();

            for (var i = 0; i < value.ChangeCount; i++) {
                var panel = _logsContainer.AddUIComponent<LogPanel>();
                if (value.ChangeCount - 1 != i) panel.RenderFg = true;

                panel.Log = value[i];
                _logPanels.Add(panel);
            }

            IsMinimize = true;
        }

        private void CreateLogContainer() {
            _logsContainer = AddUIComponent<LiteContainer>();
            _logsContainer.RowGap = 4;
            _logsContainer.AutoLayout = true;
            _logsContainer.width = LayoutWidth;
            _logsContainer.LayoutPadding.SetAll(10, 10, 0, 10);
            _logsContainer.AutoFitChildrenVertically = true;
        }

        private void CreateVersionInfo() {
            _versionContainer = AddUIComponent<LiteContainer>();
            _versionContainer.size = new Vector2(LayoutWidth, 50);
            _versionContainer.eventClicked += (_, _) => IsMinimize = !IsMinimize;

            _versionLabel = _versionContainer.AddUIComponent<Label>();
            _versionLabel.TextHorizontalAlignment = UIHorizontalAlignment.Left;
            _versionLabel.TextVerticalAlignment = UIVerticalAlignment.Middle;
            _versionLabel.WordWrap = true;
            _versionLabel.SizeMode = TextSizeMode.AutoSize;
            _versionLabel.TextScale = 1.125f;

            _dateLabel = _versionContainer.AddUIComponent<Label>();
            _dateLabel.TextHorizontalAlignment = UIHorizontalAlignment.Left;
            _dateLabel.TextVerticalAlignment = UIVerticalAlignment.Middle;
            _dateLabel.WordWrap = true;
            _dateLabel.SizeMode = TextSizeMode.AutoSize;
            _dateLabel.TextScale = 1.125f;

            _minimumLabel = _versionContainer.AddUIComponent<Label>();
            _minimumLabel.TextHorizontalAlignment = UIHorizontalAlignment.Right;
            _minimumLabel.TextVerticalAlignment = UIVerticalAlignment.Middle;
            _minimumLabel.WordWrap = true;
            _minimumLabel.SizeMode = TextSizeMode.AutoSize;
            _minimumLabel.TextScale = 1.125f;
        }

        private void OnVersionContainerTextChanged() {
            if (_minimumLabel is not null) _minimumLabel.relativePosition = new Vector2(_versionContainer.width - 10 - _minimumLabel.width, (50 - _minimumLabel.height) / 2);

            if (_versionLabel is null) return;
            _versionLabel.relativePosition = new Vector2(10, (50 - _versionLabel.height) / 2);
            if (_dateLabel is not null) _dateLabel.relativePosition = new Vector2(230, (50 - _dateLabel.height) / 2);
        }
    }

    public class LogPanel : FlexContainer {
        private Label _tagLabel;
        private Label _textLabel;
        private ChangelogEntry _log;

        public ChangelogEntry Log {
            get => _log;
            set {
                _log = value;
                OnLogChanged(value);
            }
        }

        public override void Awake() {
            base.Awake();
            width = LayoutWidth - 20;
            FgCustomSize = new Vector2(width, 20);
            FgVerticalAlignment = UIVerticalAlignment.Bottom;
            FgAtlas = Atlases.Shared;
            FgSprites.SetValues(SharedAtlasKeys.LineBottom);
            FgColors.SetValues(UIColors.GroupFgNormal);
            LayoutPadding.SetTop(4).SetBottom(4);
            FgSpriteMode = ForegroundSpriteMode.Custom;
            AutoFitChildrenVertically = true;
            _columnGap = 6;
            AutoLayout = true;
        }

        private void OnLogChanged(ChangelogEntry changeLogEntry) {
            if (_tagLabel is null) {
                _tagLabel = AddUIComponent<Label>();
                _tagLabel.BgAtlas = Atlases.Shared;
                _tagLabel.BgSprites.SetValues(SharedAtlasKeys.RoundRect6);
                _tagLabel.size = new Vector2(100, 20);
                _tagLabel.SizeMode = TextSizeMode.AutoHeight;
                _tagLabel.TextPadding.SetTop(4);
                _tagLabel.TextScale = 0.8f;
                _tagLabel.TextHorizontalAlignment = UIHorizontalAlignment.Center;
                _tagLabel.TextVerticalAlignment = UIVerticalAlignment.Middle;
                _textLabel = AddUIComponent<Label>();
                _textLabel.width = LayoutWidth - 20 - 100 - 6;
                _textLabel.SizeMode = TextSizeMode.AutoHeight;
                _textLabel.WordWrap = true;
                _textLabel.TextScale = 0.8f;
                _textLabel.TextPadding.SetTop(4).SetBottom(4);
                _textLabel.TextHorizontalAlignment = UIHorizontalAlignment.Left;
            }

            _tagLabel.BgColors.SetValues(GetColor(changeLogEntry.Flag));
            _tagLabel.Text = GetLocalized(changeLogEntry.Flag);

            _textLabel.Text = changeLogEntry.LocalizedDescription ? changeLogEntry.Description.Format(LocalizationManager.LocalizeFormat) : changeLogEntry.Description;

            Invalidate();
        }

        private string GetLocalized(ChangelogFlag changelogFlag) => changelogFlag switch {
            ChangelogFlag.Added => SharedTranslations.LogMessageBox_Added,
            ChangelogFlag.Removed => SharedTranslations.LogMessageBox_Removed,
            ChangelogFlag.Updated => SharedTranslations.LogMessageBox_Updated,
            ChangelogFlag.Fixed => SharedTranslations.LogMessageBox_Fixed,
            ChangelogFlag.Optimized => SharedTranslations.LogMessageBox_Optimized,
            ChangelogFlag.Translation => SharedTranslations.LogMessageBox_Translation,
            ChangelogFlag.Attention => SharedTranslations.LogMessageBox_Attention,
            _ => string.Empty
        };

        private string Localize(string value) => LocalizationManager.Localize(value);

        private Color32 GetColor(ChangelogFlag changelogFlag) => changelogFlag switch {
            ChangelogFlag.Added => new Color32(38, 158, 62, 255),
            ChangelogFlag.Removed => new Color32(146, 50, 128, 255),
            ChangelogFlag.Updated => new Color32(58, 86, 190, 255),
            ChangelogFlag.Fixed => new Color32(216, 136, 30, 255),
            ChangelogFlag.Optimized => new Color32(113, 30, 160, 255),
            ChangelogFlag.Translation => new Color32(8, 150, 150, 255),
            ChangelogFlag.Attention => new Color32(146, 4, 10, 255),
            _ => new Color32(160, 160, 160, 255)
        };
    }
}