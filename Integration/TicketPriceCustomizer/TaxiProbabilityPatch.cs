using ColossalFramework;
using ImprovedPublicTransport.Util;

namespace ImprovedPublicTransport.Integration.TicketPriceCustomizer
{
    internal static class TaxiProbabilityPatch
    {
        public static void Apply()
        {
            var postfix = new PatchUtil.MethodDefinition(typeof(TaxiProbabilityPatch), nameof(Postfix));
            PatchUtil.Patch(new PatchUtil.MethodDefinition(typeof(ResidentAI), "GetTaxiProbability"), postfix: postfix);
            PatchUtil.Patch(new PatchUtil.MethodDefinition(typeof(TouristAI), "GetTaxiProbability"), postfix: postfix);
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(typeof(ResidentAI), "GetTaxiProbability"));
            PatchUtil.Unpatch(new PatchUtil.MethodDefinition(typeof(TouristAI), "GetTaxiProbability"));
        }

        private static void Postfix(ref int __result)
        {
            if (__result <= 0 || ModSetting.Instance.TicketPriceCustomizerMode != ModSetting.TicketPriceCustomizerModes.Enabled)
            {
                return;
            }

            var settings = ModSetting.Instance.TicketPriceCustomizer;
            if (settings == null)
            {
                return;
            }

            Adjust(ref __result, Singleton<SimulationManager>.instance.m_isNightTime ? settings.TaxiNightMultiplier : settings.TaxiMultiplier);
        }

        private static void Adjust(ref int probability, float multiplier)
        {
            if (probability <= 0 || multiplier == 1f)
            {
                return;
            }

            probability = multiplier <= 0f ? 100 : System.Math.Max(1, System.Math.Min(100, (int)System.Math.Round(probability / multiplier)));
        }
    }
}
