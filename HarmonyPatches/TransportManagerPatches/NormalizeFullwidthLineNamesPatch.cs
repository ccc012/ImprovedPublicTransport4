using System;
using ColossalFramework;
using ImprovedPublicTransport.Util;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.HarmonyPatches.TransportManagerPatches
{
    public static class NormalizeFullwidthLineNamesPatch
    {
        public static void Apply()
        {
            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(TransportManager),
                    nameof(TransportManager.UpdateData),
                    argumentTypes: new[] { typeof(SimulationManager.UpdateMode) }),
                postfix: new PatchUtil.MethodDefinition(typeof(NormalizeFullwidthLineNamesPatch),
                    nameof(Postfix))
            );
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(TransportManager),
                    nameof(TransportManager.UpdateData),
                    argumentTypes: new[] { typeof(SimulationManager.UpdateMode) })
            );
        }

        private static void Postfix()
        {
            try
            {
                TransportManager transportManager = Singleton<TransportManager>.instance;
                InstanceManager instanceManager = Singleton<InstanceManager>.instance;
                if (transportManager == null || instanceManager == null)
                {
                    return;
                }

                for (ushort lineId = 1; lineId < transportManager.m_lines.m_size; lineId++)
                {
                    ref TransportLine line = ref transportManager.m_lines.m_buffer[lineId];
                    if ((line.m_flags & TransportLine.Flags.CustomName) == 0)
                    {
                        continue;
                    }

                    InstanceID id = default;
                    id.TransportLine = lineId;
                    string currentName = instanceManager.GetName(id);
                    if (string.IsNullOrEmpty(currentName) || !FullwidthDigitNormalizer.ContainsFullwidthDigits(currentName))
                    {
                        continue;
                    }

                    string normalizedName = FullwidthDigitNormalizer.NormalizeDigits(currentName);
                    if (normalizedName == currentName)
                    {
                        continue;
                    }

                    instanceManager.SetName(id, normalizedName);
                    Utils.Log($"RescueFullwidthDigits: normalized line #{lineId} name from '{currentName}' to '{normalizedName}'.");
                }
            }
            catch (Exception ex)
            {
                Utils.LogError($"RescueFullwidthDigits: failed to normalize custom transport line names: {ex.Message}");
            }
        }
    }
}
