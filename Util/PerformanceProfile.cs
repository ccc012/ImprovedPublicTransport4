using ImprovedPublicTransport;

namespace ImprovedPublicTransport.Util
{
    /// <summary>
    /// Single place that maps <see cref="ModSetting.PerformanceProfiles"/> to concrete caps.
    /// Integrations should read these helpers instead of inventing their own magic numbers.
    /// </summary>
    public static class PerformanceProfile
    {
        public static ModSetting.PerformanceProfiles Current =>
            ModSetting.Instance?.PerformanceProfile ?? ModSetting.PerformanceProfiles.Normal;

        public static bool IsLight => Current == ModSetting.PerformanceProfiles.Light;
        public static bool IsMaximum => Current == ModSetting.PerformanceProfiles.Maximum;

        /// <summary>
        /// Train Display poll floor (seconds). Values are intentionally conservative after the
        /// 4.8 snappier-panel experiment caused hitching — Maximum no longer drops below 0.15s.
        /// </summary>
        public static float TrainDisplayMinPollSeconds => Current switch
        {
            ModSetting.PerformanceProfiles.Light => 0.40f,
            ModSetting.PerformanceProfiles.Maximum => 0.15f,
            _ => 0.20f,
        };

        /// <summary>Commuter citizen-grid scan hard cap.</summary>
        public static int CommuterMaxCitizens => Current switch
        {
            ModSetting.PerformanceProfiles.Light => 80,
            ModSetting.PerformanceProfiles.Maximum => 2000,
            _ => 200,
        };

        /// <summary>Commuter map icons + destinations kept.</summary>
        public static int CommuterMaxDestinations => Current switch
        {
            ModSetting.PerformanceProfiles.Light => 6,
            ModSetting.PerformanceProfiles.Maximum => 80,
            _ => 12,
        };

        /// <summary>Commuter panel destination list rows.</summary>
        public static int CommuterPanelListRows => Current switch
        {
            ModSetting.PerformanceProfiles.Light => 4,
            ModSetting.PerformanceProfiles.Maximum => 15,
            _ => 5,
        };

        /// <summary>Commuter panel refresh interval in frames (~60 fps).</summary>
        public static int CommuterRefreshFrames => Current switch
        {
            ModSetting.PerformanceProfiles.Light => 90,
            ModSetting.PerformanceProfiles.Maximum => 30,
            _ => 45,
        };

        /// <summary>When Full map detail is on, multiply caps (still clamped by profile max).</summary>
        public static int CommuterMaxCitizensEffective(bool fullMap)
        {
            var baseCap = CommuterMaxCitizens;
            if (!fullMap)
            {
                return baseCap;
            }

            // Full map wants more icons but Light still stays modest.
            return Current switch
            {
                ModSetting.PerformanceProfiles.Light => System.Math.Min(baseCap * 2, 160),
                ModSetting.PerformanceProfiles.Maximum => System.Math.Min(baseCap, 2500),
                _ => System.Math.Min(baseCap * 4, 1200),
            };
        }

        public static int CommuterMaxDestinationsEffective(bool fullMap)
        {
            var baseCap = CommuterMaxDestinations;
            if (!fullMap)
            {
                return baseCap;
            }

            return Current switch
            {
                ModSetting.PerformanceProfiles.Light => System.Math.Min(baseCap * 2, 12),
                ModSetting.PerformanceProfiles.Maximum => System.Math.Max(baseCap, 64),
                _ => System.Math.Min(baseCap * 3, 48),
            };
        }
    }
}
