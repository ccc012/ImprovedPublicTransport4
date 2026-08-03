// <copyright file="TrackerPanelManager.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace FlightTracker
{
    using System.Collections.Generic;
    using AlgernonCommons;
    using AlgernonCommons.UI;
    using ColossalFramework;

    /// <summary>
    /// Static class to manage the flight tracker panel.
    /// </summary>
    internal static class TrackerPanelManager
    {
        /// <summary>
        /// Creates the panel object in-game and displays it.
        /// </summary>
        internal static void Create() => StandalonePanelManager<TrackerPanel>.Create();

        /// <summary>
        /// Closes the panel by destroying the object (removing any ongoing UI overhead).
        /// </summary>
        internal static void Close() => StandalonePanelManager<TrackerPanel>.Panel?.Close();

        /// <summary>
        /// Sets the target to the selected building, creating the panel if necessary.
        /// </summary>
        /// <param name="buildingID">New building ID.</param>
        internal static void SetTarget(ushort buildingID)
        {
            // If no existing panel, create it.
            if (!StandalonePanelManager<TrackerPanel>.Panel)
            {
                Create();
            }

            // Set the target.
            StandalonePanelManager<TrackerPanel>.Panel.SetTarget(buildingID);
        }

        /// <summary>
        /// Handles target building changes.
        /// </summary>
        internal static void TargetChanged()
        {
            ushort buildingID = WorldInfoPanel.GetCurrentInstanceID().Building;

            // Set target to this building if it's supported, or close if it's an unsupported building.
            if (IsSupportedBuilding(buildingID))
            {
                SetTarget(buildingID);
            }
            else
            {
                Close();
            }
        }

        /// <summary>
        /// Checks to see if the given building is supported by the mod.
        /// </summary>
        /// <param name="buildingID">Building ID of building to check.</param>
        /// <returns>A value indicating whether the given building is supported.</returns>
        private static bool IsSupportedBuilding(ushort buildingID)
        {
            if (buildingID == 0)
            {
                return false;
            }

            Logging.Message("checking building ", buildingID);

            var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];
            BuildingInfo buildingInfo = building.Info;
            if (buildingInfo == null)
            {
                Logging.Message("no building info");
                return false;
            }

            // Must be an airport-related building.
            if (buildingInfo.GetSubService() != ItemClass.SubService.PublicTransportPlane)
            {
                Logging.Message("not a plane subservice: ", buildingInfo.name);
                return false;
            }

            // Only support buildings whose airport complex actually owns vehicles. On large airports
            // (Airports DLC), the terminal building the player selects often doesn't own any aircraft
            // itself - the real owners are sibling stand/gate sub-buildings chained off the same main
            // (root) building via m_subBuilding. So check the whole complex, not just this building.
            foreach (ushort complexBuildingID in GetAirportComplexBuildings(buildingID))
            {
                if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[complexBuildingID].m_ownVehicles != 0)
                {
                    Logging.Message("building supported: ", buildingInfo.name);
                    return true;
                }
            }

            Logging.Message("plane subservice but no vehicles anywhere in complex: ", buildingInfo.name);
            return false;
        }

        /// <summary>
        /// Gets every building ID that belongs to the same airport complex as <paramref name="buildingID"/>:
        /// the root (main) building reached by following <see cref="Building.m_parentBuilding"/> up the chain,
        /// plus every sub-building reached by following <see cref="Building.m_subBuilding"/> down from the root.
        /// </summary>
        /// <param name="buildingID">Building ID to start from (can be the root or any sub-building).</param>
        /// <returns>List of building IDs in the complex, including the root itself.</returns>
        internal static List<ushort> GetAirportComplexBuildings(ushort buildingID)
        {
            Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
            var result = new List<ushort>();

            // Walk up to the root (main) building of the complex.
            ushort rootID = buildingID;
            int guard = 0;
            while (buildingBuffer[rootID].m_parentBuilding != 0)
            {
                rootID = buildingBuffer[rootID].m_parentBuilding;

                // A building chain cannot legitimately be longer than the building buffer itself;
                // this only trips on a corrupted m_parentBuilding cycle, and bails instead of hanging.
                if (++guard > 49152)
                {
                    result.Add(buildingID);
                    return result;
                }
            }

            result.Add(rootID);

            ushort subBuildingID = buildingBuffer[rootID].m_subBuilding;
            guard = 0;
            while (subBuildingID != 0)
            {
                result.Add(subBuildingID);
                subBuildingID = buildingBuffer[subBuildingID].m_subBuilding;

                if (++guard > 49152)
                {
                    break;
                }
            }

            return result;
        }
    }
}