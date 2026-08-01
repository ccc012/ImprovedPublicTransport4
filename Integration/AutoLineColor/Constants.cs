namespace AutoLineColor {
    public static class Constants {
        // Settings are centralized in IPT4's ModSetting (CSLModsCommon)
        // Logging is integrated into IPT3's centralized logging system via ImprovedPublicTransport.Util.Utils

        // Was 10s; 15s is enough for auto-colour of new lines and cuts lock/scan frequency.
        public const double UpdateIntervalSeconds = 15.0;

        public const int MaxLineAnalysisStops = 50;
    }
}
