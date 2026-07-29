using System;
using ImprovedPublicTransport.Util;
using Utils = ImprovedPublicTransport.Util.Utils;

namespace RealisticWalkingSpeed.Patches
{
    public class CitizenWalkingSpeedInGamePatch : IInGamePatch
    {
        private readonly SpeedData _speedData;

        public CitizenWalkingSpeedInGamePatch(SpeedData speedData)
        {
            _speedData = speedData ?? throw new ArgumentNullException(nameof(speedData));
        }

        public void Apply()
        {
            try
            {
                int modifiedCount = 0;
                for (uint i = 0; i < PrefabCollection<CitizenInfo>.LoadedCount(); i++)
                {
                    var citizenPrefab = PrefabCollection<CitizenInfo>.GetLoaded(i);
                    if (citizenPrefab == null)
                        continue;

                    float newSpeed = _speedData.GetAverageSpeed(citizenPrefab.m_agePhase, citizenPrefab.m_gender);
                    citizenPrefab.m_walkSpeed = newSpeed;
                    modifiedCount++;
                }
                if (Diagnostics.VerboseRuntimeLogs)
                {
                    Utils.Log($"CitizenWalkingSpeedInGamePatch: Applied realistic walking speeds to {modifiedCount} citizen prefabs");
                }
            }
            catch (System.Exception ex)
            {
                Utils.LogError($"CitizenWalkingSpeedInGamePatch: Failed to apply patches: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}