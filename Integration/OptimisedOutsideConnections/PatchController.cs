using System.Reflection;
using HarmonyLib;
using ImprovedPublicTransport;
using ImprovedPublicTransport.Util;
using UnityEngine;

namespace OptimisedOutsideConnections
{
    internal static class PatchController
    {
        public const string HarmonyModID = "IPT4.OptimisedOutsideConnections";

        private static Harmony _harmony;

        private static Harmony GetHarmonyInstance() => _harmony ??= new Harmony(HarmonyModID);

        public static void Activate()
        {
            GetHarmonyInstance().PatchAll(Assembly.GetExecutingAssembly());
            ApplyDummyTrafficSetting();
        }

        public static void Deactivate()
        {
            GetHarmonyInstance().UnpatchAll(HarmonyModID);
        }

        // Vanilla's own dummy traffic factor on each outside-connection prefab (m_dummyTrafficFactor,
        // default 1000) controls how much decorative pass-through traffic that connection spawns
        // purely for visual atmosphere - it never actually enters/exits the city. Zeroing it removes
        // that decorative traffic while leaving real import/export/passenger flow untouched. Applied
        // once per level load (like upstream), not Harmony-patched, since it is a one-time prefab
        // field write rather than an ongoing behaviour change. Matched by prefab name, same as
        // upstream - there is no other per-connection-type identifier available on OutsideConnectionAI.
        private static void ApplyDummyTrafficSetting()
        {
            if (!ModSetting.Instance.EnableOptimisedOutsideConnections)
            {
                return;
            }

            var setting = ModSetting.Instance;
            var connections = Resources.FindObjectsOfTypeAll<OutsideConnectionAI>();
            var affected = 0;
            foreach (var connection in connections)
            {
                if (connection == null)
                {
                    continue;
                }

                var disable = connection.name switch
                {
                    "Train Connection" => setting.DisableTrainDummyTraffic,
                    "Airplane Connection" => setting.DisablePlaneDummyTraffic,
                    "Ship Connection" => setting.DisableShipDummyTraffic,
                    "Road Connection Small" or "Road Connection" => setting.DisableRoadDummyTraffic,
                    _ => false,
                };

                if (disable)
                {
                    connection.m_dummyTrafficFactor = 0;
                    affected++;
                }
            }

            Utils.Log($"OptimisedOutsideConnections: dummy traffic disabled on {affected} of {connections.Length} outside connection prefab(s).");
        }
    }
}
