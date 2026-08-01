// Adapted from SharedStopEnabler (GPL-3.0, Workshop 2096382380, github.com/CodeBardian/SharedStopEnabler) - see LICENSE.txt.
using System;
using HarmonyLib;
using ImprovedPublicTransport;
using UnityEngine;

namespace SharedStopEnabler
{
    /// <summary>
    /// Elevated/bridge stops need GetClosestLanePosition without the requireConnect gate.
    /// </summary>
    [HarmonyPatch(typeof(NetSegment))]
    [HarmonyPatch("GetClosestLanePosition")]
    [HarmonyPatch(
        new[]
        {
            typeof(Vector3), typeof(NetInfo.LaneType), typeof(VehicleInfo.VehicleType),
            typeof(VehicleInfo.VehicleCategory), typeof(VehicleInfo.VehicleType), typeof(bool),
            typeof(Vector3), typeof(int), typeof(float), typeof(Vector3), typeof(int), typeof(float),
        },
        new[]
        {
            ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal,
            ArgumentType.Normal, ArgumentType.Normal,
            ArgumentType.Out, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out,
        })]
    internal static class Patch_NetSegment_GetClosestLanePosition
    {
        [HarmonyPrefix]
        public static void Prefix(NetSegment __instance, ref bool requireConnect)
        {
            if (!ModSetting.Instance.EnableSharedStopEnabler || !requireConnect)
            {
                return;
            }

            if (__instance.Info?.m_netAI is RoadBridgeAI)
            {
                requireConnect = false;
            }
        }
    }
}
