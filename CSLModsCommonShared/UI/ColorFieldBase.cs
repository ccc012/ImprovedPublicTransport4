// using System;
// using ColossalFramework.UI;
// using UnityEngine;
//
// namespace CSLModsCommon.UI;
// [Obsolete]
// public class ColorFieldBase<T> : Button where T : ColorPicker {
//     private Color _selectedColor;
//     protected ColorPickerPosition _pickerPosition = ColorPickerPosition.RightBelow;
//     private T _popup;
//     private Color _undoColor;
//
//     public event Action<Color> EventOnSelectedColorChanged;
//     public event Action<T> EventPopupOpen;
//     public event Action<T> EventPopupClose;
//
//     public virtual Color SelectedColor {
//         get => _selectedColor;
//         set {
//             if (value.Equals(_selectedColor)) return;
//             _selectedColor = value;
//             Invalidate();
//         }
//     }
//
//     public ColorPickerPosition PickerPosition {
//         get => _pickerPosition;
//         set {
//             if (value != _pickerPosition) {
//                 ClosePopup();
//                 _pickerPosition = value;
//                 Invalidate();
//             }
//         }
//     }
//
//     public override void Awake() {
//         base.Awake();
//         _fgSpriteMode = ForegroundSpriteMode.Scale;
//         _fgScaleFactor = 0.8f;
//         RenderFg = true;
//     }
//
//     private void CheckForPopupClose() {
//         if (_popup is null || !Input.GetMouseButtonDown(0)) {
//             return;
//         }
//
//         var camera = GetCamera();
//         var ray = camera.ScreenPointToRay(Input.mousePosition);
//         if (_popup.Raycast(ray) || IsHovering) {
//             return;
//         }
//
//         ClosePopup();
//     }
//
//     public override void OnDisable() {
//         base.OnDisable();
//         ClosePopup();
//     }
//
//     public override void OnDestroy() {
//         base.OnDestroy();
//         ClosePopup();
//     }
//
//     public override void Update() {
//         base.Update();
//         CheckForPopupClose();
//     }
//
//     protected override void OnClick(UIMouseEventParameter p) {
//         base.OnClick(p);
//         if (_popup is not null) {
//             ClosePopup();
//         }
//         else {
//             OpenPopup();
//         }
//     }
//
//     private bool OpenPopup() {
//         _undoColor = _selectedColor;
//         if (_popup != null) {
//             return false;
//         }
//
//         var uiComponent = GetRootContainer();
//         _popup = uiComponent.AddUIComponent<T>();
//         _popup.Focus();
//         _popup.RGBColor = _selectedColor;
//         _popup.EventRGBColorChanged += OnSelectedColorChanged;
//         _popup.eventKeyDown += OnPopupKeyDown;
//         var popupPosition = CalculatePopupPosition();
//         _popup.transform.position = popupPosition;
//         _popup.transform.rotation = transform.rotation;
//         EventPopupOpen?.Invoke(_popup);
//         return true;
//     }
//
//     private void OnPopupKeyDown(UIComponent comp, UIKeyEventParameter p) {
//         if (!builtinKeyNavigation) return;
//         if (p.keycode == KeyCode.Space || p.keycode == KeyCode.Return) {
//             ClosePopup();
//             p.Use();
//             return;
//         }
//
//         if (p.keycode == KeyCode.Escape) {
//             _selectedColor = _undoColor;
//             _popup.RGBColor = _selectedColor;
//             ClosePopup();
//             p.Use();
//         }
//     }
//
//     private void ClosePopup() {
//         if (_popup is null) {
//             return;
//         }
//
//         _popup.EventRGBColorChanged -= OnSelectedColorChanged;
//         EventPopupClose?.Invoke(_popup);
//         Destroy(_popup.gameObject);
//         _popup = null;
//     }
//
//     protected virtual void OnSelectedColorChanged(Color selectedColor) {
//         SelectedColor = selectedColor;
//         EventOnSelectedColorChanged?.Invoke(selectedColor);
//     }
//
//     private Vector3 CalculatePopupPosition() {
//         var num = PixelsToUnits();
//         var a = pivot.TransformToUpperLeft(size, arbitraryPivotOffset);
//         var a2 = transform.position + a * num;
//         var scaledDirection = GetScaledDirection(Vector3.down);
//         var scaledDirection2 = GetScaledDirection(Vector3.right);
//         var vector = Vector3.zero;
//         if (PickerPosition == ColorPickerPosition.RightAbove) {
//             vector = a2 - scaledDirection * _popup.size.y * num;
//         }
//         else if (PickerPosition == ColorPickerPosition.LeftAbove) {
//             vector = a2 - scaledDirection * _popup.size.y * num - scaledDirection2 * (_popup.size.x - size.x) * num;
//         }
//         else if (PickerPosition == ColorPickerPosition.RightBelow) {
//             vector = a2 + scaledDirection * size.y * num;
//         }
//         else if (PickerPosition == ColorPickerPosition.LeftBelow) {
//             vector = a2 + scaledDirection * size.y * num - scaledDirection2 * (_popup.size.x - size.x) * num;
//         }
//
//         var a3 = _popup.transform.parent.position / num + _popup.parent.pivot.TransformToUpperLeft(size, arbitraryPivotOffset);
//         var vector2 = a3 + scaledDirection * _popup.parent.size.y + scaledDirection2 * _popup.parent.size.x;
//         var a4 = vector / num;
//         var vector3 = a4 + scaledDirection * _popup.size.y + scaledDirection2 * _popup.size.x;
//         if (a4.x < a3.x) {
//             vector.x = a2.x;
//         }
//         else if (vector3.x > vector2.x) {
//             vector.x = a2.x - (_popup.size.x - size.x) * num;
//         }
//
//         if (a4.y > a3.y) {
//             vector.y = a2.y - size.y * num;
//         }
//         else if (vector3.y < vector2.y) {
//             vector.y = a2.y + _popup.size.y * num;
//         }
//
//         return vector;
//     }
//
//     protected override Color32 GetFgRenderColor() => SelectedColor;
// }

