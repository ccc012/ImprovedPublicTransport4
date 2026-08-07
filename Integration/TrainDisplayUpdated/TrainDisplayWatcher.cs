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

            if (Time.realtimeSinceStartup < _nextPollTime)
            {
                return;
            }

            _nextPollTime = Time.realtimeSinceStartup + PerformanceProfile.TrainDisplayRefreshSeconds;

            var scope = ModSetting.Instance.TrainDisplayScope;
            ushort vehicleId = 0;
            var found = scope != ModSetting.TrainDisplayScopes.SelectedVehicle
                && FpsCameraIntegration.TryGetVehicle(out vehicleId);
            if (!found && scope != ModSetting.TrainDisplayScopes.FirstPerson)
            {
                found = TrainDisplayIntegration.TryGetSelectedVehicle(out vehicleId);
            }

            if (!found)
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

        private void OnDestroy()
        {
            ClearOverlay();
            FpsCameraIntegration.Clear();
        }
    }
}
