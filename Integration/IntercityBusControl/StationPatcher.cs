using System;
using System.Collections.Generic;
using CF = ColossalFramework;
using ImprovedPublicTransport;
using ImprovedPublicTransport.Util;

namespace IntercityBusControl
{
    public static class StationPatcher
    {
        public static readonly HashSet<string> PatchedBuildingNames = new HashSet<string>();

        /// <summary>
        /// Prefab names that default the accept-intercity checkbox to OFF (simple/converted
        /// stations). Native Intercity Bus Stations are not in this set (default ON).
        /// </summary>
        public static readonly HashSet<string> PrefabsDefaultReject = new HashSet<string>();

        private static bool _netInfoNotFoundLogged;
        private static readonly Dictionary<BuildingInfo, OriginalPrefabState> OriginalPrefabStates =
            new Dictionary<BuildingInfo, OriginalPrefabState>();

        private sealed class OriginalPrefabState
        {
            public ItemClass Class;
            public TransportStationAI AI;
            public NetInfo TransportLineInfo;
            public TransportInfo TransportInfo;
            public TransportInfo SecondaryTransportInfo;
        }

        internal static void RecordOriginalState(BuildingInfo info, TransportStationAI ai)
        {
            if (info == null || ai == null || OriginalPrefabStates.ContainsKey(info))
            {
                return;
            }

            OriginalPrefabStates.Add(info, new OriginalPrefabState
            {
                Class = info.m_class,
                AI = ai,
                TransportLineInfo = ai.m_transportLineInfo,
                TransportInfo = ai.m_transportInfo,
                SecondaryTransportInfo = ai.m_secondaryTransportInfo
            });
        }

        internal static void RestorePrefabs()
        {
            try
            {
                foreach (var pair in OriginalPrefabStates)
                {
                    if (pair.Key == null || pair.Value.AI == null)
                        continue;
                    pair.Key.m_class = pair.Value.Class;
                    pair.Value.AI.m_transportLineInfo = pair.Value.TransportLineInfo;
                    pair.Value.AI.m_transportInfo = pair.Value.TransportInfo;
                    pair.Value.AI.m_secondaryTransportInfo = pair.Value.SecondaryTransportInfo;
                }
            }
            finally
            {
                OriginalPrefabStates.Clear();
                Reset();
            }
        }

        public static void Reset()
        {
            PatchedBuildingNames.Clear();
            PrefabsDefaultReject.Clear();
            _netInfoNotFoundLogged = false;
        }

        internal static NetInfo TryGetIntercityBusLine(bool logIfMissing = false)
        {
            var intercityBusLine = PrefabCollection<NetInfo>.FindLoaded(Mod.IntercityBusLine);
            if (intercityBusLine != null)
            {
                return intercityBusLine;
            }

            if (logIfMissing && !_netInfoNotFoundLogged)
            {
                _netInfoNotFoundLogged = true;
                Utils.LogWarning(
                    "Intercity Bus Control - '" + Mod.IntercityBusLine +
                    "' NetInfo not found; station patching will retry when NetInfos are loaded.");
            }

            return null;
        }

        public static void PatchStations()
        {
            try
            {
                var intercityBusLine = TryGetIntercityBusLine(logIfMissing: true);
                if (intercityBusLine == null)
                {
                    return;
                }

                var classDict = Mod.GetItemClassDict();
                if (classDict == null || !classDict.ContainsKey("Intercity Bus"))
                {
                    Utils.LogWarning(
                        "Intercity Bus Control - 'Intercity Bus' item class not found; Sunset Harbor DLC may not be active.");
                    return;
                }

                var intercityBusClass = classDict["Intercity Bus"];
                var intercityBusTransport = PrefabCollection<TransportInfo>.FindLoaded("Intercity Bus");

                int patched = 0;
                int registered = 0;
                var patchedPrefabs = new List<BuildingInfo>();
                uint count = (uint)PrefabCollection<BuildingInfo>.LoadedCount();
                for (uint i = 0; i < count; i++)
                {
                    var info = PrefabCollection<BuildingInfo>.GetLoaded(i);
                    if (info?.m_buildingAI is not TransportStationAI ai)
                    {
                        continue;
                    }

                    // Always register native SH intercity stations for the UI toggle (IPT3 wrongly
                    // skipped them so PatchedBuildingNames never contained "Intercity Bus Station *").
                    if (IsNativeIntercityBusStation(info, ai))
                    {
                        if (PatchedBuildingNames.Add(info.name))
                        {
                            registered++;
                        }

                        // Do NOT force m_maxVehicleCount on natives: TransportStationAI is a DepotAI
                        // subclass, so writing capacity makes the City Service panel show
                        // "ônibus em uso X/Y" like a bus garage. Leave prefab spawn caps alone.
                        continue;
                    }

                    if (TryPatchStation(info, ai, intercityBusLine, intercityBusClass, intercityBusTransport))
                    {
                        patched++;
                        patchedPrefabs.Add(info);
                    }
                }

                // After patching prefabs, call CreateConnectionLines on all placed instances
                // of the converted prefabs so they establish valid outside connection links.
                // This fixes the bug where converted stations spawn a bus that immediately despawns.
                int relinked = 0;
                var buildingManager = CF.Singleton<BuildingManager>.instance;
                foreach (var prefab in patchedPrefabs)
                {
                    for (ushort buildingId = 1; buildingId < buildingManager.m_buildings.m_buffer.Length; buildingId++)
                    {
                        ref var building = ref buildingManager.m_buildings.m_buffer[buildingId];
                        if (building.Info == prefab && building.m_flags != Building.Flags.None)
                        {
                            try
                            {
                                UnlimitedOutsideConnections.Patch_TransportStationAI_CreateConnectionLines.CreateConnectionLines(
                                    (TransportStationAI)prefab.m_buildingAI, buildingId, ref building);
                                relinked++;
                            }
                            catch (Exception e)
                            {
                                Utils.LogWarning($"Intercity Bus Control - CreateConnectionLines failed for {prefab.name} (id={buildingId}): {e.Message}");
                            }
                        }
                    }
                }

                // Always log summary (not verbose-only) so playtests can confirm coverage.
                Utils.Log(
                    $"Intercity Bus Control - PatchStations complete: {patched} converted, " +
                    $"{registered} native registered, {relinked} instances relinked, " +
                    $"{PatchedBuildingNames.Count} total for UI toggle: " +
                    $"[{string.Join(", ", new List<string>(PatchedBuildingNames).ToArray())}].");
            }
            catch (Exception e)
            {
                Utils.LogError($"Intercity Bus Control - PatchStations error: {e.Message}");
            }
        }

        /// <summary>
        /// Sunset Harbor native intermunicipal terminals only (default accept ON).
        /// Converted simple stations must NOT match: after convert they also get Intercity Bus
        /// Line/transport, so we only trust the English prefab name (or PrefabsDefaultReject).
        /// </summary>
        internal static bool IsNativeIntercityBusStation(BuildingInfo info, TransportStationAI ai)
        {
            if (info == null)
            {
                return false;
            }

            var name = info.name ?? string.Empty;
            if (PrefabsDefaultReject.Contains(name))
            {
                return false;
            }

            // Official Sunset Harbor asset ids only.
            return name.IndexOf("Intercity Bus Station", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("IntercityBusStation", StringComparison.OrdinalIgnoreCase) >= 0;
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

            bool isBusPrimary = IsBusSubService(ti1);
            bool isBusSecondary = IsBusSubService(ti2);

            // Exactly one bus slot (original RegionalBuses / IPT3 XOR).
            if (!(isBusPrimary ^ isBusSecondary))
            {
                return false;
            }

            bool alreadyHasIntercityLine = ai.m_transportLineInfo != null
                && string.Equals(ai.m_transportLineInfo.name, Mod.IntercityBusLine, StringComparison.Ordinal);

            int curMax = isBusPrimary ? ai.m_maxVehicleCount : ai.m_maxVehicleCount2;
            var desiredCap = GetCapacityForCurrentMode();

            if (alreadyHasIntercityLine)
            {
                // Do not force capacity on stations (shows "buses in use X/Y" like a depot).
                PatchedBuildingNames.Add(info.name);
                if (!IsNativeIntercityBusStation(info, ai))
                {
                    PrefabsDefaultReject.Add(info.name);
                }

                return false;
            }

            // Do not steal a station that already has a different line type (city bus line etc.).
            if (isBusPrimary && ai.m_transportLineInfo != null)
            {
                return false;
            }

            RecordOriginalState(info, ai);
            ai.m_transportLineInfo = intercityBusLine;

            if (isBusPrimary)
            {
                info.m_class = intercityBusClass;
                if (intercityBusTransport != null)
                {
                    ai.m_transportInfo = intercityBusTransport;
                }
                // Keep prefab m_maxVehicleCount (do not write 40/100000) so the City Service
                // panel does not gain a fake "buses in use" depot line on a station.
            }
            else
            {
                if (intercityBusTransport != null)
                {
                    ai.m_secondaryTransportInfo = intercityBusTransport;
                }
            }

            PatchedBuildingNames.Add(info.name);
            // Simple/converted stations: checkbox visible but default OFF (player must opt in).
            PrefabsDefaultReject.Add(info.name);
            Utils.Log($"Intercity Bus Control - StationPatcher: converted '{info.name}' to intercity bus terminal (default reject).");
            return true;
        }

        internal static int GetCapacityForCurrentMode()
        {
            switch (ModSetting.Instance.IntercityTerminalCapacityMode)
            {
                case ModSetting.DepotCapacityModes.Realistic:
                    return 40;
                case ModSetting.DepotCapacityModes.Intermediate:
                    return 200;
                default:
                    return 100000;
            }
        }

        private static bool IsBusSubService(TransportInfo ti) =>
            ti?.m_class != null
            && ti.m_class.m_subService == ItemClass.SubService.PublicTransportBus;
    }
}
