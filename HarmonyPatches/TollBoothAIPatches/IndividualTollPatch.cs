using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ColossalFramework;
using HarmonyLib;
using ImprovedPublicTransport.Util;
using UnityEngine;

namespace ImprovedPublicTransport.HarmonyPatches.TollBoothAIPatches
{
    public static class IndividualTollPatch
    {
        private static readonly Type[] EnterTollRoadArguments =
        {
            typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(ushort), typeof(ushort), typeof(int)
        };

        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(TollBoothAI), "UpdatePathCost"),
                transpiler: new PatchUtil.MethodDefinition(typeof(IndividualTollPatch), nameof(UpdatePathCostTranspiler)));
            PatchEnterTollRoad(typeof(VehicleAI), nameof(EnterTollRoadPrefix));
            PatchEnterTollRoad(typeof(PassengerCarAI), nameof(PassengerCarEnterTollRoadPrefix));
            PatchEnterTollRoad(typeof(CargoTruckAI), nameof(CargoTruckEnterTollRoadPrefix));
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(typeof(TollBoothAI), "UpdatePathCost"));
            UnpatchEnterTollRoad(typeof(VehicleAI));
            UnpatchEnterTollRoad(typeof(PassengerCarAI));
            UnpatchEnterTollRoad(typeof(CargoTruckAI));
        }

        private static void PatchEnterTollRoad(Type type, string prefix)
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(type, nameof(VehicleAI.EnterTollRoad), argumentTypes: EnterTollRoadArguments),
                prefix: new PatchUtil.MethodDefinition(typeof(IndividualTollPatch), prefix));
        }

        private static void UnpatchEnterTollRoad(Type type)
        {
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(type, nameof(VehicleAI.EnterTollRoad), argumentTypes: EnterTollRoadArguments));
        }

        private static IEnumerable<CodeInstruction> UpdatePathCostTranspiler(MethodBase original, IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var clamp = AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp), new[] { typeof(int), typeof(int), typeof(int) });
            var replacement = AccessTools.Method(typeof(IndividualTollPatch), nameof(GetLaneCost));
            var ticketCost = AccessTools.Field(typeof(NetLane), nameof(NetLane.m_ticketCost));
            var matches = 0;
            for (var i = 7; i + 2 < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Call || !Equals(codes[i].operand, clamp)
                    || !LoadsInt(codes[i - 6], 99) || codes[i - 5].opcode != OpCodes.Add
                    || !LoadsInt(codes[i - 4], 100) || codes[i - 3].opcode != OpCodes.Div
                    || !LoadsInt(codes[i - 2], 1) || !LoadsInt(codes[i - 1], 255)
                    || codes[i + 1].opcode != OpCodes.Conv_U1
                    || codes[i + 2].opcode != OpCodes.Stfld || !Equals(codes[i + 2].operand, ticketCost))
                {
                    continue;
                }

                codes[i - 7].opcode = OpCodes.Ldarg_2;
                codes[i - 7].operand = null;
                codes[i - 6].opcode = OpCodes.Call;
                codes[i - 6].operand = replacement;
                for (var j = i - 5; j <= i; j++)
                {
                    codes[j].opcode = OpCodes.Nop;
                    codes[j].operand = null;
                }
                matches++;
            }

            if (matches != 1)
            {
                throw new InvalidOperationException(original.DeclaringType.FullName + "." + original.Name + " ticket-cost sequence count: " + matches);
            }

            return codes;
        }

        private static bool LoadsInt(CodeInstruction instruction, int value)
        {
            if (value == 1 && instruction.opcode == OpCodes.Ldc_I4_1) return true;
            if (instruction.opcode == OpCodes.Ldc_I4_S) return Convert.ToInt32(instruction.operand) == value;
            return instruction.opcode == OpCodes.Ldc_I4 && Convert.ToInt32(instruction.operand) == value;
        }

        private static byte GetLaneCost(ref Building building)
        {
            var value = (building.m_flags & Building.Flags.Active) == 0 ? 0 : 50 * building.m_education1;
            return (byte)Mathf.Clamp((value + 99) / 100, 0, 255);
        }

        private static bool EnterTollRoadPrefix(ref Vehicle vehicleData, ushort buildingID, int basePrice)
        {
            Charge(ref vehicleData, buildingID, basePrice, ItemClass.Level.Level1);
            return false;
        }

        private static bool PassengerCarEnterTollRoadPrefix(ref Vehicle vehicleData, ushort buildingID, int basePrice)
        {
            Charge(ref vehicleData, buildingID, basePrice, ItemClass.Level.Level1);
            return false;
        }

        private static bool CargoTruckEnterTollRoadPrefix(ref Vehicle vehicleData, ushort buildingID, int basePrice)
        {
            Charge(ref vehicleData, buildingID, basePrice * 2, ItemClass.Level.Level2);
            return false;
        }

        private static void Charge(ref Vehicle vehicleData, ushort buildingID, int price, ItemClass.Level level)
        {
            if (buildingID == 0) return;

            var buildingManager = Singleton<BuildingManager>.instance;
            var districtManager = Singleton<DistrictManager>.instance;
            var district = districtManager.GetDistrict(buildingManager.m_buildings.m_buffer[buildingID].m_position);
            if ((districtManager.m_districts.m_buffer[district].m_cityPlanningPolicies & DistrictPolicies.CityPlanning.AutomatedToll) != 0)
            {
                price = (price * 70 + Singleton<SimulationManager>.instance.m_randomizer.Int32(100u)) / 100;
                districtManager.m_districts.m_buffer[district].m_cityPlanningPoliciesEffect |= DistrictPolicies.CityPlanning.AutomatedToll;
            }
            else
            {
                vehicleData.m_flags2 |= Vehicle.Flags2.EndStop;
            }

            Singleton<EconomyManager>.instance.AddResource(EconomyManager.Resource.PublicIncome, price, ItemClass.Service.Vehicles, ItemClass.SubService.None, level);
            buildingManager.m_buildings.m_buffer[buildingID].m_customBuffer1 = (ushort)Mathf.Min(buildingManager.m_buildings.m_buffer[buildingID].m_customBuffer1 + 1, ushort.MaxValue);
        }
    }
}
