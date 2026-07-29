using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ImprovedPublicTransport.Util;

namespace IntercityBusControl
{
    public static class StationPatcher
    {
        public static readonly HashSet<string> PatchedBuildingNames = new HashSet<string>();

        public static void Reset()
        {
            PatchedBuildingNames.Clear();
        }

        public static void PatchStations()
        {
            try
            {
                var intercityBusLine = PrefabCollection<NetInfo>.FindLoaded(Mod.IntercityBusLine);
                if (intercityBusLine == null)
                {
                    Utils.LogWarning("Intercity Bus Control - '" + Mod.IntercityBusLine + "' NetInfo not found; skipping station patching.");
                    return;
                }

                var classField = typeof(ItemClassCollection).GetField("m_classDict", BindingFlags.Static | BindingFlags.NonPublic);
                var classDict = classField?.GetValue(null) as Dictionary<string, ItemClass>;
                if (classDict == null || !classDict.ContainsKey("Intercity Bus"))
                {
                    Utils.LogWarning("Intercity Bus Control - 'Intercity Bus' item class not found; Sunset Harbor DLC may not be active.");
                    return;
                }
                var intercityBusClass = classDict["Intercity Bus"];
                var intercityBusTransport = PrefabCollection<TransportInfo>.FindLoaded("Intercity Bus");

                int patched = 0;
                uint count = (uint)PrefabCollection<BuildingInfo>.LoadedCount();
                for (uint i = 0; i < count; i++)
                {
                    var info = PrefabCollection<BuildingInfo>.GetLoaded(i);
                    if (info?.m_buildingAI is TransportStationAI ai)
                    {
                        if (TryPatchStation(info, ai, intercityBusLine, intercityBusClass, intercityBusTransport))
                        {
                            patched++;
                        }
                    }
                }

                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log($"Intercity Bus Control - PatchStations complete: {patched} station(s) patched.");
                }
            }
            catch (Exception e)
            {
                Utils.LogError($"Intercity Bus Control - PatchStations error: {e.Message}");
            }
        }

        private static bool TryPatchStation(
            BuildingInfo info,
            TransportStationAI ai,
            NetInfo intercityBusLine,
            ItemClass intercityBusClass,
            TransportInfo intercityBusTransport)
        {
            var ti1 = ai.m_transportInfo;
            var ti2 = ai.m_secondaryTransportInfo;

            bool isBusPrimary = IsBusSubService(ti1, ItemClass.SubService.PublicTransportBus);
            bool isBusSecondary = IsBusSubService(ti2, ItemClass.SubService.PublicTransportBus);

            if (!(isBusPrimary ^ isBusSecondary))
            {
                return false;
            }

            bool alreadyHasIntercityLine = ai.m_transportLineInfo?.name == Mod.IntercityBusLine;
            int curMax = isBusPrimary ? ai.m_maxVehicleCount : ai.m_maxVehicleCount2;
            if (alreadyHasIntercityLine && curMax > 0)
            {
                return false;
            }

            if (isBusPrimary && ai.m_transportLineInfo != null && !alreadyHasIntercityLine)
            {
                return false;
            }

            ai.m_transportLineInfo = intercityBusLine;

            if (isBusPrimary)
            {
                info.m_class = intercityBusClass;
                if (intercityBusTransport != null)
                {
                    ai.m_transportInfo = intercityBusTransport;
                }
                ai.m_maxVehicleCount = 100000;
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log($"Intercity Bus Control - StationPatcher: patched {info.name} (primary bus)");
                }
            }
            else
            {
                if (intercityBusTransport != null)
                {
                    ai.m_secondaryTransportInfo = intercityBusTransport;
                }
                ai.m_maxVehicleCount2 = 100000;
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log($"Intercity Bus Control - StationPatcher: patched {info.name} (secondary bus)");
                }
            }

            PatchedBuildingNames.Add(info.name);
            return true;
        }

        private static bool IsBusSubService(TransportInfo ti, ItemClass.SubService subService)
        {
            return ti != null && ti.m_class != null && ti.m_class.m_subService == subService;
        }
    }
}