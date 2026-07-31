using CSLModsCommon.Logging;

namespace ImprovedPublicTransport.Util
{
    /// <summary>
    /// Simple diagnostics flags used across IPT integrations.
    /// </summary>
    /// <remarks>
    /// Both flags used to be hardcoded to <c>false</c> unconditionally, with a comment saying
    /// "consider exposing via settings later" - meaning every <c>Utils.Log(...)</c> call gated on
    /// either of these was dead code, regardless of the "Nível de Log"/"Log Level" dropdown already
    /// present in Options > Avançado (that dropdown only ever controlled CSLModsCommon's own log
    /// file, a separate mechanism from these two flags and from Utils.Log's game-log output). Wiring
    /// these to that same dropdown means it actually does what changing it always looked like it
    /// should do.
    /// </remarks>
    public static class Diagnostics
    {
        /// <summary>
        /// When true, integration transpilers may log additional details to the IPT log.
        /// </summary>
        public static bool VerboseTranspileLogs => LogManager.GetLogger().IsDebugEnabled;

        /// <summary>
        /// When true, runtime patches may emit high-frequency trace logging.
        /// </summary>
        public static bool VerboseRuntimeLogs => LogManager.GetLogger().IsVerboseEnabled;
    }
}
