using ImprovedPublicTransport;

namespace ExpressBusServices
{
    public class EBSModConfig
    {
        public enum ExpressMode
        {
            NONE = 0,
            PRUDENTIAL = 1,
            AGGRESSIVE = 2,
        }

        // this is mainly used to determine the kind of unbunching that should be enabled
        public static ExpressMode CurrentExpressBusMode { get; set; }

        // this is used to determine whether service self-balancing is enabled
        public static bool UseServiceSelfBalancing { get; set; }

        // this is used to determine whether service self-balancing can target middle stops
        public static bool ServiceSelfBalancingCanDoMiddleStop { get; set; }

        // this is used to determine whether minibus mode is enabled: fast board/depart for minibus vehicles
        public static bool CanUseMinibusMode { get; set; }

        // section break for express tram

        public enum ExpressTramMode
        {
            NONE = 0,
            LIGHT_RAIL,
            TRAM,
        }

        public static ExpressTramMode CurrentExpressTramMode { get; set; }

        /// <summary>
        /// Copy live options from <see cref="ModSetting"/> into the static runtime config
        /// that Harmony patches read on every boarding/unbunching decision.
        /// Shared by level-load and mid-session hot-apply.
        /// </summary>
        public static void SyncFromSettings(ModSetting settings = null)
        {
            settings = settings ?? ModSetting.Instance;
            CurrentExpressBusMode = (ExpressMode)settings.ExpressBusUnbunchingMode;
            UseServiceSelfBalancing = settings.ExpressBusEnableSelfBalancing;
            ServiceSelfBalancingCanDoMiddleStop = settings.ExpressBusAllowMiddleStopBalancing;
            CanUseMinibusMode = settings.ExpressBusEnableMinibusMode;
            CurrentExpressTramMode = (ExpressTramMode)settings.ExpressTramUnbunchingMode;
        }

        /// <summary>True when both bus and tram express modes are fully off (no patches needed).</summary>
        public static bool IsFullyDisabled(ModSetting settings = null)
        {
            settings = settings ?? ModSetting.Instance;
            return settings.ExpressBusUnbunchingMode == ModSetting.ExpressBusServicesModes.None
                && settings.ExpressTramUnbunchingMode == ModSetting.ExpressTramServicesModes.Disabled;
        }
    }
}
