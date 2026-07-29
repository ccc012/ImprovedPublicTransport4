using System;
using System.Reflection;
using AlgernonCommons;
using AlgernonCommons.Notifications;

namespace ImprovedPublicTransport.UI.AlgernonCommons
{
    /// <summary>
    /// Integration for AlgernonCommons "What's new" version messages.
    /// Text is embedded directly to avoid any dependency on the CSV translation system.
    /// </summary>
    public sealed class WhatsNewIntegration : global::AlgernonCommons.ModBase
    {
        public WhatsNewIntegration()
        {
            // Explicitly call LoadSettings in constructor since no one calls OnEnabled()
            LoadSettings();
        }

        public override string BaseName => ImprovedPublicTransportMod.BaseModName;

        // Left empty on purpose: these were leftover entries from the original IPT3 fork
        // (versions 3.0.0/3.0.1, "Race Day", the pre-IPT4 mod-integration list) that never got
        // updated for a single IPT4 release, so this popup kept showing stale/wrong info no
        // matter the current version. IptModManager.GenerateChangelogs() (CSLModsCommon's own
        // Changelog dialog, in Options > Advanced) is the actively maintained changelog now -
        // keeping two parallel changelog sources in sync is exactly what let this go stale.
        public override WhatsNewMessage[] WhatsNewMessages => new WhatsNewMessage[0];

        public override void LoadSettings()
        {
            string savedVersion = ModSetting.Instance.WhatsNewLastSeenVersion ?? "0.0.0";
            UnityEngine.Debug.Log($"[IPT3] LoadSettings: saved version = {savedVersion}");
            
            // Normalize version format to always have at least 3 parts (Major.Minor.Build)
            var versionParts = savedVersion.Split('.');
            while (versionParts.Length < 3)
            {
                savedVersion += ".0";
                versionParts = savedVersion.Split('.');
            }
            
            try
            {
                // LastNotifiedVersion is INTERNAL - must use NonPublic binding flag
                PropertyInfo prop = typeof(WhatsNew).GetProperty(
                    "LastNotifiedVersion",
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (prop != null)
                {
                    MethodInfo setter = prop.GetSetMethod(nonPublic: true);
                    if (setter != null)
                    {
                        setter.Invoke(null, new object[] { new Version(savedVersion) });
                        UnityEngine.Debug.Log($"[IPT3] SUCCESS: Set LastNotifiedVersion to {savedVersion}");
                    }
                    else UnityEngine.Debug.LogError("[IPT3] ERROR: No setter on LastNotifiedVersion");
                }
                else UnityEngine.Debug.LogError("[IPT3] ERROR: LastNotifiedVersion property not found");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }

        public override void SaveSettings()
        {
            // When user dismisses "What's new", save the current assembly version as the last seen version
            // Format without revision to match WhatsNewMessages version format
            try
            {
                Version currentVersion = AssemblyUtils.CurrentVersion;
                string versionString = currentVersion != null 
                    ? $"{currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Build}"
                    : "0.0.0";
                ModSetting.Instance.WhatsNewLastSeenVersion = versionString;
                UnityEngine.Debug.Log($"[IPT3 WhatsNewIntegration.SaveSettings] Saved version: {versionString}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(new Exception($"[IPT3] SaveSettings failed: {ex.Message}", ex));
                // Fallback: use whats new message version
                if (WhatsNewMessages != null && WhatsNewMessages.Length > 0)
                {
                    ModSetting.Instance.WhatsNewLastSeenVersion = WhatsNewMessages[0].Version.ToString();
                }
            }
            CSLModsCommon.Manager.Domain.DefaultDomain.GetOrCreateManager<CSLModsCommon.Manager.SettingManager>().SaveSettings();
        }
    }
}
