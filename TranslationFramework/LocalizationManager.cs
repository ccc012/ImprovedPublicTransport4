using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColossalFramework.Globalization;

namespace ImprovedPublicTransport.TranslationFramework
{
    /// <summary>
    /// Handles localisation for a mod.
    /// </summary>
    public class LocalizationManager
    {
        protected List<ILanguage> _languages = new List<ILanguage>();
        protected ILanguage _currentLanguage = null;
        protected bool _languagesLoaded = false;
        protected bool _loadLanguageAutomatically = true;
        private string fallbackLanguage;
        private ILanguageDeserializer languageDeserializer;
        private Type modType;

        // Dedupes the "doesn't contain a suitable translation" warning below, keyed by
        // "<language>|<id>" so a real language switch gets its own first-time warning instead of
        // being silently swallowed by an id already logged for a different language. Without this,
        // every caller that (correctly) uses a vanilla-only key through this framework - e.g.
        // Localization.Get("INFO_PUBLICTRANSPORT_BUS"), which always falls through to Colossal's
        // own Locale.Get afterwards and works fine - re-logs this warning every time Localization's
        // own resolved-value cache gets invalidated (any locale change event), not just once. One
        // player's session produced a 2.2 MB output_log from exactly this, initially misdiagnosed
        // as unrelated vanilla/engine noise before tracing it back here.
        private readonly HashSet<string> _loggedMissingTranslations = new HashSet<string>();

        public LocalizationManager(Type modType, ILanguageDeserializer languageDeserializer, bool loadLanguageAutomatically = true,
            string fallbackLanguage = "en")
        {
            // Was `?? new DefaultLanguageDeserializer()` - a legacy XML-format deserializer from
            // before Translations/*.txt existed, with zero real callers (the only real caller,
            // Localization.cs, always passes PlainTextLanguageDeserializer explicitly). Removed
            // along with DefaultLanguage/DefaultLanguageDeserializer/TranslatableAttribute - fail
            // fast instead of silently falling back to a deserializer that can't read our own file
            // format, if a future caller ever forgets to pass one.
            this.languageDeserializer = languageDeserializer ?? throw new ArgumentNullException(nameof(languageDeserializer));
            this._loadLanguageAutomatically = loadLanguageAutomatically;
            this.fallbackLanguage = fallbackLanguage;
            this.modType = modType;
            LocaleManager.eventLocaleChanged += SetCurrentLanguage;
            // The Options panel has its own language selector (CSLModsCommon), separate from the
            // game's language. Without this, picking a language there only re-translated the
            // framework's own strings (Version/Changelog/...) while every string of ours stayed
            // in the game's language - which reads as "half the panel didn't translate".
            CSLModsCommon.Manager.LocalizationManager.ModActiveLocaleChanged += OnModActiveLocaleChanged;
        }

        public void Deinit()
        {
            LocaleManager.eventLocaleChanged -= SetCurrentLanguage;
            CSLModsCommon.Manager.LocalizationManager.ModActiveLocaleChanged -= OnModActiveLocaleChanged;
        }

        private void OnModActiveLocaleChanged(string localeId, CSLModsCommon.Manager.LocalizationManager manager) => SetCurrentLanguage();

        private void SetCurrentLanguage()
        {
            if (_languages == null || _languages.Count == 0)
            {
                return;
            }

            // Mod's own language selection wins; fall back to the game's language, then to English.
            _currentLanguage = FindLanguage(GetModSelectedLocale())
                               ?? FindLanguage(LocaleManager.exists ? LocaleManager.instance.language : null)
                               ?? FindLanguage(fallbackLanguage);
        }

        /// <summary>
        /// Gets the locale the user picked in this mod's Options panel, or null when set to
        /// "use game language" (or when the framework isn't ready yet).
        /// </summary>
        private static string GetModSelectedLocale()
        {
            try
            {
                var settingLocaleId = ModSetting.Instance?.LocaleId;
                if (string.IsNullOrEmpty(settingLocaleId) ||
                    settingLocaleId == CSLModsCommon.Manager.LocalizationManager.UseGameLanguage)
                {
                    return null;
                }

                return settingLocaleId;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Resolves a locale id to a loaded language file. Handles the two naming schemes in play:
        /// CSLModsCommon uses long ids ("de-DE", "zh-TW"), our translation files use either the
        /// short form ("de.txt") or the lowercased long form ("zh-tw.txt").
        /// </summary>
        private ILanguage FindLanguage(string localeId)
        {
            if (string.IsNullOrEmpty(localeId) || _languages == null)
            {
                return null;
            }

            // Canonicalise Steam/game codes (pt-BR, zh-CN, koreana, …) onto Translations/*.txt stems.
            var canonical = LanguageFormat.PlainTextLanguageDeserializer.NormalizeLocaleName(localeId);
            var match = _languages.Find(l => string.Equals(l.LocaleName(), canonical, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                return match;
            }

            var lower = localeId.ToLowerInvariant().Replace('_', '-');
            match = _languages.Find(l => string.Equals(l.LocaleName(), lower, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                return match;
            }

            int dash = lower.IndexOf('-');
            if (dash > 0)
            {
                var shortId = lower.Substring(0, dash);
                var shortCanonical = LanguageFormat.PlainTextLanguageDeserializer.NormalizeLocaleName(shortId);
                return _languages.Find(l =>
                    string.Equals(l.LocaleName(), shortCanonical, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(l.LocaleName(), shortId, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        /// <summary>
        /// Loads all languages up if not already loaded.
        /// </summary>
        public void LoadLanguages()
        {
            if (!_languagesLoaded && _loadLanguageAutomatically)
            {
                _languagesLoaded = true;
                RefreshLanguages();
                SetCurrentLanguage();
            }
        }

        /// <summary>
        /// Forces a reload of the languages, even if they're already loaded
        /// </summary>
        public void RefreshLanguages()
        {
            _languages.Clear();

            string basePath = Util.AssemblyPath(modType);

            // Only log debug info if verbose logging enabled
            bool verboseLogging = ImprovedPublicTransport.Util.Diagnostics.VerboseTranspileLogs;
            if (verboseLogging)
            {
                try
                {
                    ImprovedPublicTransport.Util.Utils.LogWarning($"LocalizationManager: basePath='{basePath}'");
                }
                catch { }
            }

            if (basePath != "")
            {
                string languagePath = basePath + System.IO.Path.DirectorySeparatorChar + "Translations";
                if (verboseLogging)
                {
                    try
                    {
                        ImprovedPublicTransport.Util.Utils.LogWarning($"LocalizationManager: languagePath='{languagePath}' Exists={System.IO.Directory.Exists(languagePath)}");
                    }
                    catch { }
                }

                if (Directory.Exists(languagePath))
                {
                    // TopDirectoryOnly: full IPT packs live at Translations/*.txt. Nested leftovers
                    // (e.g. stale Translations/Core/*.txt from older deploys) must not win first and
                    // shadow the root packs — GetFiles(AllDirectories)+OrderBy path loaded Core first.
                    string[] languageFiles = Directory.GetFiles(languagePath, "*.txt", SearchOption.TopDirectoryOnly)
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    var loadedLocales = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (verboseLogging)
                    {
                        try
                        {
                            ImprovedPublicTransport.Util.Utils.LogWarning($"LocalizationManager: found {languageFiles.Length} language files");
                        }
                        catch { }
                    }

                    foreach (string languageFile in languageFiles)
                    {
                        ILanguage loadedLanguage = null;
                        try
                        {
                            loadedLanguage = languageDeserializer.DeserialiseLanguage(languageFile);
                            if (verboseLogging)
                            {
                                ImprovedPublicTransport.Util.Utils.LogWarning($"LocalizationManager: Loading localization file: {languageFile}. Detected locale name: {loadedLanguage?.LocaleName()}");
                            }
                        }
                        catch (Exception e)
                        {
                            UnityEngine.Debug.LogError(
                                "Error happened when deserializing language file " + languageFile);
                            UnityEngine.Debug.LogException(e);
                        }
                        if (loadedLanguage != null)
                        {
                            if (!loadedLocales.Add(loadedLanguage.LocaleName()))
                            {
                                UnityEngine.Debug.LogWarning("Skipping duplicate localisation file for locale \"" + loadedLanguage.LocaleName() + "\": " + languageFile);
                                continue;
                            }

                            _languages.Add(loadedLanguage);
                        }
                    }

                    if (verboseLogging)
                    {
                        try
                        {
                            ImprovedPublicTransport.Util.Utils.LogWarning($"LocalizationManager: loaded {_languages.Count} languages");
                        }
                        catch { }
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Can't load any localisation files");
                }
            }
        }

        /// <summary>
        /// Ensure that a fallback language is available and selected if no current language has been set yet.
        /// This is useful when localization is requested early in the UI lifecycle before Colossal's LocaleManager
        /// has become available; it forces the manager to load language files and select the fallback (usually 'en').
        /// </summary>
        public void EnsureFallbackLanguageLoaded()
        {
            // Only load once - _languagesLoaded flag prevents repeated reloads
            if (!_languagesLoaded)
            {
                LoadLanguages();
            }

            // If no current language has been selected yet, choose the fallback language if present,
            // otherwise pick the first available language.
            if (_currentLanguage == null && _languages != null && _languages.Count > 0)
            {
                _currentLanguage = _languages.Find(l => l.LocaleName() == fallbackLanguage) ?? _languages[0];
            }
        }

        /// <summary>
        /// Returns whether you can translate into a specific translation ID
        /// </summary>
        /// <param name="translationId">The ID of the translation to check</param>
        /// <returns>Whether a translation into this ID is possible</returns>
        private bool HasTranslation(string translationId)
        {
            LoadLanguages();

            if (translationId == null)
            {
                return true;
            }

            return _currentLanguage != null && _currentLanguage.HasTranslation(translationId);
        }

        /// <summary>
        /// Gets a translation for a specific translation ID
        /// </summary>
        /// <param name="translationId">The ID to return the translation for</param>
        /// <returns>A translation of the translationId</returns>
        public string GetTranslation(string translationId)
        {
            LoadLanguages();

            if (translationId == null)
            {
                return "null";
            }

            string translatedText = translationId;

            if (_currentLanguage != null)
            {
                if (HasTranslation(translationId))
                {
                    translatedText = _currentLanguage.GetTranslation(translationId);
                }
                else if (_loggedMissingTranslations.Add(_currentLanguage.LocaleName() + "|" + translationId))
                {
                    UnityEngine.Debug.LogWarning("Returned translation for language \"" + _currentLanguage.LocaleName() + "\" doesn't contain a suitable translation for \"" + translationId + "\"");
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("Can't get a translation for \"" + translationId + "\" as there is not a language defined");
            }

            return translatedText;
        }

        /// <summary>
        /// Try to read a translation directly from a Locale file (e.g., Locale/en.txt).
        /// This is a last-resort fallback for cases where the language manager hasn't
        /// selected a language or parsing failed earlier.
        /// </summary>
        public string TryGetTranslationFromLocaleFile(string translationId)
        {
            try
            {
                string basePath = Util.AssemblyPath(modType);
                if (string.IsNullOrEmpty(basePath)) return null;
                string enPath = System.IO.Path.Combine(basePath, "Translations");
                enPath = System.IO.Path.Combine(enPath, "en.txt");
                if (!System.IO.File.Exists(enPath)) return null;
                foreach (string raw in System.IO.File.ReadAllLines(enPath))
                {
                    if (raw == null) continue;
                    var str = raw.Trim();
                    if (str.Length == 0) continue;
                    int idx = str.IndexOf(' ');
                    if (idx <= 0) continue;
                    var key = str.Substring(0, idx);
                    if (key == translationId)
                    {
                        var val = str.Substring(idx + 1).Replace("\\n", "\n");
                        return val;
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("LocalizationManager: fallback read failed: " + e.Message);
            }
            return null;
        }
    }
}
