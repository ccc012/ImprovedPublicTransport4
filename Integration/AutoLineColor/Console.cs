using ColossalFramework.Plugins;
using System;
using ImprovedPublicTransport.Util;

namespace AutoLineColor
{
    /// <summary>
    /// Logging wrapper for AutoLineColor integration.
    /// Redirects all logging to IPT3's centralized logging system via ImprovedPublicTransport.Util.Utils.
    /// </summary>
    public class Console
    {
        private static Console _instance;

        private Console()
        {
#if DEBUG
            Debug = true;
#endif
        }

        public static Console Instance => _instance ?? (_instance = new Console());

        public bool Debug { get; private set; }

        public void SetDebug(bool shouldDebug)
        {
            Debug = shouldDebug;
        }

        public void Message(string p)
        {
            if (!Debug) return;
            var msg = FormatMessage(p, "Message");
            Utils.Log($"[AutoLineColor] {msg}");
        }

        public void Warning(string p)
        {
            if (!Debug) return;
            var msg = FormatMessage(p, "Warning");
            Utils.LogWarning($"[AutoLineColor] {msg}");
        }

        public void Error(string p)
        {
            // Unlike Message/Warning, errors are not debug-gated - a real failure should always
            // surface, or it silently hides from players/support who never enabled debug logging.
            var msg = FormatMessage(p, "Error");
            Utils.LogError($"[AutoLineColor] {msg}");
        }

        private static string FormatMessage(string msg, string type)
        {
            try
            {
                return $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} ({type}) {msg}";
            }
            catch
            {
                return msg;
            }
        }
    }
}
