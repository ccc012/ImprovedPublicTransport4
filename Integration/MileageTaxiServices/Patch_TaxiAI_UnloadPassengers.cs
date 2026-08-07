// Adapted from Mileage Taxi Services (MIT, github.com/Vectorial1024/MileageTaxiServices) - see LICENSE.txt.
// Companion to Patch_TaxiAI_SimulationStep: vanilla's TaxiAI.UnloadPassengers pays the taxi its full
// straight-line-distance fare via EconomyManager.AddResource on arrival. Since Patch_TaxiAI_SimulationStep
// already paid an incremental per-frame fare for the whole trip, leaving vanilla's arrival AddResource call
// untouched would double-charge riders / over-pay taxis. This transpiler finds that single AddResource call
// inside UnloadPassengers and replaces it with a much smaller flat "base mileage" fare, so the incremental
// fare *replaces* the vanilla arrival fare instead of stacking on top of it.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ColossalFramework;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace MileageTaxiServices
{
    [HarmonyPatch]
    [UsedImplicitly]
    public static class Patch_TaxiAI_UnloadPassengers
    {
        /// <summary>
        /// Equals to 1/1000, the standard "journey displacement" fare rate (matches the vanilla rate
        /// that Patch_TaxiAI_SimulationStep's comment refers to as "the standard" rate).
        /// </summary>
        private const float VanillaTaxiFareRate = 1f / 1000f;

        /// <summary>
        /// Flat base mileage (in-game meters) granted to a taxi on arrival, replacing vanilla's full
        /// straight-line-distance arrival fare now that the trip was already paid incrementally.
        /// </summary>
        private const int TaxiBaseMileage = 500;

        [UsedImplicitly]
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("TaxiAI:UnloadPassengers");
        }

        [HarmonyTranspiler]
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> HandleTaxiPaysNoFareWhenArrive(IEnumerable<CodeInstruction> instructions)
        {
            /*
             * Replace the callvirt AddResource with a call to our own dummy method, so it effectively
             * becomes a no-op while still consuming the leftover symbols on the stack (and pushing back
             * an int, matching AddResource's own return type, so the stack stays balanced).
             */
            var signature = new List<Type>
            {
                typeof(EconomyManager.Resource),
                typeof(int),
                typeof(ItemClass)
            };
            return new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Callvirt,
                        AccessTools.Method(typeof(EconomyManager), nameof(EconomyManager.AddResource),
                            signature.ToArray()))
                ) // find the (only) occurrence of .AddResource()
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, typeof(Patch_TaxiAI_UnloadPassengers).GetMethod(nameof(HandleTaxiBaseMileage)))
                ) // insert replacement call to consume the stack symbols in place of the original call
                .Set(OpCodes.Nop, null) // and ignore the original instruction
                .InstructionEnumeration();
        }

        [UsedImplicitly]
        public static int HandleTaxiBaseMileage(this EconomyManager manager, EconomyManager.Resource resource, int amount, ItemClass itemClass, TaxiAI taxiInstance)
        {
            if (taxiInstance?.m_transportInfo == null)
                return 0;
            // "Abuse" this replacement call site to pay a flat "taxi base mileage fare" instead of
            // vanilla's full straight-line-distance arrival fare.
            var baseFare = Mathf.RoundToInt(taxiInstance.m_transportInfo.m_ticketPrice * TaxiBaseMileage * VanillaTaxiFareRate);
            manager.AddResource(resource, baseFare, itemClass);
            return 0;
        }
    }
}
