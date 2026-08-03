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

        /// <summary>
        /// Ticket Prices tab passenger count refresh interval (seconds). The refresh scans the
        /// entire vehicle buffer (up to 16384 or 65536 slots), so lighter profiles refresh less
        /// frequently to preserve framerate.
        /// </summary>
        public static float TicketPricesRefreshSeconds => Current switch
        {
            ModSetting.PerformanceProfiles.Light => 12f,
            ModSetting.PerformanceProfiles.Maximum => 3f,
            _ => 5f,
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

        /// <summary>
        /// Maximum citizens to inspect per waiting passenger query. Each inspection may call
        /// TransportArriveAtSource, which is expensive, so lighter profiles inspect fewer citizens.
        /// </summary>
        public static int WaitingPassengerMaxInspect => Current switch
        {
            ModSetting.PerformanceProfiles.Light => 80,
            ModSetting.PerformanceProfiles.Maximum => 400,
            _ => 150,
        };

    }
}
