using System;
using ColossalFramework.UI;
using CSLModsCommon.Collections;
using CSLModsCommon.Extension;
using CSLModsCommon.UI.Atlas;
using CSLModsCommon.UI.Buttons;
using CSLModsCommon.UI.Buttons.RadioButtons;
using CSLModsCommon.UI.Containers;
using CSLModsCommon.UI.DropDown;
using CSLModsCommon.UI.Labels;
using CSLModsCommon.UI.SettingsCard;
using CSLModsCommon.UI.Sliders;
using UnityEngine;

namespace CSLModsCommon.UI.ControlPanel;

public partial class ControlPanelBase {
    public sealed class SettingsSection : SettingsCardBase<LiteContainer> {
        private ReusableList<ISettingsCard> _itemCards;

        public CSLModsCommon.Collections.IReadOnlyList<ISettingsCard> ItemCards => _itemCards;

        public override void Awake() {
            base.Awake();
            m_Size = new Vector2(100, 10);
            _direction = FlexDirection.Column;
            _textElementGap = _rowGap = 4;
            _itemCards = ReusableList<ISettingsCard>.Rent();
            Control = AddUIComponent<LiteContainer>();
            Control.width = width;
            Control.AutoLayout = true;
            Control.AutoFitChildrenVertically = true;
            Control.BgAtlas = Atlases.Shared;
            Control.BgSprites.SetValues(SharedAtlasKeys.RoundRect6);
            Control.BgColors.SetValues(UIColors.GroupBgNormal);
        }

        public override void OnDestroy() {
            base.OnDestroy();
            _itemCards.Return();
        }

        protected override void AddHeaderElement() {
            base.AddHeaderElement();
            HeaderElement.TextScale = 0.8f;
            HeaderElement.TextColors.SetValues(UIColors.MajorTextElementColors);
            HeaderElement.TextPadding.SetAll(10, 10, 0, 0);
        }

        protected override void AddDescriptionElement() {
            base.AddDescriptionElement();
            DescriptionElement.TextScale = 0.8f;
            DescriptionElement.TextColors.SetValues(UIColors.MinorTextElementColors);
            DescriptionElement.TextPadding.SetAll(10, 10, 0, 0);
        }

        protected override void OnSizeChanged() {
            base.OnSizeChanged();
            if (Control is null) return;
            if (Mathf.Abs(width - Control.width) > 0.01f) Control.width = width;
        }

        public EmptyLiteContainerCard AddEmptyLiteContainer(string header = null, string description = null, Action<EmptyLiteContainerCard> beforeLayoutAction = null) {
            var card = AddItemCard<EmptyLiteContainerCard, LiteContainer>(header, description);
            beforeLayoutAction?.Invoke(card);
            return card;
        }

        public EmptyFlexContainerCard AddEmptyFlexContainer(string header = null, string description = null, Action<EmptyFlexContainerCard> beforeLayoutAction = null) {
            var card = AddItemCard<EmptyFlexContainerCard, FlexContainer>(header, description);
            beforeLayoutAction?.Invoke(card);
            return card;
        }

        public CheckBoxCard AddCheckBox(bool isChecked, string checkBoxText, string header = null, string description = null, UIElementEventHandler<bool> callback = null, Action<CheckBoxCard> beforeLayoutAction = null) => AddCheckBox(isChecked, checkBoxText, header, description, (_, b) => callback?.Invoke(b), beforeLayoutAction);

        public CheckBoxCard AddCheckBox(bool isChecked, string checkBoxText, string header = null, string description = null, UIElementEventHandler<CheckBox, bool> callback = null, Action<CheckBoxCard> beforeLayoutAction = null) {
            var card = AddItemCard<CheckBoxCard, CheckBox>(header, description);
            var checkBox = card.Control;
            checkBox.width = card.width - card.LayoutPadding.Horizontal;
            checkBox.LabelElement.TextPadding.SetTop(1);
            checkBox.CheckBoxIndicatorElement.SetStyle(StyleType.ControlPanelStyle);
            checkBox.IsChecked = isChecked;
            checkBox.Text = checkBoxText;
            checkBox.CheckChanged += callback;
            beforeLayoutAction?.Invoke(card);
            return card;
        }

        public RadioGroupLogic<V> AddRadioGroup<V>(
            string header,
            string description,
            Action<RadioGroupLogic<V>, RadioGroup> options,
            Action<RadioGroupCard> beforeLayoutAction = null
        ) {
            var card = AddItemCard<RadioGroupCard, RadioGroup>(header, description, FlexDirection.Column);
            var radioGroup = card.Control;
            radioGroup.RowGap = 6;
            radioGroup.width = card.width - card.LayoutPadding.Horizontal;
            var logic = RadioGroupLogic<V>.Create();
            options?.Invoke(logic, radioGroup);
            foreach (var radioButton in radioGroup.Buttons) {
                if (radioButton is null) continue;

                radioButton.Radio?.SetStyle(StyleType.ControlPanelStyle);

                if (radioButton.TextElement is null) continue;
                radioButton.TextElement.TextScale = 0.8f;
                radioButton.TextElement.TextPadding.SetTop(1);
            }

            beforeLayoutAction?.Invoke(card);
            return logic;
        }

        public RadioGroupCard AddRadioGroup(string header, string description, Action<RadioGroup> options, Action<RadioGroupCard> beforeLayoutAction = null) {
            var card = AddItemCard<RadioGroupCard, RadioGroup>(header, description, FlexDirection.Column);
            var radioGroup = card.Control;
            radioGroup.RowGap = 6;
            radioGroup.width = card.width - card.LayoutPadding.Horizontal;
            options?.Invoke(radioGroup);
            foreach (var radioButton in radioGroup.Buttons) {
                if (radioButton is null) continue;

                radioButton.Radio?.SetStyle(StyleType.ControlPanelStyle);

                if (radioButton.TextElement is null) continue;
                radioButton.TextElement.TextScale = 0.8f;
                radioButton.TextElement.TextPadding.SetTop(1);
            }

            beforeLayoutAction?.Invoke(card);
            return card;
        }

        public NormalButtonsCard AddButtons(
            string header,
            string description,
            Action<INormalButtonsCard> resisterButtons = null,
            Action<NormalButtonsCard> beforeLayoutAction = null) {
            var card = AddItemCard<NormalButtonsCard, LiteContainer>(header, description);
            var container = card.Control;
            container.ColumnGap = 6;
            resisterButtons?.Invoke(card);

            foreach (var button in card.Buttons.Values) {
                button.autoSize = false;
                button.TextScale = 0.8f;
                button.height = 24;
                button.TextPadding.SetAll(10, 10, 4, 0);
                button.SetStyle(StyleType.ControlPanelStyle);
                button.AutoWidth = true;
            }

            beforeLayoutAction?.Invoke(card);
            return card;
        }

        public NormalButtonCard AddButton(string header, string description, string buttonText, UIElementEventHandler onButtonClicked = null, Action<NormalButtonCard> beforeLayoutAction = null) => AddButton(header, description, buttonText, null, 30, _ => onButtonClicked?.Invoke(), beforeLayoutAction);

        public NormalButtonCard AddButton(string header, string description, string buttonText, float? buttonWidth, float buttonHeight = 24, UIElementEventHandler<NormalButton> onButtonClicked = null, Action<NormalButtonCard> beforeLayoutAction = null) {
            var panel = AddItemCard<NormalButtonCard, NormalButton>(header, description);
            var button = panel.Control;
            SetButtonSettings(button, buttonText, buttonWidth, buttonHeight, onButtonClicked);
            beforeLayoutAction?.Invoke(panel);
            return panel;
        }

        public static NormalButton SetButtonSettings(NormalButton button, string buttonText, float? buttonWidth = null, float buttonHeight = 24, UIElementEventHandler<NormalButton> onButtonClicked = null) {
            button.autoSize = false;
            button.TextScale = 0.8f;
            button.height = buttonHeight;
            button.TextPadding.SetAll(10, 10, 4, 0);
            button.SetStyle(StyleType.ControlPanelStyle);
            button.Text = buttonText;
            if (buttonWidth is not null)
                button.width = (float)buttonWidth;
            else
                button.AutoWidth = true;
            button.eventClicked += (c, _) => onButtonClicked?.Invoke(c as NormalButton);
            return button;
        }

        public SliderCard AddSlider(string header, string description, float minValue, float maxValue, float stepValue, float defaultValue, Vector2 sliderSize, Action<float> callback = null, bool gradientStyle = false, Action<SliderCard> beforeLayoutAction = null) {
            var card = AddItemCard<SliderCard, Slider>(header, description, FlexDirection.Column);
            var slider = card.Control;
            slider.size = sliderSize;
            slider.MinValue = minValue;
            slider.MaxValue = maxValue;
            slider.StepSize = stepValue;
            slider.Value = defaultValue;
            slider.ValueChanged += (_, v) => callback?.Invoke(v);
            if (gradientStyle)
                slider.SetGradientStyle();
            else
                slider.SetGreenStyle();
            beforeLayoutAction?.Invoke(card);
            return card;
        }

        public ByteFieldCard AddByteField(string header, string description, byte defaultValue, byte minLimit, byte maxLimit, byte wheelStep, Action<byte> callback = null, float fieldWidth = 100, float fieldHeight = 24, Action<ByteFieldCard> beforeLayoutAction = null) => AddValueField<ByteFieldCard, ByteValueField, byte>(header, description, defaultValue, minLimit, maxLimit, wheelStep, callback, fieldWidth, fieldHeight, beforeLayoutAction);

        public FloatFieldCard AddFloatField(string header, string description, float defaultValue, float minLimit, float maxLimit, float wheelStep, Action<float> callback = null, float fieldWidth = 100, float fieldHeight = 24, Action<FloatFieldCard> beforeLayoutAction = null) => AddValueField<FloatFieldCard, FloatValueField, float>(header, description, defaultValue, minLimit, maxLimit, wheelStep, callback, fieldWidth, fieldHeight, beforeLayoutAction);

        public LongFieldCard AddLongField(string header, string description, long defaultValue, long minLimit, long maxLimit, long wheelStep, Action<long> callback = null, float fieldWidth = 100, float fieldHeight = 24, Action<LongFieldCard> beforeLayoutAction = null) => AddValueField<LongFieldCard, LongValueField, long>(header, description, defaultValue, minLimit, maxLimit, wheelStep, callback, fieldWidth, fieldHeight, beforeLayoutAction);

        public IntFieldCard AddIntField(string header, string description, int defaultValue, int minLimit, int maxLimit, int wheelStep, Action<int> callback = null, float fieldWidth = 100, float fieldHeight = 24, Action<IntFieldCard> beforeLayoutAction = null) => AddValueField<IntFieldCard, IntValueField, int>(header, description, defaultValue, minLimit, maxLimit, wheelStep, callback, fieldWidth, fieldHeight, beforeLayoutAction);

        public StringFieldCard AddStringField(string header, string description, string defaultValue, Action<string> callback = null, float fieldWidth = 100, float fieldHeight = 24, Action<StringFieldCard> beforeLayoutAction = null) => AddValueField<StringFieldCard, StringValueField, string>(header, description, defaultValue, string.Empty, null, string.Empty, callback, fieldWidth, fieldHeight, beforeLayoutAction);

        public TValueFieldCard AddValueField<TValueFieldCard, TValueFieldControl, TValue>(
            string header,
            string description,
            TValue defaultValue,
            TValue minLimit,
            TValue maxLimit,
            TValue wheelStep,
            Action<TValue> callback = null,
            float fieldWidth = 100,
            float fieldHeight = 24,
            Action<TValueFieldCard> beforeLayoutAction = null
        )
            where TValueFieldCard : ValueFieldCardBase<TValueFieldControl, TValue>
            where TValueFieldControl : ValueFieldBase<TValue, TValueFieldControl>
            where TValue : IComparable<TValue> {
            var card = AddItemCard<TValueFieldCard, TValueFieldControl>(header, description);
            SetValueFieldSettings(card.Control, defaultValue, minLimit, maxLimit, wheelStep, callback, fieldWidth, fieldHeight);
            beforeLayoutAction?.Invoke(card);
            return card;
        }

        public static TValueFieldControl SetValueFieldSettings<TValueFieldControl, TValue>(
            TValueFieldControl control,
            TValue defaultValue,
            TValue minLimit,
            TValue maxLimit,
            TValue wheelStep,
            Action<TValue> callback = null,
            float fieldWidth = 100,
            float fieldHeight = 24
        )
            where TValueFieldControl : ValueFieldBase<TValue, TValueFieldControl>
            where TValue : IComparable<TValue> {
            control.builtinKeyNavigation = true;
            control.TextPadding.SetTop(6);
            control.size = new Vector2(fieldWidth, fieldHeight);
            control.MinValue = minLimit;
            control.MaxValue = maxLimit;
            control.WheelStep = wheelStep;
            control.CanWheel = true;
            control.ShowTooltip = true;
            control.SelectOnFocus = true;
            control.CursorHeight = 14;
            control.CustomCursorHeight = true;
            control.UseValueLimit = true;
            if (typeof(TValue) == typeof(string)) {
                control.UseValueLimit = false;
                control.builtinKeyNavigation = false;
            }

            control.TextScale = 0.8f;
            control.Value = defaultValue;
            control.EventValueChanged += (_, v) => callback?.Invoke(v);
            control.SetStyle(StyleType.ControlPanelStyle);
            return control;
        }

        public DropDownCard AddDropDown<T>(
            string header,
            string description,
            DropDownItem<T>[] items,
            Func<DropDownItem<T>, bool> isSelected,
            Action<DropDownItem<T>> onChanged,
            float? dropDownWidth,
            float dropDownHeight = 24,
            Action<DropDownLogic<T>> onLogicCreated = null,
            Action<DropDownCard> beforeLayoutAction = null
        ) {
            DropDownLogic<T> logic = null;
            return AddDropDown(
                header,
                description,
                () => {
                    logic = DropDownLogic<T>.Create().SetItems(items, isSelected);
                    logic.LogicSelectionChanged += (_, v) => onChanged?.Invoke(v);
                    onLogicCreated?.Invoke(logic);
                    return logic;
                },
                popup => popup.BindItems<T, DropDownPopupButton>(logic.VisibleLabels, logic.Items),
                dropDownWidth,
                dropDownHeight,
                beforeLayoutAction
            );
        }

        public DropDownCard AddDropDown(string header,
            string description,
            Func<IDropDownLogic> logicFactory,
            Action<FixedDropDownPopup> popupItemsFactory,
            float? dropDownWidth,
            float dropDownHeight = 24,
            Action<DropDownCard> beforeLayoutAction = null
        ) {
            var card = AddItemCard<DropDownCard, DropDownButton>(header, description);
            var dropDownButton = card.Control;

            float newDropDownWidth;
            if (dropDownWidth is null) {
                newDropDownWidth = 100f;
                dropDownButton.size = new Vector2(newDropDownWidth, dropDownHeight);
                dropDownButton.TextPadding.SetLeft(6).SetRight(6);
                dropDownButton.AutoWidth = true;
            }
            else {
                newDropDownWidth = dropDownWidth.Value;
                dropDownButton.size = new Vector2(newDropDownWidth, dropDownHeight);
            }

            dropDownButton.Text = "DropDownButton";
            dropDownButton.FgAtlas = dropDownButton.BgAtlas = Atlases.Shared;
            dropDownButton.BgSprites.SetValues(SharedAtlasKeys.RoundRect6);
            dropDownButton.BgColors.SetValues(UIColors.BgElementColors);
            dropDownButton.BgColors.FocusedColor = UIColors.GreenNormal;
            dropDownButton.FgSprites.SetValues(SharedAtlasKeys.DownArrow);
            dropDownButton.FgColors.DisabledColor = UIColors.White60;
            dropDownButton.FgSpritePadding.Right = 8;
            dropDownButton.FgScaleFactor = 0.6f;
            dropDownButton.FgSpriteMode = ForegroundSpriteMode.Scale;
            dropDownButton.FgHorizontalAlignment = UIHorizontalAlignment.Right;
            dropDownButton.TextHorizontalAlignment = UIHorizontalAlignment.Left;
            dropDownButton.TextPadding.SetAll(8, 8, 4, 0);
            dropDownButton.TextScale = 0.8f;
            dropDownButton.TextColors.DisabledColor = UIColors.White60;
            dropDownButton.RenderFg = true;

            var logic = logicFactory?.Invoke();
            dropDownButton.BindLogic(logic);

            dropDownButton.PopupFactory = owner => {
                var popup = owner.GetRootContainer().AddUIComponent<FixedDropDownPopup>();
                popup.width = 200;
                popup.BgAtlas = Atlases.Shared;
                popup.BgSprites.SetValues(SharedAtlasKeys.RoundRect6);
                popup.BgColors.SetValues(UIColors.BgElementNormal);
                popup.LayoutPadding.SetAll(4);
                popupItemsFactory?.Invoke(popup);

                popup.SetButtonsStyle(button => {
                    button.size = new Vector2(newDropDownWidth - 8, dropDownHeight);
                    button.MinWidth = newDropDownWidth - 8;
                    button.AutoWidth = true;
                    button.TextScale = 0.8f;
                    button.TextPadding.SetAll(4, 4, 4, 0);
                    button.BgSprites.SetValues(SharedAtlasKeys.RoundRect6);
                    button.BgSprites.NormalSprite = string.Empty;
                    button.BgColors.SetValues(UIColors.BgElementNormal);
                    button.BgColors.HoveredColor = UIColors.BgElementHovered;
                    button.BgColors.FocusedColor = UIColors.GreenNormal;
                });

                return popup;
            };

            beforeLayoutAction?.Invoke(card);
            return card;
        }

        public LabelCard AddLabel(string header, string text, string description = null, Action<LabelCard> beforeLayoutAction = null) {
            var card = AddItemCard<LabelCard, Label>(header, description);
            card.Control.TextScale = 0.8f;
            card.Control.Text = text;
            beforeLayoutAction?.Invoke(card);
            return card;
        }

        public ToggleSwitchCard AddToggleSwitch(bool isOn, string header, string description, UIElementEventHandler<bool> callback, Action<ToggleSwitchCard> beforeLayoutAction = null) => AddToggleSwitch(isOn, header, description, (_, v) => callback?.Invoke(v), beforeLayoutAction);

        public ToggleSwitchCard AddToggleSwitch(bool isOn, string header, string description, UIElementEventHandler<ToggleSwitchIndicator, bool> callback, Action<ToggleSwitchCard> beforeLayoutAction = null) {
            var card = AddItemCard<ToggleSwitchCard, ToggleSwitchIndicator>(header, description);
            var button = card.Control;
            button.SetStyle(StyleType.ControlPanelStyle);
            button.autoSize = false;
            button.size = new Vector2(40, 24);
            button.IsOn = isOn;
            button.ToggleChanged += (toggle, v) => callback?.Invoke(toggle as ToggleSwitchIndicator, v);
            beforeLayoutAction?.Invoke(card);
            return card;
        }

        public void RemoveAllItemPanel() {
            foreach (var panel in _itemCards) panel.Self.DestroySelf();

            _itemCards.Clear();
        }

        public TCard AddItemCard<TCard, TControl>(string header = null, string description = null, FlexDirection direction = FlexDirection.Row) where TCard : SettingsCardBase<TControl> where TControl : UIComponent {
            var card = Control.AddUIComponent<TCard>();
            card.width = width;
            card.Direction = direction;
            card.TextElementGap = 4;
            card.RowGap = card.ColumnGap = 10;
            card.BgAtlas = card.FgAtlas = Atlases.Shared;
            card.FgSprites.SetValues(SharedAtlasKeys.LineBottom);
            card.FgSpriteMode = ForegroundSpriteMode.Custom;
            card.FgCustomSize = new Vector2(card.width - 20, 20);
            card.FgVerticalAlignment = UIVerticalAlignment.Bottom;
            card.FgColors.SetValues(UIColors.GroupFgNormal);
            card.RenderFg = true;
            card.LayoutPadding.SetAll(10);
            _itemCards.Add(card);
            RenderItemPanelFg();
            if (string.IsNullOrEmpty(header)) return card;
            card.Header = header;
            card.HeaderElement.TextScale = 0.8f;
            if (string.IsNullOrEmpty(description)) return card;
            card.Description = description;
            card.DescriptionElement.TextScale = 0.7f;
            card.DescriptionElement.TextColors.DisabledColor = UIColors.White50;
            return card;
        }

        private void RenderItemPanelFg() {
            var count = _itemCards.Count;
            for (var i = 0; i < count; i++) _itemCards[i].RenderFg = i != count - 1;
        }
    }
}