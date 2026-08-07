using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ImprovedPublicTransport.Util
{
    /// <summary>
    /// Runtime compatibility checks run once per level load. When a confirmed conflict with an
    /// external mod is detected, the conflicting IPT4 feature is disabled (the mod stays enabled,
    /// the rest of IPT4 stays enabled) and a one-time warning is logged. This is deliberately
    /// conservative: only feature-level disabling, never whole-mod bans — bans live in
    /// IptModManager for mods IPT4 replaces entirely.
    /// </summary>
    public static class CompatibilityGuard
    {
        // TMCE (Transfer Manager CE) and its predecessor TME (Transfer Manager Extended) share
        // the same TaxiMove feature: both transpile TaxiAI.SimulationStep when their TaxiMove
        // option is on, and both patch TransportStationAI.CreateIncoming/OutgoingVehicle for the
        // airport-gate spawn fix. They are detected separately because each has its own Harmony
        // owner string.
        private const string TmceHarmonyOwner = "Sleepy.TransferManagerCE";
        private const string TmeHarmonyOwner = "Sleepy.TransferManagerExtended";

        private static bool _checkedOnce;

        /// <summary>Call after Harmony patches are applied, once per level load.</summary>
        public static void RunLevelChecks()
        {
            if (_checkedOnce)
            {
                return;
            }
            _checkedOnce = true;

            try
            {
                CheckTmceTaxiMoveVsTaxiStandFix();
            }
            catch (Exception ex)
            {
                Utils.LogError($"CompatibilityGuard: TMCE check failed: {ex.Message}");
            }
        }

        private static bool MethodIsPatchedBy(string typeName, string methodName, string owner)
        {
            var method = AccessTools.Method(typeName + ":" + methodName);
            if (method == null)
            {
                return false;
            }

            var info = Harmony.GetPatchInfo(method);
            if (info == null)
            {
                return false;
            }

            bool HasOwner(System.Collections.Generic.IEnumerable<Patch> patches)
            {
                if (patches == null) return false;
                foreach (var p in patches)
                {
                    if (string.Equals(p.owner, owner, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
                return false;
            }

            return HasOwner(info.Prefixes) || HasOwner(info.Postfixes)
                || HasOwner(info.Transpilers) || HasOwner(info.Finalizers);
        }

        /// <summary>
        /// TMCE/TME's TaxiMove feature patches TaxiAI.SimulationStep to take over taxi target
        /// assignment. IPT4's Taxi Stand Fix patches the same method to route idle taxis to
        /// stands — the two fight over who assigns the taxi's destination. When either mod has
        /// TaxiMove active, disable Taxi Stand Fix (both mods' docs list Taxi Stand Fix as
        /// incompatible). The rest of IPT4 stays untouched.
        /// </summary>
        private static void CheckTmceTaxiMoveVsTaxiStandFix()
        {
            // TaxiMove is only active if the mod actually patched TaxiAI.SimulationStep.
            bool tmceTaxiMove = MethodIsPatchedBy("TaxiAI", "SimulationStep", TmceHarmonyOwner);
            bool tmeTaxiMove = MethodIsPatchedBy("TaxiAI", "SimulationStep", TmeHarmonyOwner);
            if (!tmceTaxiMove && !tmeTaxiMove)
            {
                return;
            }

            var settings = ModSetting.Instance;
            if (settings != null && settings.EnableTaxiStandFix)
            {
                settings.EnableTaxiStandFix = false;
                var culprit = tmceTaxiMove ? "Transfer Manager CE (TaxiMove)" : "Transfer Manager Extended (TaxiMove)";
                Utils.LogWarning(
                    "CompatibilityGuard: " + culprit + " patches TaxiAI.SimulationStep, " +
                    "which conflicts with IPT4's Taxi Stand Fix. Taxi Stand Fix was disabled to avoid " +
                    "the two mods fighting over taxi destinations. Re-enable it in Options only if you " +
                    "turn TaxiMove off in that mod.");
            }
        }

        /// <summary>Reset the once-per-session guard when the level unloads.</summary>
        public static void Reset()
        {
            _checkedOnce = false;
        }
    }
}
