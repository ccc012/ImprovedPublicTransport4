using System;
using ColossalFramework;
using ImprovedPublicTransport.Util;
using UnityEngine;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.HarmonyPatches.DepotAIPatches
{
    /// <summary>
    /// Hard enforcement for realistic/intermediate depot caps:
    /// <list type="bullet">
    /// <item><see cref="DepotAI.CreateVehicle"/> prefix blocks new spawns when at budget-adjusted max.</item>
    /// <item>A light ~5s simulation sweep returns excess vehicles already sourced from capped depots
    /// (covers fleets that spawned before the cap was applied, or any path that skipped StartTransfer).</item>
    /// </list>
    /// Works with <see cref="StartTransferPatch"/> / <see cref="DepotUtil.CanAddVehicle"/>; does not
    /// replace them — CreateVehicle is the final choke point for every depot spawn path.
    /// </summary>
    public static class DepotCapacityEnforcePatch
    {
        private const int UncappedThreshold = 100000;
        /// <summary>How often to enforce caps when idle (no excess last pass).</summary>
        private const float EnforceIntervalIdleSeconds = 12f;
        /// <summary>Faster re-check after we actually returned vehicles.</summary>
        private const float EnforceIntervalBusySeconds = 6f;
        /// <summary>Full building-buffer rescan for capped depots (expensive).</summary>
        private const float DepotListRefreshSeconds = 45f;

        private static float _nextEnforceRealtime;
        private static float _nextDepotListRefreshRealtime;
        private static bool _actionQueued;
        private static ushort[] _cappedDepotIds = new ushort[0];
        private static int _cappedDepotCount;

        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(DepotAI), "CreateVehicle"),
                new PatchUtil.MethodDefinition(typeof(DepotCapacityEnforcePatch), nameof(CreateVehiclePre))
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(DepotAI), "CreateVehicle")
            );
            _cappedDepotCount = 0;
            _cappedDepotIds = new ushort[0];
        }

        /// <summary>
        /// Called from the main-thread enforcer component; schedules work on the simulation thread.
        /// </summary>
        public static void Tick()
        {
            if (!ImprovedPublicTransportMod.InGame)
            {
                return;
            }

            if (!Singleton<SimulationManager>.exists || !Singleton<BuildingManager>.exists)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (now < _nextEnforceRealtime || _actionQueued)
            {
                return;
            }

            // Placeholder; EnforceExcessVehicles sets the real next interval based on work done.
            _nextEnforceRealtime = now + EnforceIntervalIdleSeconds;
            _actionQueued = true;
            Singleton<SimulationManager>.instance.AddAction(() =>
            {
                try
                {
                    EnforceExcessVehicles();
                }
                catch (Exception ex)
                {
                    Utils.LogError($"DepotCapacityEnforce: {ex.Message}");
                }
                finally
                {
                    _actionQueued = false;
                }
            });
        }

        /// <summary>
        /// Final spawn gate: if this depot has a finite cap and is already full, skip CreateVehicle.
        /// Transport stations are left alone (IntercityBusControl owns those).
        /// </summary>
        private static bool CreateVehiclePre(
            DepotAI __instance,
            ushort buildingID,
            ref Building data,
            VehicleInfo vehicleInfo)
        {
            try
            {
                if (__instance == null || __instance is TransportStationAI)
                {
                    return true;
                }

                if (!TryResolveMaxForVehicleInfo(__instance, vehicleInfo, out int maxVehicles))
                {
                    // Unknown slot — fall back to primary if it is capped (still better than free spawn).
                    maxVehicles = __instance.m_maxVehicleCount;
                }

                if (maxVehicles <= 0 || maxVehicles >= UncappedThreshold)
                {
                    return true;
                }

                int allowed = DepotUtil.GetBudgetAdjustedMax(__instance, maxVehicles);
                if (allowed <= 0)
                {
                    return false;
                }

                if (__instance.GetVehicleCount(buildingID, ref data) >= allowed)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"DepotCapacityEnforce CreateVehiclePre: {ex.Message}");
            }

            return true;
        }

        private static bool TryResolveMaxForVehicleInfo(
            DepotAI depotAi,
            VehicleInfo vehicleInfo,
            out int maxVehicles)
        {
            maxVehicles = 0;
            if (depotAi == null || vehicleInfo == null)
            {
                return false;
            }

            var vType = vehicleInfo.m_vehicleType;

            if (depotAi.m_transportInfo != null
                && VehicleTypesCompatible(vType, depotAi.m_transportInfo))
            {
                maxVehicles = depotAi.m_maxVehicleCount;
                return true;
            }

            if (depotAi.m_secondaryTransportInfo != null
                && VehicleTypesCompatible(vType, depotAi.m_secondaryTransportInfo))
            {
                maxVehicles = depotAi.m_maxVehicleCount2;
                return true;
            }

            return false;
        }

        private static bool VehicleTypesCompatible(VehicleInfo.VehicleType vehicleType, TransportInfo slot)
        {
            if (slot == null)
            {
                return false;
            }

            if (vehicleType != VehicleInfo.VehicleType.None && vehicleType == slot.m_vehicleType)
            {
                return true;
            }

            if (vehicleType == VehicleInfo.VehicleType.Ferry && DepotUtil.IsFerryLike(slot))
            {
                return true;
            }

            if (vehicleType == VehicleInfo.VehicleType.Blimp && DepotUtil.IsBlimpLike(slot))
            {
                return true;
            }

            if (vehicleType == VehicleInfo.VehicleType.Helicopter && DepotUtil.IsHelicopterLike(slot))
            {
                return true;
            }

            if (vehicleType == VehicleInfo.VehicleType.Trolleybus
                && slot.m_transportType == TransportInfo.TransportType.Trolleybus)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Walk cached capped depots; if own-vehicle count exceeds budget max, detach excess
        /// vehicles from their lines so they return to the depot (SetTransportLine 0).
        /// Only touches vehicles on the depot's <c>m_ownVehicles</c> chain.
        /// </summary>
        internal static void EnforceExcessVehicles()
        {
            var buildingManager = Singleton<BuildingManager>.instance;
            var vehicleManager = Singleton<VehicleManager>.instance;
            if (buildingManager == null || vehicleManager == null)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (_cappedDepotCount == 0 || now >= _nextDepotListRefreshRealtime)
            {
                RefreshCappedDepotList(buildingManager);
                _nextDepotListRefreshRealtime = now + DepotListRefreshSeconds;
            }

            int depotsChecked = 0;
            int vehiclesReturned = 0;
            var buffer = buildingManager.m_buildings.m_buffer;
            int buildingSize = buffer.Length;

            for (int i = 0; i < _cappedDepotCount; i++)
            {
                ushort buildingID = _cappedDepotIds[i];
                if (buildingID == 0 || buildingID >= buildingSize)
                {
                    continue;
                }

                ref Building building = ref buffer[buildingID];
                if ((building.m_flags & Building.Flags.Created) == Building.Flags.None
                    || building.Info == null)
                {
                    continue;
                }

                var depotAi = building.Info.m_buildingAI as DepotAI;
                if (depotAi == null || depotAi is TransportStationAI)
                {
                    continue;
                }

                int maxPrimary = depotAi.m_maxVehicleCount;
                int maxSecondary = depotAi.m_maxVehicleCount2;
                int maxVehicles = PickFiniteMax(maxPrimary, maxSecondary);
                if (maxVehicles <= 0 || maxVehicles >= UncappedThreshold)
                {
                    continue;
                }

                int allowed = DepotUtil.GetBudgetAdjustedMax(depotAi, maxVehicles);
                if (allowed <= 0)
                {
                    continue;
                }

                int count = depotAi.GetVehicleCount(buildingID, ref building);
                depotsChecked++;
                if (count <= allowed)
                {
                    continue;
                }

                int excess = count - allowed;
                vehiclesReturned += ReturnExcessFromDepot(
                    buildingID, ref building, vehicleManager, excess);
            }

            // Adaptive cadence: idle cities rarely need a full enforce pass.
            _nextEnforceRealtime = Time.realtimeSinceStartup
                + (vehiclesReturned > 0 ? EnforceIntervalBusySeconds : EnforceIntervalIdleSeconds);

            // Only log when something actually changed — idle "checked N, returned 0" spam
            // was flooding Unity Debug.Log every few seconds under Verbose.
            if (vehiclesReturned > 0)
            {
                Utils.Log(
                    $"DepotCapacityEnforce: checked {depotsChecked} capped depot(s), " +
                    $"returned {vehiclesReturned} excess vehicle(s).");
            }
        }

        /// <summary>
        /// Full building buffer scan — only every <see cref="DepotListRefreshSeconds"/>.
        /// </summary>
        private static void RefreshCappedDepotList(BuildingManager buildingManager)
        {
            var buffer = buildingManager.m_buildings.m_buffer;
            int size = buffer.Length;
            int count = 0;

            // First pass: count
            for (ushort buildingID = 1; buildingID < size; buildingID++)
            {
                if (IsCappedDepot(ref buffer[buildingID]))
                {
                    count++;
                }
            }

            if (_cappedDepotIds == null || _cappedDepotIds.Length < count)
            {
                _cappedDepotIds = new ushort[Math.Max(count, 16)];
            }

            int write = 0;
            for (ushort buildingID = 1; buildingID < size && write < count; buildingID++)
            {
                if (IsCappedDepot(ref buffer[buildingID]))
                {
                    _cappedDepotIds[write++] = buildingID;
                }
            }

            _cappedDepotCount = write;
        }

        private static bool IsCappedDepot(ref Building building)
        {
            if ((building.m_flags & Building.Flags.Created) == Building.Flags.None
                || building.Info == null)
            {
                return false;
            }

            var depotAi = building.Info.m_buildingAI as DepotAI;
            if (depotAi == null || depotAi is TransportStationAI)
            {
                return false;
            }

            int maxVehicles = PickFiniteMax(depotAi.m_maxVehicleCount, depotAi.m_maxVehicleCount2);
            return maxVehicles > 0 && maxVehicles < UncappedThreshold;
        }

        private static int PickFiniteMax(int primary, int secondary)
        {
            bool primaryCapped = primary > 0 && primary < UncappedThreshold;
            bool secondaryCapped = secondary > 0 && secondary < UncappedThreshold;
            if (primaryCapped && secondaryCapped)
            {
                return Math.Min(primary, secondary);
            }

            if (primaryCapped)
            {
                return primary;
            }

            if (secondaryCapped)
            {
                return secondary;
            }

            return UncappedThreshold;
        }

        private static int ReturnExcessFromDepot(
            ushort depotID,
            ref Building depotBuilding,
            VehicleManager vehicleManager,
            int excess)
        {
            if (excess <= 0)
            {
                return 0;
            }

            int returned = 0;
            var vBuffer = vehicleManager.m_vehicles.m_buffer;
            int vSize = vBuffer.Length;

            // Walk this depot's own-vehicle chain only — O(fleet of depot), not O(all vehicles).
            ushort vehicleID = depotBuilding.m_ownVehicles;
            int guard = 0;
            while (vehicleID != 0 && returned < excess && guard++ < 4096)
            {
                if (vehicleID >= vSize)
                {
                    break;
                }

                ref Vehicle vehicle = ref vBuffer[vehicleID];
                ushort next = vehicle.m_nextOwnVehicle;

                // Skip trailers / multi-unit tails.
                if (vehicle.m_leadingVehicle == 0
                    && (vehicle.m_flags & Vehicle.Flags.GoingBack) == 0
                    && vehicle.m_transportLine != 0
                    && vehicle.m_sourceBuilding == depotID)
                {
                    var ai = vehicle.Info?.m_vehicleAI;
                    if (ai != null)
                    {
                        try
                        {
                            ai.SetTransportLine(vehicleID, ref vehicle, 0);
                            returned++;
                        }
                        catch (Exception ex)
                        {
                            Utils.LogError(
                                $"DepotCapacityEnforce: failed to return vehicle {vehicleID} " +
                                $"from depot {depotID}: {ex.Message}");
                        }
                    }
                }

                vehicleID = next;
            }

            return returned;
        }
    }

    /// <summary>
    /// Main-thread tick that schedules <see cref="DepotCapacityEnforcePatch"/> work every ~5s.
    /// </summary>
    public class DepotCapacityEnforcer : MonoBehaviour
    {
        private void Update()
        {
            DepotCapacityEnforcePatch.Tick();
        }
    }
}
