using System;
using ImprovedPublicTransport.UI;
using ImprovedPublicTransport.Util;
using UnityEngine;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.HarmonyPatches.PublicTransportStopWorldInfoPanelPatches
{
    /// <summary>
    /// Gives a newly-selected, never-named stop a real name automatically (the same nearby-building
    /// suggestion the stop panel's own dropdown already offers, just applied without requiring the
    /// player to open the dropdown and pick one). A stop with no name has no effect on
    /// <see cref="ColossalFramework.Globalization.InstanceManager.GetName"/>-dependent features (e.g.
    /// TrainDisplayUpdated's route strip falls back to a bare "?" for it) until named - this makes
    /// that the common case rather than something the player has to notice and fix by hand. Only ever
    /// names a stop that has no name yet; a name set by the player (or by this patch, on an earlier
    /// visit) is never touched again.
    /// </summary>
    // Deliberately no [HarmonyPatch]/[HarmonyPostfix] attributes here - this is applied manually via
    // PatchUtil (see Apply()/Undo() below), the same convention every other native (non-Integration)
    // patch in HarmonyPatches/ uses. Attribute-based discovery would ALSO get picked up by every
    // Integration's own harmony.PatchAll(Assembly.GetExecutingAssembly()) call, which scans the whole
    // assembly rather than just its own namespace - that would apply this same postfix a second time
    // under an unrelated Harmony ID, running it twice per stop click.
    internal static class AutoNameStopPatch
    {
        private static readonly Type[] ShowArgumentTypes = { typeof(Vector3), typeof(InstanceID) };

        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(PublicTransportStopWorldInfoPanel), "Show", argumentTypes: ShowArgumentTypes),
                null,
                new PatchUtil.MethodDefinition(typeof(AutoNameStopPatch), nameof(Postfix))
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(PublicTransportStopWorldInfoPanel), "Show", argumentTypes: ShowArgumentTypes)
            );
        }

        public static void Postfix(InstanceID instanceID)
        {
            try
            {
                StopAutoNamer.EnsureNamed(instanceID);
            }
            catch (Exception ex)
            {
                Utils.LogError($"AutoNameStopPatch: failed to auto-name stop: {ex.Message}");
            }
        }
    }
}
