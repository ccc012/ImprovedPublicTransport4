using System.Collections.Generic;
using ColossalFramework;

namespace SingleTrainTrackAI
{
    /// <summary>
    /// One reservation slot per single-track SECTION (the whole contiguous stretch between two
    /// pass-through boundaries, see <see cref="SectionClassifier"/>) - the first train to claim it
    /// holds the entire section until it moves off, blocking any other train from entering from
    /// either end in the meantime. Reserving whole sections (instead of one segment at a time) is
    /// what prevents two opposing trains from each claiming a different segment of the same section
    /// and meeting head-on in the middle.
    /// </summary>
    internal static class TrackReservation
    {
        private class Reservation
        {
            public ushort VehicleId;
            public uint LastRenewedFrame;
        }

        // Safety net, not the normal release path (see Patch_TrainAI_CalculateTargetSpeed, which
        // releases immediately once a train's current segment moves out of the section it holds).
        // This only matters if a train is removed from the simulation without ever being seen to
        // leave its held section (e.g. despawned mid-section) - without it, that section would stay
        // reserved forever and permanently deadlock anything waiting to use it from the other end.
        private const uint StaleAfterFrames = 600;

        private static readonly Dictionary<SectionClassifier.Section, Reservation> _reservations =
            new Dictionary<SectionClassifier.Section, Reservation>();
        private static readonly Dictionary<ushort, SectionClassifier.Section> _vehicleHeldSection =
            new Dictionary<ushort, SectionClassifier.Section>();

        internal static bool IsHeldByOther(SectionClassifier.Section section, ushort vehicleId)
        {
            if (section == null || !_reservations.TryGetValue(section, out var existing))
            {
                return false;
            }

            var frame = CurrentFrame;
            // Must expire here too - only Occupy() used to check staleness, so a despawned
            // holder left the section "held forever" and every following train braked to 0
            // before the single track with no one actually on it.
            if (frame - existing.LastRenewedFrame > StaleAfterFrames)
            {
                _reservations.Remove(section);
                if (_vehicleHeldSection.TryGetValue(existing.VehicleId, out var held) && held == section)
                {
                    _vehicleHeldSection.Remove(existing.VehicleId);
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
                ReleaseInternal(section, existing.VehicleId);
                return false;
            }

            // Section is genuinely held by another live train - this is the "wait" side of the
            // priority queue: whichever train reached the section first (and is therefore already
            // its holder) keeps it, every other train arriving afterwards brakes here until the
            // holder releases it (see Occupy/ReleaseInternal). No separate queue data structure is
            // needed because only one train can ever hold a given section at a time.
            return true;
        }

        /// <summary>Claims or renews the calling vehicle's hold on <paramref name="section"/>, and
        /// releases whatever single-track section it held previously if that has changed.</summary>
        internal static void Occupy(SectionClassifier.Section section, ushort vehicleId, uint currentFrame)
        {
            if (section == null)
            {
                return;
            }

            if (_vehicleHeldSection.TryGetValue(vehicleId, out var previouslyHeld) && previouslyHeld != section)
            {
                ReleaseInternal(previouslyHeld, vehicleId);
            }

            if (_reservations.TryGetValue(section, out var existing))
            {
                if (existing.VehicleId == vehicleId || currentFrame - existing.LastRenewedFrame > StaleAfterFrames)
                {
                    existing.VehicleId = vehicleId;
                    existing.LastRenewedFrame = currentFrame;
                }
                // else: held by someone else and not stale - leave it as-is. The caller
                // (Patch_TrainAI_CalculateTargetSpeed) never calls Occupy for a section a vehicle
                // isn't already physically standing on, so this branch is only reachable if two
                // trains somehow entered the same single-track section at once (a collision the
                // speed-hold below is meant to prevent in the first place) - not overwriting the
                // existing holder here is the safer of the two options.
            }
            else
            {
                _reservations[section] = new Reservation { VehicleId = vehicleId, LastRenewedFrame = currentFrame };
            }

            _vehicleHeldSection[vehicleId] = section;
        }

        /// <summary>Releases whatever single-track section the given vehicle currently holds, if any -
        /// for when a vehicle leaves the simulation (despawns, is deleted) outside of the normal
        /// segment-to-segment transition Occupy() already handles.</summary>
        internal static void ReleaseVehicle(ushort vehicleId)
        {
            if (_vehicleHeldSection.TryGetValue(vehicleId, out var held))
            {
                ReleaseInternal(held, vehicleId);
            }
        }

        private static void ReleaseInternal(SectionClassifier.Section section, ushort vehicleId)
        {
            if (_reservations.TryGetValue(section, out var existing) && existing.VehicleId == vehicleId)
            {
                _reservations.Remove(section);
            }

            _vehicleHeldSection.Remove(vehicleId);
        }

        internal static void Clear()
        {
            _reservations.Clear();
            _vehicleHeldSection.Clear();
        }

        internal static uint CurrentFrame => Singleton<SimulationManager>.instance.m_currentFrameIndex;
    }
}
