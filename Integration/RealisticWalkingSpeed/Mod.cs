using CitiesHarmony.API;
using ICities;
using RealisticWalkingSpeed.Patches;
using System.Collections.Generic;
using ImprovedPublicTransport.Util;
using ImprovedPublicTransport;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace RealisticWalkingSpeed
{
    /// <summary>
    /// RealisticWalkingSpeedMod entry points for IPT-managed lifecycle. This class is intentionally NOT an IUserMod
    /// so IPT can control initialization and teardown.
    /// </summary>
    public static class RealisticWalkingSpeedMod
    {
        // Store original walk speeds so we can restore them when disabling the mod
        private static Dictionary<CitizenInfo, float> _originalWalkSpeeds = new Dictionary<CitizenInfo, float>();
        private static bool _inGamePatchApplied = false;

        public static void EnableRealisticWalkingSpeedMod()
        {
            if (Diagnostics.VerboseRuntimeLogs)
            {
                Utils.Log("RealisticWalkingSpeed: EnableRealisticWalkingSpeedMod called");
            }
            HarmonyHelper.DoOnHarmonyReady(() => 
            {
                Patcher.PatchAll();
                if (ImprovedPublicTransportMod.InGame)
                {
                    ApplyInGamePatch();
                }
            });
        }

        public static void DisableRealisticWalkingSpeedMod()
        {
            if (Diagnostics.VerboseRuntimeLogs)
            {
                Utils.Log("RealisticWalkingSpeed: DisableRealisticWalkingSpeedMod called");
            }
            
            if (!HarmonyHelper.IsHarmonyInstalled)
            {
                Utils.LogWarning("RealisticWalkingSpeed: Harmony not installed, cannot disable patches");
                return;
            }

            Patcher.UnpatchAll();
            if (ImprovedPublicTransportMod.InGame)
            {
                RevertInGamePatch();
            }
        }

        public static void OnLevelLoaded(LoadMode mode)
        {
            if (mode != LoadMode.NewGame && mode != LoadMode.NewGameFromScenario && mode != LoadMode.LoadGame)
                return;

            if (Diagnostics.VerboseRuntimeLogs)
            {
                Utils.Log($"RealisticWalkingSpeed: Level loaded in mode {mode}");
            }
            ApplyInGamePatch();
        }

        private static void ApplyInGamePatch()
        {
            if (_inGamePatchApplied)
            {
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log("RealisticWalkingSpeed: In-game patch already applied, skipping.");
                }
                return;
            }

            if (PrefabCollection<CitizenInfo>.LoadedCount() == 0)
            {
                Utils.LogWarning("RealisticWalkingSpeed: Citizen prefabs are not loaded yet; skipping in-game speed patch for now.");
                return;
            }

            try
            {
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log("RealisticWalkingSpeed: Applying in-game walk speed patches...");
                }
                
                _originalWalkSpeeds.Clear();
                for (uint i = 0; i < PrefabCollection<CitizenInfo>.LoadedCount(); i++)
                {
                    var citizenPrefab = PrefabCollection<CitizenInfo>.GetLoaded(i);
                    if (citizenPrefab != null)
                    {
                        _originalWalkSpeeds[citizenPrefab] = citizenPrefab.m_walkSpeed;
                    }
                }

                new CitizenWalkingSpeedInGamePatch(new SpeedData()).Apply();
                _inGamePatchApplied = _originalWalkSpeeds.Count > 0;
                if (_inGamePatchApplied && Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log($"RealisticWalkingSpeed: In-game patches applied to {_originalWalkSpeeds.Count} citizen prefabs");
                }
            }
            catch (System.Exception ex)
            {
                Utils.LogError($"RealisticWalkingSpeed: Failed to apply in-game patches: {ex.Message}\n{ex.StackTrace}");
                _originalWalkSpeeds.Clear();
                _inGamePatchApplied = false;
            }
        }

        private static void RevertInGamePatch()
        {
            if (!_inGamePatchApplied)
            {
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log("RealisticWalkingSpeed: No in-game patches to revert, skipping.");
                }
                return;
            }

            try
            {
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log("RealisticWalkingSpeed: Reverting in-game walk speed patches...");
                }
                int revertedCount = 0;
                
                for (uint i = 0; i < PrefabCollection<CitizenInfo>.LoadedCount(); i++)
                {
                    var citizenPrefab = PrefabCollection<CitizenInfo>.GetLoaded(i);
                    if (citizenPrefab != null && _originalWalkSpeeds.ContainsKey(citizenPrefab))
                    {
                        citizenPrefab.m_walkSpeed = _originalWalkSpeeds[citizenPrefab];
                        revertedCount++;
                    }
                }

                _originalWalkSpeeds.Clear();
                _inGamePatchApplied = false;
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log($"RealisticWalkingSpeed: In-game patches reverted on {revertedCount} citizen prefabs");
                }
            }
            catch (System.Exception ex)
            {
                Utils.LogError($"RealisticWalkingSpeed: Failed to revert in-game patches: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}