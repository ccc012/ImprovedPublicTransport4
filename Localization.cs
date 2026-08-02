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

        // Resolved lookups, memoised until the language changes.
        //
        // Without this, any key that lives in the *vanilla* locale rather than our own Translations
        // files (INFO_PUBLICTRANSPORT_*, VEHICLE_PANEL_*, ...) pays the full fallback chain on every
        // single call. That is fine for one-shot UI binds, but Train Display resolves the vehicle
        // type label on every poll (up to ~10 Hz), and when the game's own locale for the active
        // language is missing that key - vanilla pt-BR is, for the whole INFO_PUBLICTRANSPORT_*
        // family - Colossal's Locale.Get writes a warning line to output_log *and still returns the
        // English string*. So the call "succeeds", the LoggedMissingKeys dedup below is never
        // reached, and the game logs the same line several times a second for as long as the
        // overlay is up. One real save produced a 2.2 MB log that way.
        private static readonly Dictionary<string, string> ResolvedCache = new Dictionary<string, string>();
        private static bool _localeHooksBound;

        private static void EnsureLocaleHooks()
        {
            if (_localeHooksBound)
            {
                return;
            }

            _localeHooksBound = true;

            // Both languages have to invalidate: the mod's own picker (Options) and the game's.
            // Getting this wrong silently breaks live language switching, which has regressed here
            // before - see the 4.2.3/4.3.1 entries in CHANGELOG.md.
            try
            {
                CSLModsCommon.Manager.LocalizationManager.ModActiveLocaleChanged += (_, __) => InvalidateCache();
            }
            catch (Exception ex)
            {
                ImprovedPublicTransport.Util.Utils.LogWarning($"Localization: could not hook mod locale change: {ex.Message}");
            }

            try
            {
                ColossalFramework.Globalization.LocaleManager.eventLocaleChanged += InvalidateCache;
            }
            catch (Exception ex)
            {
                ImprovedPublicTransport.Util.Utils.LogWarning($"Localization: could not hook game locale change: {ex.Message}");
            }
        }

        /// <summary>Drops every memoised lookup. Safe to call more often than needed.</summary>
        public static void InvalidateCache()
        {
            ResolvedCache.Clear();
            LoggedMissingKeys.Clear();
        }

        public static string Get(string translationId)
        {
            EnsureLocaleHooks();

            string cached;
            if (ResolvedCache.TryGetValue(translationId, out cached))
            {
                return cached;
            }

            // Only successful resolutions are cached. A miss must stay retryable: Get can be called
            // before the translation files have finished loading, and permanently memoising that
            // would pin the raw key on screen for the rest of the session - the exact "every string
            // shows as SETTINGS_SPEED:0" class of bug in the 4.2.3 changelog entry.
            var result = Resolve(translationId);
            if (result != null)
            {
                ResolvedCache[translationId] = result;
                return result;
            }

            if (LoggedMissingKeys.Add(translationId))
                ImprovedPublicTransport.Util.Utils.LogWarning($"Missing translation for '{translationId}'");
            return translationId;
        }

        /// <summary>Runs the full fallback chain. Returns null when nothing resolved.</summary>
        private static string Resolve(string translationId)
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

            return null;
        }
    }
}