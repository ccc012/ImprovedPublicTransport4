using System;
using System.Collections.Generic;
using UnityEngine;
using ImprovedPublicTransport;
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

                var classDict = Mod.GetItemClassDict();
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
                    Utils.Log($"Intercity Bus Control - PatchStations complete: {patched} newly patched this sweep, {PatchedBuildingNames.Count} total eligible station(s): [{string.Join(", ", new List<string>(PatchedBuildingNames).ToArray())}].");
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
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log($"Intercity Bus Control - skipped {info.name}: not exactly one of primary/secondary transport is Bus (primary={isBusPrimary}, secondary={isBusSecondary}).");
                }
                return false;
            }

            bool alreadyHasIntercityLine = ai.m_transportLineInfo?.name == Mod.IntercityBusLine;
            int curMax = isBusPrimary ? ai.m_maxVehicleCount : ai.m_maxVehicleCount2;
            // Bails only if the cap already matches what the CURRENT mode wants - comparing against
            // "> 0" instead let a terminal patched under an earlier mode (e.g. still carrying the old
            // effectively-unlimited 100,000) survive forever: this guard would see a positive number,
            // consider the station "already patched" and never re-apply ApplyCapacity again, so
            // switching modes in Options had no visible effect on any terminal patched before the
            // switch.
            if (alreadyHasIntercityLine && curMax == GetCapacityForCurrentMode())
            {
                // Already patched (by an earlier call this same session, e.g. InitializePrefabPatch
                // catching a late-loaded asset before this retroactive sweep ran) - still register it
                // so the UI toggle keeps showing. Without this, a station that was already correctly
                // patched would silently lose its entry in PatchedBuildingNames on the next sweep,
                // hiding the toggle for a station that is, in fact, fully functional.
                PatchedBuildingNames.Add(info.name);
                return false;
            }

            if (isBusPrimary && ai.m_transportLineInfo != null && !alreadyHasIntercityLine)
            {
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log($"Intercity Bus Control - skipped {info.name}: primary transport already has a non-intercity line assigned ({ai.m_transportLineInfo.name}).");
                }
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
                ApplyCapacity(ai, primary: true);
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
                ApplyCapacity(ai, primary: false);
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log($"Intercity Bus Control - StationPatcher: patched {info.name} (secondary bus)");
                }
            }

            PatchedBuildingNames.Add(info.name);
            return true;
        }

        /// <summary>
        /// Vehicle cap for a patched intercity bus terminal, per
        /// <see cref="ModSetting.IntercityTerminalCapacityMode"/>. Shared with
        /// <see cref="HarmonyPatches.BuildingInfoPatches.InitializePrefabPatch"/>, which applies the
        /// same patch to prefabs that finish loading after level load (custom assets, editor) - both
        /// call sites must agree, or a terminal's cap would depend on which code path happened to
        /// patch it first.
        /// </summary>
        /// <remarks>
        /// A single intercity bus terminal typically serves several intercity lines at once (unlike
        /// a normal depot, which usually serves one), so even "Realistic" leaves headroom rather
        /// than adopting whatever a plain bus depot's own default happens to be - it just drops the
        /// effectively-unlimited 100,000 down to something a player could plausibly hit and notice.
        /// </remarks>
        internal static int GetCapacityForCurrentMode()
        {
            switch (ModSetting.Instance.IntercityTerminalCapacityMode)
            {
                case ModSetting.DepotCapacityModes.Realistic:
                    return 40;
                case ModSetting.DepotCapacityModes.Intermediate:
                    return 200;
                default: // Disabled - preserves the behaviour every existing save already has.
                    return 100000;
            }
        }

        private static void ApplyCapacity(TransportStationAI ai, bool primary)
        {
            var cap = GetCapacityForCurrentMode();
            if (primary)
            {
                ai.m_maxVehicleCount = cap;
            }
            else
            {
                ai.m_maxVehicleCount2 = cap;
            }
        }

        private static bool IsBusSubService(TransportInfo ti, ItemClass.SubService subService)
        {
            return ti != null && ti.m_class != null && ti.m_class.m_subService == subService;
        }
    }
}