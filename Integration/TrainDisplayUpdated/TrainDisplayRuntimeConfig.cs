// <copyright file="TrainDisplayRuntimeConfig.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the GPL-3.0 license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace ImprovedPublicTransport.Integration.TrainDisplayUpdated
{
    using UnityEngine;

    internal static class TrainDisplayRuntimeConfig
    {
        /// <summary>
        /// Former "100%" was too small; user-facing 100% now equals ~165% of the old base size.
        /// </summary>
        internal const float BaseScaleBoost = 1.65f;

        internal static Vector2 OverlayOffset { get; set; } = Vector2.zero;
        internal static float Padding { get; set; } = 10f;
        internal static float LineSpacing { get; set; } = 1.05f;

        internal static ModSetting.TrainDisplayFields VisibleFields => ModSetting.Instance.TrainDisplayVisibleFields;
        internal static ModSetting.TrainDisplayOverlayPositions OverlayPosition => ModSetting.Instance.TrainDisplayOverlayPosition;
        /// <summary>User slider 0.5–2.0; multiply by <see cref="BaseScaleBoost"/> for draw size.</summary>
        internal static float OverlayScale => Mathf.Clamp(ModSetting.Instance.TrainDisplayOverlayScale, 0.5f, 2f);
        internal static float DrawScale => OverlayScale * BaseScaleBoost;
        internal static float OverlayOpacity => ModSetting.Instance.TrainDisplayOverlayOpacity;
        internal static float UpdateInterval => ModSetting.Instance.TrainDisplayUpdateInterval;
        internal static ModSetting.TrainDisplayModes Mode => ModSetting.Instance.TrainDisplayMode;
        internal static ModSetting.TrainDisplayColorThemes ColorTheme => ModSetting.Instance.TrainDisplayColorTheme;

        /// <summary>
        /// Face fill of the single corner panel. Original = solid white.
        /// Dark/Light = semi-transparent black/white. Colours = pastel (pulled toward white).
        /// </summary>
        internal static Color PanelFaceColor => ModSetting.Instance.TrainDisplayColorTheme switch
        {
            ModSetting.TrainDisplayColorThemes.Original => new Color(1f, 1f, 1f, 0.98f),
            // Solid black.
            ModSetting.TrainDisplayColorThemes.Dark => new Color(0.05f, 0.05f, 0.06f, 0.98f),
            // Semi-transparent black / white.
            ModSetting.TrainDisplayColorThemes.BlackSemi => new Color(0.06f, 0.06f, 0.08f, 0.70f),
            ModSetting.TrainDisplayColorThemes.Light => new Color(1f, 1f, 1f, 0.72f),
            // Pastel hues, semi-transparent.
            ModSetting.TrainDisplayColorThemes.Blue => new Color(0.72f, 0.84f, 0.96f, 0.88f),
            ModSetting.TrainDisplayColorThemes.Green => new Color(0.74f, 0.92f, 0.78f, 0.88f),
            ModSetting.TrainDisplayColorThemes.Amber => new Color(0.98f, 0.92f, 0.7f, 0.88f), // yellow
            ModSetting.TrainDisplayColorThemes.Simple => new Color(0.96f, 0.76f, 0.76f, 0.88f), // red
            _ => new Color(1f, 1f, 1f, 0.98f),
        };

        internal static Color PanelInkColor => ModSetting.Instance.TrainDisplayColorTheme switch
        {
            ModSetting.TrainDisplayColorThemes.Dark => new Color(0.95f, 0.95f, 0.97f, 1f),
            ModSetting.TrainDisplayColorThemes.BlackSemi => new Color(0.95f, 0.95f, 0.97f, 1f),
            _ => new Color(0.06f, 0.06f, 0.08f, 1f),
        };

        internal static bool ShowExtrasStrip =>
            (ModSetting.Instance.TrainDisplayVisibleFields & ModSetting.TrainDisplayFields.ExtrasMask) != 0;

        internal static Color BackgroundColor => PanelFaceColor;
        internal static Color TextColor => PanelInkColor;
    }
}
