using ColossalFramework;
using System.Collections.Generic;
using System.Linq;
using BetterBoarding.DataTypes;
using UnityEngine;
using ImprovedPublicTransport.Util;

namespace BetterBoarding
{
    public static class BoardingUtility
    {
        public struct PassengerChoice
        {
            public readonly ushort CitizenID;
            public readonly ushort VehicleID;
            public readonly ushort BoardingVehicleID;
            public readonly Vector3 EntryPosition;

            public PassengerChoice(ushort citizenID, ushort vehicleID, ushort boardingVehicleID, Vector3 entryPosition)
            {
                CitizenID = citizenID;
                VehicleID = vehicleID;
                BoardingVehicleID = boardingVehicleID;
                EntryPosition = entryPosition;
            }
        }

        public static int HandleBetterBoarding(ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
        {
            // the one-stop replacement to LoadPassengers
            if (currentStop == 0 || nextStop == 0)
            {
                return 0;
            }
            
            // remember to reset the wait time alarm! otherwise outside vehicles would keep endlessly spawning
            var netManager = Singleton<NetManager>.instance;
            netManager.m_nodes.m_buffer[currentStop].m_maxWaitTime = 0;

            var trainStatus = new TrainOccupancyInfo(vehicleID);
            var paxStatus = new PassengerWaitingInfo(currentStop, nextStop);
            /*
             * generate matches here! the flow is:
             * each passenger indicates their vehicle choice, closest vehicle first
             * then, process the first choice;
             * then, process the second choice;
             * ...
             * do this until one of:
             * - all are boarded
             * - train is full
             * best is if we can somehow know this as soon as possible to stop unnecessary iteration
             */

            // prepare ranked choices
            var freeVehiclesList = trainStatus.FreeCompartments;
            var maxRank = freeVehiclesList.Count;
            if (maxRank == 0)
            {
                // no free vehicles; simply stop
                return 0;
            }
            var sortedPaxList = paxStatus.SortedPassengers;
            var paxCount = sortedPaxList.Count;
            var initialFreeCapacity = trainStatus.FreeCapacity;
            var freeCapacity = initialFreeCapacity;
            if (maxRank == 1 && paxCount > freeCapacity)
            {
                // optimization: if there is only 1 possible vehicle, and there are too many passengers
                // then we can simply look at the first k passengers, where k = free space remaining
                sortedPaxList = sortedPaxList.GetRange(0, freeCapacity);
            }
            var paxRankedChoice = new PassengerChoice[maxRank, paxCount];
            var currentPaxIndex = 0;
            // var debugString = new StringBuilder();
            foreach (var paxInfo in sortedPaxList)
            {
                // find nth closest vehicle
                var paxPosition = paxInfo.Position;
                // Sort by distance using Array.Sort to avoid per-passenger LINQ IEnumerable allocation.
                var vehicleCount = freeVehiclesList.Count;
                var sortBuffer = new VehicleOccupancyInfo[vehicleCount];
                for (int si = 0; si < vehicleCount; si++) sortBuffer[si] = freeVehiclesList[si];
                System.Array.Sort(sortBuffer, (a, b) =>
                    Vector3.SqrMagnitude(paxPosition - a.Position)
                    .CompareTo(Vector3.SqrMagnitude(paxPosition - b.Position)));
                var rank = 0;
                foreach (var vehicle in sortBuffer)
                {
                    paxRankedChoice[rank, currentPaxIndex] = new PassengerChoice(
                        paxInfo.CitizenID,
                        vehicle.VehicleID,
                        vehicle.BoardingVehicleID,
                        vehicle.Position
                    );
                    // debugString.AppendLine($"Pax {paxInfo.CitizenID} rank {rank} picks vehicle {vehicle.VehicleID}");
                    ++rank;
                }
                ++currentPaxIndex;
            }
            // Utils.LogError(debugString.ToString());

            // ranked choices ready; process them!
            var instance3 = Singleton<NetManager>.instance;
            var num = instance3.m_nodes.m_buffer[currentStop].m_tempCounter;
            ProcessRankedChoices(paxRankedChoice, paxStatus.CurrentStopPosition, ref freeCapacity, ref num);

            // finalize the stuff
            instance3.m_nodes.m_buffer[currentStop].m_tempCounter = (ushort)Mathf.Min(num, 65535);
            
            // Return number of passengers boarded
            return initialFreeCapacity - freeCapacity;
        }

        private static void ProcessRankedChoices(PassengerChoice[,] paxRankedChoice, Vector3 stopPosition, ref int freeSpaceRemaining, ref ushort serviceCount)
        {
            ref var vehicleBuffer = ref Singleton<VehicleManager>.instance.m_vehicles.m_buffer;
            var citizenManager = Singleton<CitizenManager>.instance;
            var maxRank = paxRankedChoice.GetLength(0);
            var paxCount = paxRankedChoice.GetLength(1);
            var boardedPaxIDs = new HashSet<ushort>();
            for (var currentRank = 0; currentRank < maxRank; currentRank++)
            {
                for (var currentPaxIndex = 0; currentPaxIndex < paxCount; currentPaxIndex++)
                {
                    var currentRankedChoice = paxRankedChoice[currentRank, currentPaxIndex];
                    // check whether we need to do this
                    var citizenID = currentRankedChoice.CitizenID;
                    if (boardedPaxIDs.Contains(citizenID))
                    {
                        // already boarded; skip
                        continue;
                    }
                    // use chosen picking target for distance/entry, but board onto a valid vehicle (e.g., trailer choice on bus boards main vehicle)
                    var chosenVehicleID = currentRankedChoice.VehicleID;
                    var boardingVehicleID = currentRankedChoice.BoardingVehicleID;
                    if (boardingVehicleID == 0 || boardingVehicleID >= vehicleBuffer.Length)
                    {
                        continue;
                    }
                    var freeCitUnitID = vehicleBuffer[boardingVehicleID].GetNotFullCitizenUnit(CitizenUnit.Flags.Vehicle);
                    if (freeCitUnitID == 0)
                    {
                        // no room on the boarding vehicle
                        continue;
                    }
                    if (citizenID == 0)
                    {
                        continue;
                    }
                    ref var citizenInstance = ref citizenManager.m_instances.m_buffer[citizenID];
                    var citizenInfo = citizenInstance.Info;
                    if (citizenInfo == null || citizenInfo.m_citizenAI == null)
                    {
                        // citizen info invalid; try next
                        continue;
                    }
                    var entryPosition = currentRankedChoice.EntryPosition;
                    if (!citizenInfo.m_citizenAI.SetCurrentVehicle(citizenID, ref citizenInstance, boardingVehicleID, freeCitUnitID, entryPosition))
                    {
                        // somehow couldn't do it; try next
                        continue;
                    }
                    // successful assignment
                    serviceCount++;
                    if (boardingVehicleID > 0 && boardingVehicleID < vehicleBuffer.Length)
                    {
                        vehicleBuffer[boardingVehicleID].m_transferSize++;
                    }
                    boardedPaxIDs.Add(citizenID);
                    freeSpaceRemaining--;
                    if (freeSpaceRemaining <= 0)
                    {
                        // vehicle full; stop!
                        return;
                    }
                }
            }
        }
    }

    [System.Obsolete("Use BoardingUtility instead.")]
    public static class PassengerTrainUtility
    {
        public static int HandleBetterBoarding(ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
        {
            return BoardingUtility.HandleBetterBoarding(vehicleID, ref data, currentStop, nextStop);
        }
    }
}

