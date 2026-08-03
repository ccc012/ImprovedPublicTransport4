// <copyright file="TrainDisplayWatcher.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the GPL-3.0 license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace ImprovedPublicTransport.Integration.TrainDisplayUpdated
{
    using ImprovedPublicTransport.Util;
    using UnityEngine;

    internal sealed class TrainDisplayWatcher : MonoBehaviour
    {
        private TrainDisplayIntegration.OverlayData _overlayData;
        private float _nextPollTime;
        private float _trackedSince;
        private ushort _trackedVehicle;

        private void Update()
        {
            if (TrainDisplayRuntimeConfig.Mode != ModSetting.TrainDisplayModes.Enabled)
            {
                ClearOverlay();
                return;
            }

            // Stable throttle only — the 4.8 "snappier" path (immediate re-poll on vehicle change +
            // sub-0.1s floors on Maximum) caused hitching/freezes for some players. Keep a hard
            // 0.1s floor and honour the Options update-interval slider above that.
            if (Time.realtimeSinceStartup < _nextPollTime)
            {
                return;
            }

            // Profile is a multiplier on the slider value, not a floor - a floor stopped mattering
            // the moment the slider's own value exceeded it, which was true at the slider's default
            // against both Normal and Maximum, so switching profiles had no visible effect. The
            // 0.1s floor below is the actual hitching guard and always applies regardless of profile.
            var interval = TrainDisplayIntegration.GetUpdateInterval() * PerformanceProfile.TrainDisplayPollMultiplier;
            interval = Mathf.Max(0.1f, interval);
            _nextPollTime = Time.realtimeSinceStartup + interval;

            if (!TrainDisplayIntegration.TryGetSelectedVehicle(out ushort vehicleId))
            {
                ClearOverlay();
                return;
            }

            if (_trackedVehicle != vehicleId)
            {
                _trackedVehicle = vehicleId;
                _trackedSince = Time.realtimeSinceStartup;
            }

            if (!TrainDisplayIntegration.TryBuildOverlayData(vehicleId, Time.realtimeSinceStartup - _trackedSince, out _overlayData))
            {
                ClearOverlay();
            }
        }

        private void OnGUI()
        {
            if (!_overlayData.HasContent)
            {
                return;
            }

            TrainDisplayIntegration.DrawOverlay(_overlayData);
        }

        private void ClearOverlay()
        {
            _overlayData = default(TrainDisplayIntegration.OverlayData);
            _trackedVehicle = 0;
            _trackedSince = 0f;
        }
    }
}
