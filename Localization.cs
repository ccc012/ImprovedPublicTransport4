using System;
using System.Collections.Generic;
using ImprovedPublicTransport.LanguageFormat;
using ImprovedPublicTransport.TranslationFramework;

namespace ImprovedPublicTransport
{
    public static class Localization
    {
        // Must match whichever class implements ICities.IUserMod - that's how TranslationFramework.Util
        // finds this mod's plugin folder (matching PluginManager's per-plugin IUserMod instance type).
        // Since the CSLModsCommon migration, that's Mod, not ImprovedPublicTransportMod (which is now
        // LoadingExtensionBase-only) - passing the wrong type here means the mod folder is never found
        // and NO translation ever loads for any language.
        private static readonly LocalizationManager LocalizationManager =
            new LocalizationManager(typeof(Mod), new PlainTextLanguageDeserializer());

        // Missing keys used to LogWarning (+ file IO) on every UI bind (~4 Hz). Log once per key.
        private static readonly HashSet<string> LoggedMissingKeys = new HashSet<string>();

        public static string Get(string translationId)
        {
                // First try the mod's localization manager (most reliable for mod keys)
            try
            {
                LocalizationManager.EnsureFallbackLanguageLoaded();
                var translated = LocalizationManager.GetTranslation(translationId);
                if (translated != translationId)
                    return translated;
            }
            catch (Exception ex)
            {
                if (LoggedMissingKeys.Add("err:" + translationId))
                    ImprovedPublicTransport.Util.Utils.LogWarning($"Localization.Get primary failed for '{translationId}': {ex.Message}");
            }

            // Then try Colossal's built-in locale. Colossal's Locale.Get returns "{id}:0" (not an
            // exception) when the identifier isn't a recognized vanilla-game key - that string is
            // technically different from translationId, so without this check it looks like a "found"
            // translation and gets displayed literally (e.g. "SETTINGS_SPEED:0").
            try
            {
                var c = ColossalFramework.Globalization.Locale.Get(translationId);
                if (c != translationId && c != translationId + ":0")
                    return c;
            }
            catch
            {
                // ignore — fall through
            }

            // Last-resort: try reading directly from Locale/en.txt
            try
            {
                var fileTranslated = LocalizationManager.TryGetTranslationFromLocaleFile(translationId);
                if (!string.IsNullOrEmpty(fileTranslated))
                {
                    return fileTranslated;
                }
            }
            catch
            {
                // ignore
            }

            if (LoggedMissingKeys.Add(translationId))
                ImprovedPublicTransport.Util.Utils.LogWarning($"Missing translation for '{translationId}'");
            return translationId;
        }
    }
}