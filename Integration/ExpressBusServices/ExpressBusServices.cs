using CitiesHarmony.API;
using ICities;
using UnityEngine;
using ImprovedPublicTransport;

namespace ExpressBusServices
{
    public class ExpressBusServices : LoadingExtensionBase
    {
        /// <summary>
        /// Executed whenever a level completes its loading process.
        /// This mod activates and patches the game using Harmony library.
        /// </summary>
        /// <param name="mode">The loading mode.</param>
        public override void OnLevelLoaded(LoadMode mode)
        {
            switch (mode)
            {
                case LoadMode.LoadGame:
                case LoadMode.NewGame:
                case LoadMode.LoadScenario:
                case LoadMode.NewGameFromScenario:
                    break;

                default:
                    return;
            }

            // Sync stored settings into runtime config before patches run
            EBSModConfig.SyncFromSettings();

            // Safe-default install: do not register any EBS Harmony patches when both bus and tram
            // express modes are fully off (avoids unnecessary hot-path hooks with other mods).
            if (EBSModConfig.IsFullyDisabled())
            {
                return;
            }

            UnifyHarmonyVersions();
            PatchController.Activate();
        }

        /// <summary>
        /// Executed whenever a map is being unloaded.
        /// This mod then undoes the changes using the Harmony library.
        /// </summary>
        public override void OnLevelUnloading()
        {
            UnifyHarmonyVersions();
            PatchController.Deactivate();
        }

        private void UnifyHarmonyVersions()
        {
            if (HarmonyHelper.IsHarmonyInstalled)
            {
                // Harmony version management handled by CitiesHarmony
            }
        }
    }
}
