using ColossalFramework;
using ColossalFramework.UI;
using System;
using UnityEngine;

namespace CSLModsCommon.Utilities;

public static class Animation {
    public static void AnimateIn(UIComponent component, Action completed = null) {
        component.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
        ValueAnimator.Animate($"{component.GetType().FullName}InAnimate", val => {
            var scale = Mathf.Lerp(0.8f, 1f, val);
            component.transform.localScale = new Vector3(scale, scale, 1f);
        }, new AnimatedFloat(0f, 1f, 0.3f, EasingType.ExpoEaseOut), completed);
    }

    public static void AnimateOut(UIComponent component, Action completed = null) => ValueAnimator.Animate($"{component.GetType().FullName}OutAnimate", val => {
        var scale = Mathf.Lerp(1f, 0.9f, val);
        component.transform.localScale = new Vector3(scale, scale, 1f);
    }, new AnimatedFloat(0f, 1f, 0.2f, EasingType.ExpoEaseIn), completed);
}