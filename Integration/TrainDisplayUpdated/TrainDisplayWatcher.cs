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

            var interval = Mathf.Max(0.1f, TrainDisplayIntegration.GetUpdateInterval());
            // Performance profile still softens Light installs without racing Max.
            interval = Mathf.Max(interval, PerformanceProfile.TrainDisplayMinPollSeconds);
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
