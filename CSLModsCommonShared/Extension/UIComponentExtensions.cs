using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace CSLModsCommon.Extension;

public static class UIComponentExtensions {
    public static T AddUIComponent<T>(this UIComponent parent, Action<T> configure) where T : UIComponent {
        var component = parent.AddUIComponent<T>();
        configure?.Invoke(component);
        return component;
    }

    public static void DestroyChildrenAndSelf(this UIComponent component) {
        if (component == null) return;
        component.DestroyAllChildren();
        component.DestroySelf();
    }

    public static void DestroySelf(this UIComponent component) {
        if (component == null) return;
        if (component.parent != null) component.RemoveUIComponent(component);

        if (component.gameObject != null) Object.Destroy(component.gameObject);
    }

    public static void DestroyAllChildren(this UIComponent component) {
        if (component == null || component.parent == null) return;
        var children = new List<UIComponent>(component.components);
        foreach (var child in children) child.DestroySelf();
    }
}