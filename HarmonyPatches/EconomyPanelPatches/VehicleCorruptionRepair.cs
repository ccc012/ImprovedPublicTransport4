using System;
using ColossalFramework;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.HarmonyPatches.EconomyPanelPatches
{
    // Same repair-on-load pattern as EconomyCorruptionRepair, for a different corruption shape:
    // a save whose serialized data references a vehicle prefab whose name is itself corrupted
    // (invalid UTF-16 - confirmed via a real save's output_log.txt, where Loading Screen Mod
    // Revisited's own missing-asset reporting throws "invalid utf-16 sequence (missing surrogate
    // head)" trying to even print the name). Vanilla deserialization leaves that Vehicle slot
    // "Created" with Info never resolved (null) instead of failing loudly, and every downstream
    // system that walks live vehicles (our own code, other mods' full-city scans, and apparently
    // whatever internal step finally flips SimulationPaused off) can hang or NullReferenceException
    // the moment it reaches that slot - matching a report of "loads but never unpauses, can't
    // click any building" plus a third-party mod (Demographics, Workshop 2074258904) crashing with
    // exactly a NullReferenceException while scanning citizens/vehicles at load time.
    internal static class VehicleCorruptionRepair
    {
        public static void RepairMissingVehicleInfo()
        {
            try
            {
                var vehicleManager = Singleton<VehicleManager>.instance;
                if (vehicleManager == null)
                {
                    return;
                }

                var buffer = vehicleManager.m_vehicles.m_buffer;
                if (buffer == null)
                {
                    return;
                }

                var repaired = 0;
                for (var i = 1; i < buffer.Length; i++)
                {
                    ref var vehicle = ref buffer[i];
                    if ((vehicle.m_flags & Vehicle.Flags.Created) == 0)
                    {
                        continue;
                    }

                    if (vehicle.Info != null)
                    {
                        continue;
                    }

                    // Created but with no resolvable prefab - this is the corrupted slot. Release it
                    // through the normal vehicle-removal path (frees any trailer chain, citizen
                    // units, and path units it holds) rather than just zeroing flags, so nothing
                    // else is left dangling for the game or another mod to trip over later.
                    try
                    {
                        vehicleManager.ReleaseVehicle((ushort)i);
                        repaired++;
                    }
                    catch (Exception releaseEx)
                    {
                        Utils.LogError(
                            $"VehicleCorruptionRepair: failed to release corrupted vehicle slot {i}: {releaseEx.Message}");
                    }
                }

                if (repaired > 0)
                {
                    Utils.LogWarning(
                        $"VehicleCorruptionRepair: released {repaired} vehicle slot(s) with a Created flag "
                        + "but no resolvable prefab (corrupted/missing vehicle asset in the save).");
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"VehicleCorruptionRepair: repair failed: {ex.Message}");
            }
        }
    }
}
