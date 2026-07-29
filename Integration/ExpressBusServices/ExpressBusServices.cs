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
            var settings = ModSetting.Instance;
            EBSModConfig.CurrentExpressBusMode = (EBSModConfig.ExpressMode)settings.ExpressBusUnbunchingMode;
            EBSModConfig.UseServiceSelfBalancing = settings.ExpressBusEnableSelfBalancing;
            EBSModConfig.ServiceSelfBalancingCanDoMiddleStop = settings.ExpressBusAllowMiddleStopBalancing;
            EBSModConfig.CanUseMinibusMode = settings.ExpressBusEnableMinibusMode;
            EBSModConfig.CurrentExpressTramMode = (EBSModConfig.ExpressTramMode)settings.ExpressTramUnbunchingMode;

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
