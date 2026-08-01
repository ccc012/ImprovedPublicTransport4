// <copyright file="TrackerPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace FlightTracker
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using AlgernonCommons;
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// The flight tracker panel.
    /// </summary>
    internal class TrackerPanel : StandalonePanel
    {
        // Layout constants - private.
        private const float ListY = 40f;
        private const float ListHeight = 10f * FlightRow.FlightRowHeight;
        private const float ListWidth = 400f;
        private const float CalculatedPanelHeight = ListY + ListHeight + Margin;
        private const float CalculatedPanelWidth = 400f + Margin + Margin;

        // List of flights.
        private readonly List<FlightRowData> _tempList = new List<FlightRowData>();

        // Panel components.
        private UIList _flightList;

        // Selected target.
        private ushort _buildingID;
        private float _nextListRebuild;

        // (positioning follows the CityServiceWorldInfoPanel directly)

        /// <summary>
        /// Gets the panel width.
        /// </summary>
        public override float PanelWidth => CalculatedPanelWidth;

        /// <summary>
        /// Gets the panel height.
        /// </summary>
        public override float PanelHeight => CalculatedPanelHeight;

        /// <summary>
        /// Gets the panel's title.
        /// </summary>
        protected override string PanelTitle => ImprovedPublicTransport.Localization.Get("FLIGHT_TRACKER_NAME");

        /// <summary>
        /// Called by Unity when the object is created.
        /// Used to perform setup.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            try
            {
                // Flight list.
                _flightList = UIList.AddUIList<FlightRow>(
                    this,
                    Margin,
                    ListY,
                    ListWidth,
                    ListHeight,
                    FlightRow.FlightRowHeight);
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up flight tracker panel");
            }
        }

        /// <summary>
        /// Called by Unity every update.
        /// </summary>
        public override void Update()
        {
            base.Update();

            // If there's no current building selected in the world info panel, close the tracker.
            // This ensures Escape/Close actions are respected.
            ushort currentBuildingID = WorldInfoPanel.GetCurrentInstanceID().Building;
            if (currentBuildingID == 0)
            {
                TrackerPanelManager.Close();
                return;
            }

            // Attach to the City Service World Info Panel only (the airplane stand panel) and follow its absolute position
            // so the tracker stays attached while the camera moves/rotates.
            UIComponent infoPanel = UIView.library.Get(typeof(CityServiceWorldInfoPanel).Name) as UIComponent;

            // If the attached panel is gone/hidden (closed or Escape pressed), close the tracker to mimic expected behaviour.
            if (infoPanel == null || !infoPanel.isVisible || !infoPanel.isActiveAndEnabled || Input.GetKeyDown(KeyCode.Escape))
            {
                TrackerPanelManager.Close();
                return;
            }

            // Keep the tracker positioned to the left of the CityService info panel while it's visible.
            absolutePosition = new Vector2(infoPanel.absolutePosition.x - (PanelWidth + Margin), infoPanel.absolutePosition.y + 40f);
            isVisible = true;

            // Ensure tracker renders behind the info panel (z-order).
            if (infoPanel != null)
            {
                int targetZ = Math.Max(0, infoPanel.zOrder - 1);
                if (zOrder >= targetZ)
                {
                    zOrder = targetZ;
                }
            }

            // Rebuild flight list at most a few times per second (was every frame).
            if (Time.unscaledTime < _nextListRebuild)
            {
                return;
            }

            _nextListRebuild = Time.unscaledTime + 0.35f;

            // Local references.
            Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
            Vehicle[] vehicleBuffer = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;

            // Regenerate vehicle list.
            _tempList.Clear();
            ushort vehicleID = buildingBuffer[_buildingID].m_ownVehicles;
            int ownVehicleLoopGuard = 0;
            int maxOwnVehicles = (int)Singleton<VehicleManager>.instance.m_vehicles.m_size;
            while (vehicleID != 0)
            {
                if (++ownVehicleLoopGuard > maxOwnVehicles)
                {
                    Logging.Error("Invalid own-vehicle list detected for building ", _buildingID);
                    break;
                }
                // Local reference.
                ref Vehicle thisVehicle = ref vehicleBuffer[vehicleID];

                // Only interested in passenger aircraft.
                VehicleInfo vehicleInfo = thisVehicle.Info;
                if (vehicleInfo == null || vehicleInfo.m_class.m_subService != ItemClass.SubService.PublicTransportPlane)
                {
                    // Make sure that the next vehicle ID is assigned before continuing, otherwise there'll be an infinite loop.
                    vehicleID = thisVehicle.m_nextOwnVehicle;
                    continue;
                }

                // Determine flight status for this vehicle.
                FlightRowData.FlightStatus flightStatus = FlightRowData.FlightStatus.Incoming;
                // m_targetBuilding is a building ID (outside connection / gate), not a NetNode index.
                // Indexing nodeBuffer with it was reading unrelated node data and mis-classifying flights.
                ushort vehicleTarget = thisVehicle.m_targetBuilding;
                if (vehicleTarget != 0)
                {
                    Vector3 targetPos = buildingBuffer[vehicleTarget].m_position;
                    if (targetPos.x < -8500 || targetPos.x > 8500 || targetPos.z < -8500 || targetPos.z > 8500)
                    {
                        // Check to see if it's still at the gate.
                        if ((vehicleBuffer[vehicleID].m_flags & Vehicle.Flags.Stopped) != 0)
                        {
                            // At gate.
                            flightStatus = FlightRowData.FlightStatus.AtGate;
                        }
                        else
                        {
                            // It's left the gate.
                            flightStatus = FlightRowData.FlightStatus.Departed;
                        }
                    }
                }

                // Check for 'landed' status for arriving flights.
                if (flightStatus == FlightRowData.FlightStatus.Incoming && (thisVehicle.m_flags & Vehicle.Flags.Flying) == 0)
                {
                    // Exclude vehicles landed near map edge from being recorded as 'landed'.
                    Vector3 vehiclePos = thisVehicle.GetLastFramePosition();
                    if (vehiclePos.x > -8500 && vehiclePos.x < 8500 && vehiclePos.z > -8500 && vehiclePos.z < 8500)
                    {
                        flightStatus = FlightRowData.FlightStatus.Landed;
                    }
                }

                // Add this row to the list.
                _tempList.Add(new FlightRowData(vehicleID, vehicleInfo, flightStatus));

                // Next vehicle.
                vehicleID = thisVehicle.m_nextOwnVehicle;
            }

            // Set display list items, without changing the display.
            _flightList.Data = new FastList<object>
            {
                m_buffer = _tempList.ToArray(),
                m_size = _tempList.Count,
            };
        }

        /// <summary>
        /// Applies the panel's default position.
        /// </summary>
        public override void ApplyDefaultPosition()
        {
            // Set the flag to check/adjust the position at the next update.
            isVisible = false;
            // Positioning is handled in Update() when the info panel is visible.
        }

        /// <summary>
        /// Sets/changes the currently selected building.
        /// </summary>
        /// <param name="buildingID">New building ID.</param>
        internal virtual void SetTarget(ushort buildingID)
        {
            Logging.Message("target set to building ", buildingID);
            _buildingID = buildingID;
            // Positioning will be adjusted in Update() when the info panel is visible.
        }
    }
}
