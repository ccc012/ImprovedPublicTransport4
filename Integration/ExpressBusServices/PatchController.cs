using ExpressBusServices.PerformanceBoost;
using ExpressBusServices.Redeployment;
using HarmonyLib;
using ExpressBusServices.DataTypes;
using ImprovedPublicTransport.Util;

namespace ExpressBusServices
{
    internal class PatchController
    {
        // public const so BetterBoarding [HarmonyAfter] can reference the same owner string.
        public const string HarmonyModID = "com.vectorial1024.cities.ebs";

        /*
         * The "singleton" design is pretty straight-forward.
         */

        private static Harmony harmony;
        private static bool _isActive;

        public static bool IsActive => _isActive;

        public static Harmony GetHarmonyInstance()
        {
            if (harmony == null)
            {
                harmony = new Harmony(HarmonyModID);
            }

            return harmony;
        }

        /// <summary>
        /// Register EBS Harmony patches. Safe to call when already active (no-op): mode and
        /// self-balancing toggles are live via <see cref="EBSModConfig"/> without re-patching.
        /// </summary>
        public static void Activate()
        {
            if (_isActive)
            {
                // Still refresh the vehicle-property cache so a mid-session mode change does not
                // keep stale unbunching assumptions for vehicles already on the road.
                CachedVehicleProperties.TouchAndResetCache();
                return;
            }

            HarmonyScope.PatchNamespace(GetHarmonyInstance(), "ExpressBusServices");

            VehiclePaxDeltaInfo.EnsureTableExists();
            BusStopSkippingLookupTable.EnsureTableExists();
            ServiceBalancerUtil.EnsureTableExists();

            TeleportRedeployInstructions.EnsureTableExists();

            CachedVehicleProperties.TouchAndResetCache();
            _isActive = true;
        }

        /// <summary>Unpatch and wipe runtime tables. Safe if already inactive.</summary>
        public static void Deactivate()
        {
            if (!_isActive)
            {
                return;
            }

            GetHarmonyInstance().UnpatchAll(HarmonyModID);

            VehiclePaxDeltaInfo.WipeTable();
            BusStopSkippingLookupTable.WipeTable();
            ServiceBalancerUtil.ResetRedeploymentRecords();

            TeleportRedeployInstructions.WipeTable();

            CachedVehicleProperties.TouchAndResetCache();
            _isActive = false;
        }
    }
}
