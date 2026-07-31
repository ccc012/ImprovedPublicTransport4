using ColossalFramework.UI;
using CSLModsCommon.Collections;
using CSLModsCommon.KeyBindings;
using CSLModsCommon.UI.Atlas;
using CSLModsCommon.UI.Buttons;
using CSLModsCommon.UI.Buttons.RadioButtons;
using CSLModsCommon.UI.Containers;
using CSLModsCommon.UI.DropDown;
using CSLModsCommon.UI.Labels;
using CSLModsCommon.UI.SettingsCard;
using CSLModsCommon.UI.Sliders;
using System;
using UnityEngine;

namespace CSLModsCommon.UI.OptionsPanel;

public abstract partial class OptionsPanelBase {
    public sealed class SettingsSection : SettingsCardBase<LiteContainer> {
        private ReusableList<ISettingsCard> _itemCards;
        
        public CSLModsCommon.Collections.IReadOnlyList<ISettingsCard> ItemCards => _itemCards;

        public override void Awake() {
            base.Awake();
            m_Size = new Vector2(OptionsPanelLayout.SectionWidth, m_Size.y);
            _direction = FlexDirection.Column;
            _textElementGap = _rowGap = 4;
            _itemCards = ReusableList<ISettingsCard>.Rent();
            Control = AddUIComponent<LiteContainer>();
            Control.width = width;
            Control.AutoLayout = true;
            Control.AutoFitChildrenVertically = true;
            Control.BgAtlas = Atlases.Shared;
            Control.BgSprites.SetValues(SharedAtlasKeys.RoundRect12);
            Control.BgColors.SetValues(UIColors.GroupBg1);
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

        public EmptyFlexContainerCard AddEmptyFlexContainer(string header, string description, Action<EmptyFlexContainerCard> beforeLayoutAction = null) {
            var card = AddItemCard<EmptyFlexContainerCard, FlexContainer>(header, description);
            beforeLayoutAction?.Invoke(card);
            return card;
        }

        public EmptyLiteContainerCard AddEmptyLiteContainer(string header, string description, Action<EmptyLiteContainerCard> beforeLayoutAction = null) {
            var card = AddItemCard<EmptyLiteContainerCard, LiteContainer>(header, description);
            beforeLayoutAction?.Invoke(card);
            return card;
        }

        public KeyBindingCard AddKeyBinding(KeyBinding keyBinding, string header, string description = null, Action<KeyBindingCard> beforeLayoutAction = null) {
            var card = AddItemCard<KeyBindingCard, UIKeyBindingControls>(header, description);
            card.Control.Binding = keyBinding;
            beforeLayoutAction?.Invoke(card);
            return card;
        }

        public CheckBoxCard AddCheckBox(bool isChecked, string checkBoxText, string header = null, string description = null, UIElementEventHandler<bool> callback = null, Action<CheckBoxCard> beforeLayoutAction = null) => AddCheckBox(isChecked, checkBoxText, header, description, (_, b) => callback?.Invoke(b), beforeLayoutAction);

        public CheckBoxCard AddCheckBox(bool isChecked, string checkBoxText, string header = null, string description = null, UIElementEventHandler<CheckBox, bool> callback = null, Action<CheckBoxCard> beforeLayoutAction = null) {
            var card = AddItemCard<CheckBoxCard, CheckBox>(header, description);
            var checkBox = card.Control;
            // With no description sharing the row, stretch the checkbox to fill the whole card so
            // its own label has all the room it needs.
            //
            // With a description present, DON'T leave checkBox.width untouched either: CheckBox
            // defaults to width=100 (see CheckBox.Awake()) and derives its own label's width from
            // that internally (UpdateTextWidth() - width minus the indicator and padding), which
            // left only ~50-60px for the checkbox's own text. WordWrap then had to break every
            // Portuguese word in the label (e.g. "automaticamente", "informações") onto its own
            // line, since no single word fit - the exact same one-character-per-line symptom as the
            // original bug, just moved from the description onto the checkbox's own label instead of
            // being fixed. A fixed, generous width here leaves the rest of the row for the
            // description via ArrangeRow()'s own width - Control.width calculation.
            checkBox.width = string.IsNullOrEmpty(description)
                ? card.width - card.LayoutPadding.Horizontal
                : 320f;
            checkBox.LabelElement.TextPadding.SetTop(1);
            checkBox.CheckBoxIndicatorElement.SetStyle(StyleType.OptionPanelStyle);
            checkBox.IsChecked = isChecked;
            checkBox.Text = checkBoxText;
            checkBox.CheckChanged += callback;
            beforeLayoutAction?.Invoke(card);
            return card;
        }

        public RadioGroupCard AddEnumRadioGroup<TEnum>(
            string header,
            string description,
            TEnum currentValue,
            Action<TEnum> onChanged,
            Func<TEnum, string> getDisplayName = null
        ) where TEnum : Enum {
            var card = AddItemCard<RadioGroupCard, RadioGroup>(header, description, FlexDirection.Column);
            var group = card.Control;
            group.RowGap = 6;
            group.width = card.width - card.LayoutPadding.Horizontal;

            var values = Enum.GetValues(typeof(TEnum));
            var count = values.Length;
            var items = new RadioButtonItem<TEnum>[count];

            for (var i = 0; i < count; i++) {
                var value = (TEnum)values.GetValue(i)!;
                var displayName = getDisplayName != null ? getDisplayName(value) : value.ToString();
                items[i] = new RadioButtonItem<TEnum>(value, group.AddOption(displayName));
            }

            var logic = RadioGroupLogic<TEnum>.Create();
            logic.AddRange(items);
            logic.SetDefault(v => v.Value.Equals(currentValue));
            logic.SelectionChanged += v => onChanged?.Invoke(v.Value);
            foreach (var button in group.Buttons)
                button?.TextElement?.TextPadding.SetTop(1);
            return card;
        }

        public RadioGroupLogic<V> AddRadioGroup<V>(
            string header,
            string description,
            Action<RadioGroup> options,
            Action<RadioGroupLogic<V>> logicAction = null,
            Action<RadioGroupCard> beforeLayoutAction = null
        ) {
            var card = AddItemCard<RadioGroupCard, RadioGroup>(header, description, FlexDirection.Column);
            var group = card.Control;
            group.RowGap = 6;
            group.width = card.width - card.LayoutPadding.Horizontal;

            var logic = RadioGroupLogic<V>.Create();
            options?.Invoke(group);
            foreach (var button in group.Buttons) button?.TextElement?.TextPadding.SetTop(1);

            logicAction?.Invoke(logic);
            beforeLayoutAction?.Invoke(card);
            return logic;
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
            foreach (var radioButton in radioGroup.Buttons) radioButton?.TextElement?.TextPadding.SetTop(1);

            beforeLayoutAction?.Invoke(card);
            return logic;
        }

        public RadioGroupCard AddRadioGroup(string header, string description, Action<RadioGroup> options, Action<RadioGroupCard> beforeLayoutAction = null) {
            var card = AddItemCard<RadioGroupCard, RadioGroup>(header, description, FlexDirection.Column);
            var radioGroup = card.Control;
            radioGroup.RowGap = 6;
            radioGroup.width = card.width - card.LayoutPadding.Horizontal;
            options?.Invoke(radioGroup);
            foreach (var radioButton in radioGroup.Buttons) radioButton?.TextElement?.TextPadding.SetTop(1);

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
            container.ColumnGap = 8;
            resisterButtons?.Invoke(card);

            foreach (var button in card.Buttons.Values) {
                button.autoSize = false;
                button.TextScale = 0.9f;
                button.height = 30;
                button.TextPadding.SetAll(10, 10, 4, 0);
                button.SetStyle(StyleType.OptionPanelStyle);
                button.AutoWidth = true;
            }

            beforeLayoutAction?.Invoke(card);
            return card;
        }

        public NormalButtonCard AddButton(string header, string description, string buttonText, UIElementEventHandler onButtonClicked = null, Action<NormalButtonCard> beforeLayoutAction = null) => AddButton(header, description, buttonText, null, 30, _ => onButtonClicked?.Invoke(), beforeLayoutAction);

        public NormalButtonCard AddButton(string header, string description, string buttonText, float? buttonWidth, float buttonHeight = 30, UIElementEventHandler<NormalButton> onButtonClicked = null, Action<NormalButtonCard> beforeLayoutAction = null) {
            var panel = AddItemCard<NormalButtonCard, NormalButton>(header, description);
            var button = panel.Control;
            button.autoSize = false;
            button.TextScale = 0.9f;
            button.height = buttonHeight;
            button.TextPadding.SetAll(10, 10, 4, 0);
            button.SetStyle(StyleType.OptionPanelStyle);
            button.Text = buttonText;
            if (buttonWidth is not null)
                button.width = (float)buttonWidth;
            else
                button.AutoWidth = true;
            button.eventClicked += (c, _) => onButtonClicked?.Invoke(c as NormalButton);
            beforeLayoutAction?.Invoke(panel);
            return panel;
        }

        public SliderCard AddSlider(string header, string description, float minValue, float maxValue, float stepValue, float defaultValue, Action<float> callback = null, float sliderWidth = 700, float sliderHeight = 16, bool gradientStyle = false, Action<SliderCard> beforeLayoutAction = null) {
            var card = AddItemCard<SliderCard, Slider>(header, description, FlexDirection.Column);
            var slider = card.Control;
            slider.size = new Vector2(sliderWidth, sliderHeight);
            slider.MinValue = minValue;
            slider.MaxValue = maxValue;
            slider.StepSize = stepValue;
            slider.Value = defaultValue;
            slider.ValueChanged += (_, v) => callback?.Invoke(v);
            if (gradientStyle)
                slider.SetGradientStyle();
            else
                slider.SetBlueStyle();
            beforeLayoutAction?.Invoke(card);
            return card;
        }

        public ByteFieldCard AddByteField(string header, string description, byte defaultValue, byte minLimit, byte maxLimit, Action<byte> callback = null, float fieldWidth = 100, float fieldHeight = 28, Action<ByteFieldCard> beforeLayoutAction = null) => AddValueField<ByteFieldCard, ByteValueField, byte>(header, description, defaultValue, minLimit, maxLimit, callback, fieldWidth, fieldHeight, beforeLayoutAction);

        public FloatFieldCard AddFloatField(string header, string description, float defaultValue, float minLimit, float maxLimit, Action<float> callback = null, float fieldWidth = 100, float fieldHeight = 28, Action<FloatFieldCard> beforeLayoutAction = null) => AddValueField<FloatFieldCard, FloatValueField, float>(header, description, defaultValue, minLimit, maxLimit, callback, fieldWidth, fieldHeight, beforeLayoutAction);

        public LongFieldCard AddLongField(string header, string description, long defaultValue, long minLimit, long maxLimit, Action<long> callback = null, float fieldWidth = 100, float fieldHeight = 28, Action<LongFieldCard> beforeLayoutAction = null) => AddValueField<LongFieldCard, LongValueField, long>(header, description, defaultValue, minLimit, maxLimit, callback, fieldWidth, fieldHeight, beforeLayoutAction);

        public IntFieldCard AddIntField(string header, string description, int defaultValue, int minLimit, int maxLimit, Action<int> callback = null, float fieldWidth = 100, float fieldHeight = 28, Action<IntFieldCard> beforeLayoutAction = null) => AddValueField<IntFieldCard, IntValueField, int>(header, description, defaultValue, minLimit, maxLimit, callback, fieldWidth, fieldHeight, beforeLayoutAction);

        public StringFieldCard AddStringField(string header, string description, string defaultValue, Action<string> callback = null, float fieldWidth = 100, float fieldHeight = 28, Action<StringFieldCard> beforeLayoutAction = null) => AddValueField<StringFieldCard, StringValueField, string>(header, description, defaultValue, string.Empty, null, callback, fieldWidth, fieldHeight, beforeLayoutAction);

        public TValueFieldCard AddValueField<TValueFieldCard, TValueFieldControl, TValue>(string header,
            string description,
            TValue defaultValue,
            TValue minLimit,
            TValue maxLimit,
            Action<TValue> callback = null,
            float fieldWidth = 100,
            float fieldHeight = 28,
            Action<TValueFieldCard> beforeLayoutAction = null
        )
            where TValueFieldCard : ValueFieldCardBase<TValueFieldControl, TValue>
            where TValueFieldControl : ValueFieldBase<TValue, TValueFieldControl>
            where TValue : IComparable<TValue> {
            var card = AddItemCard<TValueFieldCard, TValueFieldControl>(header, description);
            card.Control.builtinKeyNavigation = true;
            card.Control.TextPadding.SetTop(6);
            card.Control.size = new Vector2(fieldWidth, fieldHeight);
            card.Control.MinValue = minLimit;
            card.Control.MaxValue = maxLimit;
            card.Control.UseValueLimit = true;
            if (typeof(TValue) == typeof(string)) {
                card.Control.UseValueLimit = false;
                card.Control.builtinKeyNavigation = false;
            }

            card.Control.TextScale = 0.9f;
            card.Control.Value = defaultValue;
            card.Control.EventValueChanged += (_, v) => callback?.Invoke(v);
            card.Control.SetStyle(StyleType.OptionPanelStyle);
            beforeLayoutAction?.Invoke(card);
            return card;
        }

        public DropDownCard AddDropDown<T>(
            string header,
            string description,
            DropDownItem<T>[] items,
            Func<DropDownItem<T>, bool> isSelected,
            Action<DropDownItem<T>> onChanged,
            float? dropDownWidth,
            float dropDownHeight = 30,
            Action<DropDownCard> beforeLayoutAction = null
        ) {
            DropDownLogic<T> logic = null;
            return AddDropDown(
                header,
                description,
                () => {
                    logic = DropDownLogic<T>.Create().SetItems(items, isSelected);
                    logic.LogicSelectionChanged += (_, v) => onChanged?.Invoke(v);
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
            float dropDownHeight = 30,
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
            dropDownButton.BgSprites.SetValues(SharedAtlasKeys.RoundRect8);
            dropDownButton.BgColors.SetValues(UIColors.Bg1ElementColors);
            dropDownButton.BgColors.FocusedColor = UIColors.BlueNormal;
            dropDownButton.FgSprites.SetValues(SharedAtlasKeys.DownArrow);
            dropDownButton.FgColors.DisabledColor = UIColors.White60;
            dropDownButton.FgSpritePadding.Right = 8;
            dropDownButton.FgScaleFactor = 0.6f;
            dropDownButton.FgSpriteMode = ForegroundSpriteMode.Scale;
            dropDownButton.FgHorizontalAlignment = UIHorizontalAlignment.Right;
            dropDownButton.TextHorizontalAlignment = UIHorizontalAlignment.Left;
            dropDownButton.TextPadding.SetAll(8, 8, 4, 0);
            dropDownButton.TextScale = 0.9f;
            dropDownButton.TextColors.DisabledColor = UIColors.White60;
            dropDownButton.RenderFg = true;

            var logic = logicFactory?.Invoke();
            dropDownButton.BindLogic(logic);

            dropDownButton.PopupFactory = owner => {
                var popup = owner.GetRootContainer().AddUIComponent<FixedDropDownPopup>();
                popup.width = 200;
                popup.BgAtlas = Atlases.Shared;
                popup.BgSprites.SetValues(SharedAtlasKeys.RoundRect8);
                popup.BgColors.SetValues(UIColors.Bg1ElementNormal);
                popup.LayoutPadding.SetAll(4);
                popupItemsFactory?.Invoke(popup);

                popup.SetButtonsStyle(button => {
                    button.size = new Vector2(newDropDownWidth - 8, 30);
                    button.MinWidth = newDropDownWidth - 8;
                    button.AutoWidth = true;
                    button.TextScale = 0.9f;
                    button.TextPadding.SetAll(4, 4, 4, 0);
                    button.BgSprites.SetValues(SharedAtlasKeys.RoundRect8);
                    button.BgSprites.NormalSprite = string.Empty;
                    button.BgColors.SetValues(UIColors.Bg1ElementNormal);
                    button.BgColors.HoveredColor = UIColors.Bg1ElementHovered;
                    button.BgColors.FocusedColor = UIColors.BlueNormal;
                });

                return popup;
            };

            beforeLayoutAction?.Invoke(card);
            return card;
        }

        public LabelCard AddLabel(string header, string text, string description = null, Action<LabelCard> beforeLayoutAction = null) {
            var card = AddItemCard<LabelCard, Label>(header, description);
            card.Control.Text = text;
            beforeLayoutAction?.Invoke(card);
            return card;
        }

        public ToggleSwitchCard AddToggleSwitch(bool isOn, string header, string description, UIElementEventHandler<bool> callback, Action<ToggleSwitchCard> beforeLayoutAction = null) => AddToggleSwitch(isOn, header, description, (_, v) => callback?.Invoke(v), beforeLayoutAction);

        public ToggleSwitchCard AddToggleSwitch(bool isOn, string header, string description, UIElementEventHandler<ToggleSwitchIndicator, bool> callback, Action<ToggleSwitchCard> beforeLayoutAction = null) {
            var card = AddItemCard<ToggleSwitchCard, ToggleSwitchIndicator>(header, description);
            var button = card.Control;
            button.SetStyle(StyleType.OptionPanelStyle);
            button.autoSize = false;
            button.size = new Vector2(40, 24);
            button.IsOn = isOn;
            button.ToggleChanged += (toggle, v) => callback?.Invoke(toggle as ToggleSwitchIndicator, v);
            beforeLayoutAction?.Invoke(card);
            return card;
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
            card.FgCustomSize = new Vector2(OptionsPanelLayout.SectionWidth - 32, 20);
            card.FgVerticalAlignment = UIVerticalAlignment.Bottom;
            card.FgColors.SetValues(UIColors.GroupFg1);
            card.RenderFg = true;
            card.LayoutPadding.SetAll(16, 16, 14, 14);
            _itemCards.Add(card);
            RenderItemPanelFg();
            // header and description are independent - a card with no header (every checkbox in this
            // project passes header: null, relying only on its own inline label) still needs its
            // description to render, since that is where callers put the explanatory tooltip text.
            // An early return keyed only on header silently dropped every such description.
            if (!string.IsNullOrEmpty(header))
            {
                card.Header = header;
            }
            if (string.IsNullOrEmpty(description)) return card;
            card.Description = description;
            card.DescriptionElement.TextColors.DisabledColor = UIColors.White50;
            return card;
        }

        public void RenderItemPanelFg() {
            var count = _itemCards.Count;
            for (var i = 0; i < count; i++) _itemCards[i].RenderFg = i != count - 1;
        }
    }
}