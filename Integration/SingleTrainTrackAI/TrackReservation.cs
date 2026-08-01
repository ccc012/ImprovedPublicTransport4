using System.Collections.Generic;
using ColossalFramework;

namespace SingleTrainTrackAI
{
    /// <summary>
    /// One reservation slot per single-track segment: the first train to claim it holds it until it
    /// moves off, blocking any other train from entering from the opposite direction in the meantime.
    /// </summary>
    internal static class TrackReservation
    {
        private class Reservation
        {
            public ushort VehicleId;
            public uint LastRenewedFrame;
        }

        // Safety net, not the normal release path (see Patch_TrainAI_CalculateTargetSpeed, which
        // releases immediately once a train's current segment changes away from the one it holds).
        // This only matters if a train is removed from the simulation without ever being seen to
        // leave its held segment (e.g. despawned mid-segment) - without it, that segment would stay
        // reserved forever and permanently deadlock anything waiting to use it from the other end.
        private const uint StaleAfterFrames = 600;

        private static readonly Dictionary<ushort, Reservation> _reservations = new Dictionary<ushort, Reservation>();
        private static readonly Dictionary<ushort, ushort> _vehicleHeldSegment = new Dictionary<ushort, ushort>();

        internal static bool IsHeldByOther(ushort segmentId, ushort vehicleId)
        {
            if (!_reservations.TryGetValue(segmentId, out var existing))
            {
                return false;
            }

            var frame = CurrentFrame;
            // Must expire here too - only Occupy() used to check staleness, so a despawned
            // holder left the segment "held forever" and every following train braked to 0
            // before the single track with no one actually on it.
            if (frame - existing.LastRenewedFrame > StaleAfterFrames)
            {
                _reservations.Remove(segmentId);
                if (_vehicleHeldSegment.TryGetValue(existing.VehicleId, out var held) && held == segmentId)
                {
                    _vehicleHeldSegment.Remove(existing.VehicleId);
                }

                return false;
            }

            if (existing.VehicleId == vehicleId)
            {
                return false;
            }

            // Trailers share the lead vehicle's reservation - do not block a car of the same train.
            var vehicles = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;
            var leadSelf = vehicles[vehicleId].GetFirstVehicle(vehicleId);
            var leadOther = vehicles[existing.VehicleId].GetFirstVehicle(existing.VehicleId);
            if (leadSelf != 0 && leadSelf == leadOther)
            {
                return false;
            }

            // Holder no longer exists / not spawned - free the slot.
            var otherFlags = vehicles[existing.VehicleId].m_flags;
            if ((otherFlags & Vehicle.Flags.Created) == 0 || (otherFlags & Vehicle.Flags.Spawned) == 0)
            {
                ReleaseInternal(segmentId, existing.VehicleId);
                return false;
            }

            return true;
        }

        /// <summary>Claims or renews the calling vehicle's hold on <paramref name="segmentId"/>, and
        /// releases whatever single-track segment it held previously if that has changed.</summary>
        internal static void Occupy(ushort segmentId, ushort vehicleId, uint currentFrame)
        {
            if (_vehicleHeldSegment.TryGetValue(vehicleId, out var previouslyHeld) && previouslyHeld != segmentId)
            {
                ReleaseInternal(previouslyHeld, vehicleId);
            }

            if (_reservations.TryGetValue(segmentId, out var existing))
            {
                if (existing.VehicleId == vehicleId || currentFrame - existing.LastRenewedFrame > StaleAfterFrames)
                {
                    existing.VehicleId = vehicleId;
                    existing.LastRenewedFrame = currentFrame;
                }
                // else: held by someone else and not stale - leave it as-is. The caller
                // (Patch_TrainAI_CalculateTargetSpeed) never calls Occupy for a segment a vehicle
                // isn't already physically standing on, so this branch is only reachable if two
                // trains somehow entered the same single-track segment at once (a collision the
                // speed-hold below is meant to prevent in the first place) - not overwriting the
                // existing holder here is the safer of the two options.
            }
            else
            {
                _reservations[segmentId] = new Reservation { VehicleId = vehicleId, LastRenewedFrame = currentFrame };
            }

            _vehicleHeldSegment[vehicleId] = segmentId;
        }

        /// <summary>Releases whatever single-track segment the given vehicle currently holds, if any -
        /// for when a vehicle leaves the simulation (despawns, is deleted) outside of the normal
        /// segment-to-segment transition Occupy() already handles.</summary>
        internal static void ReleaseVehicle(ushort vehicleId)
        {
            if (_vehicleHeldSegment.TryGetValue(vehicleId, out var held))
            {
                ReleaseInternal(held, vehicleId);
            }
        }

        private static void ReleaseInternal(ushort segmentId, ushort vehicleId)
        {
            if (_reservations.TryGetValue(segmentId, out var existing) && existing.VehicleId == vehicleId)
            {
                _reservations.Remove(segmentId);
            }

            _vehicleHeldSegment.Remove(vehicleId);
        }

        internal static void Clear()
        {
            _reservations.Clear();
            _vehicleHeldSegment.Clear();
        }

        internal static uint CurrentFrame => Singleton<SimulationManager>.instance.m_currentFrameIndex;
    }
}
