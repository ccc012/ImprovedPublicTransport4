using ColossalFramework.PlatformServices;
using CSLModsCommon.Logging;
using UnityEngine;

namespace CSLModsCommon.Utilities;

public static class URLHelper {
    public static void OpenURL(string url) {
        if (string.IsNullOrEmpty(url)) {
            LogManager.GetLogger().Debug("Cannot open URL because URL is null or empty");
            return;
        }

        Application.OpenURL(url);
        LogManager.GetLogger().Debug($"Opened URL: {url}");
    }

    public static void OpenSteamURL(string url) {
        if (string.IsNullOrEmpty(url)) {
            LogManager.GetLogger().Debug("Cannot open URL because URL is null or empty");
            return;
        }

        PlatformService.ActivateGameOverlayToWebPage(url);
        LogManager.GetLogger().Debug($"Opened URL: {url}");
    }
}