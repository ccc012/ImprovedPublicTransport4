// Original implementation - see the note in TaxiStandRegistry.cs about why this is not a port.
using System;
using System.Reflection;
using ColossalFramework;
using HarmonyLib;
using ImprovedPublicTransport;
using JetBrains.Annotations;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace TaxiStandFix
{
    /// <summary>
    /// Sends a genuinely idle taxi (no passenger, no destination already assigned) toward the
    /// nearest taxi stand instead of leaving it to wander at random. This is deliberately additive:
    /// it only ever calls the vanilla, public <see cref="TaxiAI.SetTarget"/> - the same method the
    /// game itself uses to dispatch a taxi - so pathfinding, building registration and the taxi's
    /// own "I am available" offer broadcast (which keeps running once it arrives, per vanilla logic
    /// in TaxiAI.SimulationStep) all behave exactly as they would for a normal dispatch. Nothing
    /// here touches transfer-offer matching, pathfinding internals, or shared prefab data.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_TaxiAI_SimulationStep
    {
        // A coarser stagger than vanilla's own ~16-frame offer-broadcast cycle: this only decides
        // "should this idle taxi start heading toward a stand", not "broadcast availability", so it
        // does not need to run every tick, and running it less often avoids fighting the vanilla
        // broadcast for CPU time on a hot per-vehicle method.
        private const int StaggerMask = 0xFF;
        private const int StaggerShift = 8;

        private const float MaxStandSearchDistance = 2000f;

        [UsedImplicitly]
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("TaxiAI:SimulationStep", new[]
            {
                typeof(ushort),
                typeof(Vehicle).MakeByRefType(),
                typeof(Vehicle.Frame).MakeByRefType(),
                typeof(ushort),
                typeof(Vehicle).MakeByRefType(),
                typeof(int)
            });
        }

        [HarmonyPostfix]
        public static void Postfix(TaxiAI __instance, ushort vehicleID, ref Vehicle vehicleData)
        {
            try
            {
                if (!ModSetting.Instance.EnableTaxiStandFix)
                {
                    return;
                }

                if (!IsIdleAndUnassigned(ref vehicleData))
                {
                    return;
                }

                var frameIndex = Singleton<SimulationManager>.instance.m_currentFrameIndex;
                if (((frameIndex >> StaggerShift) & StaggerMask) != (vehicleID & StaggerMask))
                {
                    return;
                }

                var position = vehicleData.GetLastFramePosition();
                var standId = TaxiStandRegistry.FindNearest(position, MaxStandSearchDistance);
                if (standId == 0)
                {
                    return;
                }

                __instance.SetTarget(vehicleID, ref vehicleData, standId);
            }
            catch (Exception ex)
            {
                Utils.LogError($"TaxiStandFix: failed in SimulationStep postfix: {ex.Message}");
            }
        }

        private static bool IsIdleAndUnassigned(ref Vehicle vehicleData)
        {
            if (vehicleData.m_targetBuilding != 0)
            {
                // Already has somewhere to be - a passenger pickup, a prior stand assignment
                // still in progress, or heading back to its depot. Leave it alone.
                return false;
            }

            const Vehicle.Flags disallowed = Vehicle.Flags.GoingBack
                | Vehicle.Flags.Transition
                | Vehicle.Flags.Congestion
                | Vehicle.Flags.Parking
                | Vehicle.Flags.Emergency2;

            if ((vehicleData.m_flags & disallowed) != 0)
            {
                return false;
            }

            return (vehicleData.m_flags & Vehicle.Flags.Created) != 0
                   && (vehicleData.m_flags & Vehicle.Flags.Spawned) != 0;
        }
    }
}
