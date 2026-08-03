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
        /// Train Display poll interval multiplier, applied to the player's own Options slider
        /// value. A plain floor (the previous design) stopped doing anything the moment the
        /// slider's own value exceeded it - which is true at the slider's default (0.25s) against
        /// both Normal (was 0.20s) and Maximum (was 0.15s), so switching profiles had no visible
        /// effect for most players. A multiplier always changes the effective interval relative to
        /// whatever the slider says, in either direction, while TrainDisplayWatcher still clamps
        /// the result to a hard 0.1s floor - the same safety net that already exists there, kept
        /// after the 4.8 snappier-panel experiment caused hitching on sub-0.1s intervals. At the
        /// slider's own default, Light/Maximum land on the same 0.40s/0.15s this used to hardcode,
        /// so nothing changes for a player who never touches the slider.
        /// </summary>
        public static float TrainDisplayPollMultiplier => Current switch
        {
            ModSetting.PerformanceProfiles.Light => 1.6f,
            ModSetting.PerformanceProfiles.Maximum => 0.6f,
            _ => 1f,
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
