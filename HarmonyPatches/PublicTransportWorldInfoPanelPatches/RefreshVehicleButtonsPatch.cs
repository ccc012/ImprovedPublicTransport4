using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.UI;
using ImprovedPublicTransport.Data;
using ImprovedPublicTransport.Util;
using UnityEngine;

namespace ImprovedPublicTransport.HarmonyPatches.PublicTransportWorldInfoPanelPatches
{
    public static class RefreshVehicleButtonsPatch
    {
        // GetDescription() only depends on the vehicle's prefab, never the individual vehicle - cache
        // it per-prefab instead of re-resolving VehiclePrefabs.instance.FindByName(...) for every
        // vehicle button on every single frame this panel stays open.
        private static readonly Dictionary<VehicleInfo, string> DescriptionCache = new Dictionary<VehicleInfo, string>();


        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(PublicTransportWorldInfoPanel), "RefreshVehicleButtons"),
                new PatchUtil.MethodDefinition(typeof(RefreshVehicleButtonsPatch),
                    nameof(Prefix))
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(PublicTransportWorldInfoPanel), "RefreshVehicleButtons")
            );
        }
        
        private static string _vehicleTooltipFormat;
        private static float _nextTooltipRefresh;
        private static ushort _tooltipLineId;
        private const float TooltipRefreshInterval = 0.5f;

        public static bool Prefix(float ___kvehiclesX, UITemplateList<UIButton> ___m_vehicleButtons, ushort lineID)
        {
            if (___m_vehicleButtons?.items == null)
            {
                return false;
            }

            ___m_vehicleButtons.SetItemCount(Singleton<TransportManager>.instance.m_lines.m_buffer[(int)lineID].CountVehicles(lineID));
            VehicleManager instance = Singleton<VehicleManager>.instance;
            ushort num = Singleton<TransportManager>.instance.m_lines.m_buffer[(int)lineID].m_vehicles;
            int index = 0;
            float now = Time.unscaledTime;
            bool refreshTooltips = lineID != _tooltipLineId || now >= _nextTooltipRefresh;
            if (refreshTooltips)
            {
                _nextTooltipRefresh = now + TooltipRefreshInterval;
                _tooltipLineId = lineID;
                if (_vehicleTooltipFormat == null)
                {
                    _vehicleTooltipFormat = Localization.Get("VEHICLE_BUTTON_TOOLTIP");
                }
            }

            while ((int)num != 0)
            {
                if (index >= ___m_vehicleButtons.items.Count)
                {
                    break;
                }

                Vector3 relativePosition = ___m_vehicleButtons.items[index].relativePosition;
                relativePosition.x = ___kvehiclesX;
                ___m_vehicleButtons.items[index].relativePosition = relativePosition;
                ___m_vehicleButtons.items[index].objectUserData = (object)num;
                //begin mod(+): add tooltip with asset name (throttled — not every frame)
                if (refreshTooltips)
                {
                    var prefab = instance.m_vehicles.m_buffer[num].Info;
                    string description;
                    if (prefab == null)
                    {
                        description = instance.GetVehicleName(num);
                    }
                    else if (!DescriptionCache.TryGetValue(prefab, out description) || description == null)
                    {
                        description = VehiclePrefabs.instance?.FindByName(prefab.name)?.GetDescription()
                                      ?? prefab.name;
                        DescriptionCache[prefab] = description;
                    }
                    try
                    {
                        ___m_vehicleButtons.items[index].tooltip = string.Format(_vehicleTooltipFormat, description);
                    }
                    catch
                    {
                        // Non-fatal: tooltip format failure
                    }
                }
                //end mod
                num = instance.m_vehicles.m_buffer[(int)num].m_nextLineVehicle;
                if (++index >= CachedVehicleData.MaxVehicleCount)
                {
                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + System.Environment.StackTrace);
                    break;
                }
            }

            return false;
        }
    }
}