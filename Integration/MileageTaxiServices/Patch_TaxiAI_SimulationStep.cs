using HarmonyLib;
using System;
using System.Reflection;
using ColossalFramework;
using JetBrains.Annotations;
using UnityEngine;
using ImprovedPublicTransport.Util;

namespace MileageTaxiServices
{
    [HarmonyPatch]
    [UsedImplicitly]
    public class Patch_TaxiAI_SimulationStep
    {
        /// <summary>
        /// Equals to 1/2000, which is half of the standard "journey displacement" fare rate
        /// </summary>
        private const double TaxiMileageFareRate = 1.0 / 2000.0;

        /// <summary>
        /// Generous upper bound (in-game units, ~meters) a taxi could plausibly travel in a single
        /// simulation frame. Guards against corrupted/uninitialized frame data (e.g. right after a
        /// taxi spawns, or stale frame history after loading a save) producing an implausibly large
        /// distance - which would overflow the (int) cast below into a huge garbage value (often a
        /// large negative number on Mono), draining the public transport budget in one step. This is
        /// the known upstream "snowballing negative taxi income" issue.
        /// </summary>
        private const double MaxPlausibleFrameDistance = 200.0;

        [UsedImplicitly]
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("TaxiAI:SimulationStep", new Type[]
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
        [UsedImplicitly]
        public static void HandleTaxiGenerateMileageIncome(TaxiAI __instance, ushort vehicleID, ref Vehicle vehicleData)
        {
            if (!HasPassenger(ref vehicleData))
            {
                // no passenger; skip
                return;
            }
            // has passenger
            // then simple: generate some money for it
            // Utils.Log($"Taxi vehicle ID: {vehicleID}; generating an income.");

            // calculate the instantaneous fare based on the distance travelled
            // we use "incremental pay-in-advance" as per usual incremental fare schemes IRL
            // this means idling (i.e. not enough distance travelled) also generates a little bit of fare
            // this allows maximum compatibility with other fare-scaling mods
            // note to fellow programmers: if rounding up of non-negative numbers is needed, then can simply use (int)x + ((int)(x%1 - 1) + 1)
            var distance = DetermineDelta(ref vehicleData);
            if (double.IsNaN(distance) || distance < 0 || distance > MaxPlausibleFrameDistance)
            {
                // Corrupted/uninitialized frame data - skip this frame's fare rather than risk an int overflow.
                return;
            }

            if (__instance.m_transportInfo == null || __instance.m_info?.m_class == null)
            {
                return;
            }

            var standardInstantFare = __instance.m_transportInfo.m_ticketPrice * distance * TaxiMileageFareRate;
            var instantFare = (int)standardInstantFare + 1;
            Singleton<EconomyManager>.instance.AddResource(EconomyManager.Resource.PublicIncome, instantFare, __instance.m_info.m_class);
        }

        private static bool HasPassenger(ref Vehicle vehicleData)
        {
            // Taxi occupancy is tracked by transfer size in the base game data.
            // Using it directly is more stable than relying on a reverse patch into TaxiAI.
            return vehicleData.m_transferSize > 0;
        }

        private static double DetermineDelta(ref Vehicle vehicleData)
        {
            /*
             * important note:
             * taxi must have the last 4 frames available (i.e. data for past real-life 1 second)
             * because it must have driven for some distance before picking up passengers
             */

            var lastFrameRadix = vehicleData.m_lastFrame;
            var secondLastFrameRadix = lastFrameRadix == 0 ? 3 : lastFrameRadix - 1;
            var lastFramePosition = vehicleData.GetLastFramePosition();
            Vehicle.Frame secondLastFrame;
            switch (secondLastFrameRadix)
            {
                case 0:
                    secondLastFrame = vehicleData.m_frame0;
                    break;
                case 1:
                    secondLastFrame = vehicleData.m_frame1;
                    break;
                case 2:
                    secondLastFrame = vehicleData.m_frame2;
                    break;
                case 3:
                    secondLastFrame = vehicleData.m_frame3;
                    break;
                default:
                    secondLastFrame = vehicleData.m_frame0;
                    break;
            }
            var secondLastFramePosition = secondLastFrame.m_position;

            return Vector3.Distance(secondLastFramePosition, lastFramePosition);
        }
    }
}


