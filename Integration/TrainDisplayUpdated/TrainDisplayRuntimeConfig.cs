// <copyright file="TrainDisplayRuntimeConfig.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the GPL-3.0 license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace ImprovedPublicTransport.Integration.TrainDisplayUpdated
{
    using UnityEngine;

    internal static class TrainDisplayRuntimeConfig
    {
        internal static bool FirstPersonOnly => ModSetting.Instance.TrainDisplayFirstPersonOnly;
        internal static Vector2 OverlayOffset { get; set; } = Vector2.zero;
        internal static float Padding { get; set; } = 10f;
        internal static float LineSpacing { get; set; } = 1.05f;

        internal static ModSetting.TrainDisplayFields VisibleFields => ModSetting.Instance.TrainDisplayVisibleFields;
        internal static ModSetting.TrainDisplayOverlayPositions OverlayPosition => ModSetting.Instance.TrainDisplayOverlayPosition;
        internal static float OverlayScale => ModSetting.Instance.TrainDisplayOverlayScale;
        internal static float OverlayOpacity => ModSetting.Instance.TrainDisplayOverlayOpacity;
        internal static float UpdateInterval => ModSetting.Instance.TrainDisplayUpdateInterval;
        internal static ModSetting.TrainDisplayModes Mode => ModSetting.Instance.TrainDisplayMode;

        // Simple/Dark/Light presets - kept intentionally minimal rather than a full theming system.
        internal static Color BackgroundColor => ModSetting.Instance.TrainDisplayColorTheme switch
        {
            ModSetting.TrainDisplayColorThemes.Dark => new Color(0f, 0f, 0f, 0.85f),
            ModSetting.TrainDisplayColorThemes.Light => new Color(1f, 1f, 1f, 0.85f),
            _ => new Color(0f, 0f, 0f, 0.55f),
        };

        internal static Color TextColor => ModSetting.Instance.TrainDisplayColorTheme switch
        {
            ModSetting.TrainDisplayColorThemes.Light => Color.black,
            _ => Color.white,
        };
    }
}
