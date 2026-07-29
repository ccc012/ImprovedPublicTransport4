// using System;
// using ColossalFramework.Math;
// using ColossalFramework.UI;
// using CSLModsCommon.Common;
// using CSLModsCommon.KeyBindings;
// using CSLModsCommon.Localization;
// using CSLModsCommon.UI.Atlas;
// using CSLModsCommon.UI.Utilities;
// using UnityEngine;
//
// namespace CSLModsCommon.UI;
// [Obsolete]
// public class ColorPicker : Panel {
//     protected UITextureSprite _hsbField;
//     protected UITextureSprite _hueField;
//     protected UISlicedSprite _hsbIndicator;
//     protected Slider _hueSlider;
//     protected Color _rgbColor = Color.white;
//     protected Color _hue = Color.white;
//     protected static readonly Texture2D BlankTexture = TextureLoader.CreateTexture(16, 16, Color.white);
//     protected ByteValueField _rValueField;
//     protected ByteValueField _gValueField;
//     protected ByteValueField _bValueField;
//
//     public event Action<Color> EventRGBColorChanged;
//
//     public bool Processing { get; private set; }
//
//     public Color RGBColor {
//         get => _rgbColor;
//         set => OnRGBColorChanged(value, UpdateRGBColor);
//     }
//
//     public bool IsMouseHovering => m_IsMouseHovering;
//
//     protected virtual void UpdateRGBColor(Color rgbColor) {
//         UpdateHue(rgbColor);
//         UpdateHsbIndicator(rgbColor);
//         UpdateHsbField();
//         UpdateRGBField(rgbColor);
//     }
//
//     protected Color32 ColorFromField => new(_rValueField.Value, _gValueField.Value, _bValueField.Value, 255);
//
//     protected virtual void OnRGBColorChanged(Color rgbColor, Action<Color> callback = null, bool invokeAction = true) {
//         _rgbColor = rgbColor;
//         callback?.Invoke(rgbColor);
//         if (invokeAction) {
//             EventRGBColorChanged?.Invoke(rgbColor);
//         }
//     }
//
//     public override void Awake() {
//         base.Awake();
//         InitComponents();
//     }
//
//     protected virtual void InitComponents() {
//         size = new Vector2(246, 246);
//         _bgAtlas = Atlases.Shared;
//         _bgSprites.SetSprites(SharedAtlasKeys.RoundRect6);
//         _bgColors.SetColors(UIColors.GroupBgNormal);
//         _hsbField = AddUIComponent<UITextureSprite>();
//         _hsbField.material = new Material(Shader.Find("UI/ColorPicker HSB"));
//         _hsbField.texture = BlankTexture;
//         _hsbField.size = new Vector2(200f, 200f);
//         _hsbField.canFocus = true;
//         _hsbField.eventMouseDown += IndicatorDown;
//         _hsbField.eventMouseMove += IndicatorMove;
//         _hsbField.relativePosition = new Vector2(10, 10);
//
//         _hsbIndicator = _hsbField.AddUIComponent<UISlicedSprite>();
//         _hsbIndicator.size = new Vector2(16, 16);
//         _hsbIndicator.atlas = Atlases.InGame;
//         _hsbIndicator.spriteName = "ColorPickerIndicator";
//
//         _hueField = AddUIComponent<UITextureSprite>();
//         _hueField.material = new Material(Shader.Find("UI/ColorPicker Hue"));
//         _hueField.texture = BlankTexture;
//         _hueField.size = new Vector2(16f, 200f);
//         _hueField.relativePosition = new Vector2(220, 10);
//
//         _hueSlider = _hueField.AddUIComponent<Slider>();
//         _hueSlider.size = new Vector2(16f, 200f);
//         _hueSlider.Orientation = UIOrientation.Vertical;
//         _hueSlider.relativePosition = Vector2.zero;
//         _hueSlider.MinValue = 0f;
//         _hueSlider.MaxValue = 1f;
//         _hueSlider.StepSize = 0.01f;
//         _hueSlider.Value = 0f;
//         _hueSlider.EventValueChanged += OnHueChanged;
//
//         var thumbObject = _hueSlider.AddUIComponent<UISprite>();
//         thumbObject.atlas = Atlases.Shared;
//         thumbObject.spriteName = CustomUITextureAtlas.CheckBoxOffBg;
//         thumbObject.color = new Color32(220, 220, 220, 255);
//         thumbObject.disabledColor = new Color32(110, 110, 110, 255);
//         thumbObject.size = new Vector2(14, 14);
//         thumbObject.relativePosition = Vector2.zero;
//         _hueSlider.ThumbObject = thumbObject;
//         _hueSlider.ThumbPadding.SetAll(0, 0, 7, 7);
//
//         var fieldsPanel = AddUIComponent<Panel>();
//         fieldsPanel.AutoLayoutDirection = LayoutDirection.Horizontal;
//         fieldsPanel.ItemGap = 8;
//         fieldsPanel.AutoFitChildrenHorizontally = true;
//         fieldsPanel.AutoFitChildrenVertically = true;
//         fieldsPanel.AutoLayout = true;
//         fieldsPanel.relativePosition = new Vector2(10, 220);
//         AddLabel(fieldsPanel, "R");
//         _rValueField = AddField(fieldsPanel);
//         AddLabel(fieldsPanel, "G");
//         _gValueField = AddField(fieldsPanel);
//         AddLabel(fieldsPanel, "B");
//         _bValueField = AddField(fieldsPanel);
//     }
//
//     protected virtual TextBlock AddLabel(UIComponent parentComponent, string text) {
//         var label = parentComponent.AddUIComponent<TextBlock>();
//         label.AutoSize = false;
//         label.size = new Vector2(8, 20);
//         label.TextHorizontalAlignment = UIHorizontalAlignment.Center;
//         label.TextVerticalAlignment = UIVerticalAlignment.Middle;
//         label.TextScale = 0.8f;
//         label.TextPadding.Top=4 ;
//         label.Text = text;
//         return label;
//     }
//
//     protected virtual ByteValueField AddField(UIComponent parentComponent) {
//         var field = parentComponent.AddUIComponent<ByteValueField>();
//         field.SelectOnFocus = true;
//         field.size = new Vector2(50, 20);
//         field.TextScale = 0.8f;
//         field.CanWheel = true;
//         field.MinValue = 0;
//         field.MaxValue = 255;
//         field.UseValueLimit = true;
//         field.builtinKeyNavigation = true;
//         field.SetStyle(StyleType.ControlPanelStyle);
//         field.Value = 0;
//         field.TextPadding.Top=4 ;
//         field.CursorHeight = 12;
//         field.CustomCursorHeight = true;
//         field.EventValueChanged += OnValueChanged;
//         field.WheelStep = 10;
//         field.ShowTooltip = true;
//         field.tooltip = SharedTranslations.ScrollWheel + "\n" + SharedTranslations.AltToChangeAll;
//         return field;
//     }
//
//     private void OnValueChanged(ByteValueField byteValueField, byte value) {
//         if (Processing)
//             return;
//         Processing = true;
//         if (ModifierFlagsExtensions.IsAltDown()) {
//             _rValueField.Value = value;
//             _gValueField.Value = value;
//             _bValueField.Value = value;
//         }
//
//         UpdateHsbIndicator(ColorFromField);
//         UpdateHue(ColorFromField);
//         UpdateHsbField();
//         OnRGBColorChanged(ColorFromField, null, false);
//         Processing = false;
//     }
//
//     private void OnHueChanged(Slider slider, float value) {
//         _hue = new HSBColor(value, 1f, 1f, 1f).ToColor();
//         UpdateHsbField();
//         UpdateSelectedColor();
//         UpdateRGBField(RGBColor);
//     }
//
//     private void IndicatorDown(UIComponent comp, UIMouseEventParameter p) => UpdateHsbIndicator(p);
//
//     private void IndicatorMove(UIComponent comp, UIMouseEventParameter p) {
//         if (p.buttons == UIMouseButton.Left)
//             UpdateHsbIndicator(p);
//     }
//
//     private void UpdateHsbIndicator(UIMouseEventParameter p) {
//         if (_hsbField.GetHitPosition(p.ray, out var a)) {
//             _hsbIndicator.relativePosition = a - _hsbIndicator.size * 0.5f;
//             UpdateSelectedColor();
//         }
//     }
//
//     private void UpdateSelectedColor() {
//         var vector = new Vector2(_hsbIndicator.relativePosition.x, _hsbIndicator.relativePosition.y) + _hsbIndicator.size * 0.5f;
//         var rgbColor = GetColor(vector.x, vector.y, _hsbField.width, _hsbField.height, _hue);
//         OnRGBColorChanged(rgbColor);
//         UpdateRGBField(RGBColor);
//     }
//
//     private void UpdateRGBField(Color32 rgbColor) {
//         if (Processing)
//             return;
//         Processing = true;
//         _rValueField.Value = rgbColor.r;
//         _gValueField.Value = rgbColor.g;
//         _bValueField.Value = rgbColor.b;
//         Processing = false;
//     }
//
//     private void UpdateHue(Color rgbColor) {
//         var hsbColor = HSBColor.FromColor(rgbColor);
//         _hue = new HSBColor(hsbColor.h, 1f, 1f, 1f);
//         _hueSlider.Value = HSBColor.FromColor(_hue).h;
//     }
//
//     private Color GetColor(float x, float y, float countWidth, float countHeight, Color hue) {
//         var num = x / countWidth;
//         var num2 = y / countHeight;
//         num = Mathf.Clamp01(num);
//         num2 = Mathf.Clamp01(num2);
//         var result = Color.Lerp(Color.white, hue, num) * (1f - num2);
//         result.a = 1f;
//         return result;
//     }
//
//     private void UpdateHsbField() {
//         if (_hsbField.renderMaterial is not null) {
//             _hsbField.renderMaterial.color = _hue.gamma;
//         }
//     }
//
//     private void UpdateHsbIndicator(Color rgbColor) {
//         var hsbColor = HSBColor.FromColor(rgbColor);
//         var newSize = new Vector2(hsbColor.s * _hsbField.width, (1f - hsbColor.b) * _hsbField.height);
//         _hsbIndicator.relativePosition = newSize - _hsbIndicator.size * 0.5f;
//     }
// }

