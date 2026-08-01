using ColossalFramework;
using ColossalFramework.UI;
using ImprovedPublicTransport.Util;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace ImprovedPublicTransport.HarmonyPatches.EconomyPanelPatches
{
    /// <summary>
    /// Harmony postfix on EconomyPanel.Awake to inject the Ticket Prices tab.
    /// Also provides TryInjectNow() to attempt injection immediately.
    /// </summary>
    internal static class EconomyPanelAwakePatch
    {
        private static readonly System.Reflection.MethodInfo EconomyAwakeMethod =
            typeof(EconomyPanel).GetMethod("Awake",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        public static void Apply()
        {
            if (Diagnostics.VerboseTranspileLogs)
            {
                Utils.Log("EconomyPanelAwakePatch: Attempting to apply patch on EconomyPanel.Awake");
            }

            // Log if someone else already patched EconomyPanel.Awake
            var economyAwakeMethod = EconomyAwakeMethod;
            if (economyAwakeMethod == null)
            {
                Utils.LogWarning("EconomyPanelAwakePatch: Could not find EconomyPanel.Awake method for patching.");
            }
            else
            {
                PatchUtil.LogExistingPatches(economyAwakeMethod);
            }

            // Try to inject immediately if the panel already exists
            TryInjectNow();
            
            // Also apply the patch for when the panel is created/awakened later
            if (economyAwakeMethod != null)
            {
                PatchUtil.Patch(
                    new PatchUtil.MethodDefinition(typeof(EconomyPanel), "Awake"),
                    postfix: new PatchUtil.MethodDefinition(typeof(EconomyPanelAwakePatch), nameof(Postfix))
                );
                if (Diagnostics.VerboseTranspileLogs)
                {
                    Utils.Log("EconomyPanelAwakePatch: Patch applied");
                }
            }
            else
            {
                Utils.LogError("EconomyPanelAwakePatch: Skipping Harmony patch because EconomyPanel.Awake was not found.");
            }
        }

        public static void Undo()
        {
            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(EconomyPanel), "Awake")
            );
            Integration.TicketPriceCustomizer.TicketPricesTab.Cleanup();
        }

        /// <summary>
        /// Try to find and inject the Ticket Prices tab into an existing Economy panel.
        /// </summary>
        private static void TryInjectNow()
        {
            try
            {
                var uiView = UIView.GetAView();
                if (uiView == null)
                {
                    return;
                }

                // Try to find EconomyPanel by searching components
                var components = uiView.GetComponentsInChildren<EconomyPanel>();
                EconomyPanel economyPanel = components.Length > 0 ? components[0] : null;
                
                if (economyPanel == null)
                {
                    return;
                }

                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log("EconomyPanelAwakePatch: Found existing EconomyPanel, injecting immediately");
                }
                Integration.TicketPriceCustomizer.TicketPricesTab.InjectTab(economyPanel);
            }
            catch (System.Exception ex)
            {
                Utils.LogWarning($"EconomyPanelAwakePatch: TryInjectNow failed: {ex.Message}");
            }
        }

        private static void Postfix(EconomyPanel __instance)
        {
            if (Diagnostics.VerboseRuntimeLogs)
            {
                Utils.Log("EconomyPanelAwakePatch: Postfix called - injecting Ticket Prices tab");
            }
            Integration.TicketPriceCustomizer.TicketPricesTab.InjectTab(__instance);
        }
    }
}