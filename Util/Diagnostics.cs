namespace ImprovedPublicTransport.Util
{
    /// <summary>
    /// Simple diagnostics flags used across IPT integrations.
    /// Toggle values here during development; consider exposing via settings later.
    /// </summary>
    public static class Diagnostics
    {
        /// <summary>
        /// When true, integration transpilers may log additional details to the IPT log.
        /// Default is false to avoid noisy logs in release builds.
        /// </summary>
        public static bool VerboseTranspileLogs => false;

        /// <summary>
        /// When true, runtime patches may emit high-frequency trace logging.
        /// Keep disabled in normal gameplay to avoid log spam and unnecessary overhead.
        /// </summary>
        public static bool VerboseRuntimeLogs => false;
    }
}
