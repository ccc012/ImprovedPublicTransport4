using System.Collections.Generic;
using System.IO;
using ImprovedPublicTransport.TranslationFramework;
using ImprovedPublicTransport.Util;

namespace ImprovedPublicTransport.LanguageFormat
{
    public class PlainTextLanguageDeserializer : ILanguageDeserializer
    {
        /// <summary>
        /// Maps alternate file stems / game locale ids onto the canonical Translations/*.txt stem.
        /// Covers Steam Workshop ELanguage codes as resolved by Cities: Skylines LocaleManager
        /// (e.g. pt-BR, zh-CN, es-MX) and legacy duplicates (kr → ko).
        /// </summary>
        private static readonly Dictionary<string, string> LocaleAliases = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            // Korean
            { "kr", "ko" },
            { "ko-kr", "ko" },
            { "koreana", "ko" },
            // Chinese
            { "zh", "zh-cn" },
            { "zh-hans", "zh-cn" },
            { "zh-hant", "zh-tw" },
            { "schinese", "zh-cn" },
            { "tchinese", "zh-tw" },
            { "zh-cn", "zh-cn" },
            { "zh-tw", "zh-tw" },
            // Portuguese
            { "pt-pt", "pt" },
            { "pt_pt", "pt" },
            { "portuguese", "pt" },
            { "pt-br", "pt-br" },
            { "pt_br", "pt-br" },
            { "brazilian", "pt-br" },
            // Spanish
            { "es-es", "es" },
            { "es_es", "es" },
            { "es-mx", "es-419" },
            { "es_mx", "es-419" },
            { "es-419", "es-419" },
            { "latam", "es-419" },
            { "spanish", "es" },
            // Scandinavian / Nordic
            { "nb", "no" },
            { "nn", "no" },
            { "no-no", "no" },
            { "nb-no", "no" },
            { "sv-se", "sv" },
            { "da-dk", "da" },
            { "fi-fi", "fi" },
            // Other Steam languages
            { "hu-hu", "hu" },
            { "ro-ro", "ro" },
            { "bg-bg", "bg" },
            { "el-gr", "el" },
            { "uk-ua", "uk" },
            { "vi-vn", "vi" },
            { "ms-my", "ms" },
            { "id-id", "id" },
            { "ja-jp", "ja" },
            { "th-th", "th" },
            { "tr-tr", "tr" },
            { "pl-pl", "pl" },
            { "nl-nl", "nl" },
            { "cs-cz", "cs" },
            { "sk-sk", "sk" },
            { "ru-ru", "ru" },
            { "de-de", "de" },
            { "fr-fr", "fr" },
            { "it-it", "it" },
            { "en-us", "en" },
            { "en-gb", "en" },
        };

        public ILanguage DeserialiseLanguage(string fileName)
        {
            var fileInfo = new FileInfo(fileName);
            var localeName = NormalizeLocaleName(fileInfo.Name.Replace(".txt", ""));
            if (Diagnostics.VerboseRuntimeLogs)
            {
                Utils.Log((object)("Loading localization file: " + fileName + ". Detected locale name: " + localeName));
            }

            return new LanguageDictionaryWrapper(localeName, Load(fileName));
        }

        /// <summary>Public so LocalizationManager can resolve game/Options locale ids the same way.</summary>
        public static string NormalizeLocaleName(string localeName)
        {
            if (string.IsNullOrEmpty(localeName))
            {
                return localeName;
            }

            var normalized = localeName.Trim().ToLowerInvariant().Replace('_', '-');
            if (LocaleAliases.TryGetValue(normalized, out var canonicalLocale))
            {
                return canonicalLocale;
            }

            // es-XX (except already mapped) → Spain Spanish unless latin-america codes
            if (normalized.StartsWith("es-", System.StringComparison.Ordinal) &&
                (normalized.Contains("mx") || normalized.Contains("419") || normalized.Contains("ar") ||
                 normalized.Contains("cl") || normalized.Contains("co") || normalized.Contains("pe")))
            {
                return "es-419";
            }

            return normalized;
        }

        private static Dictionary<string, string> Load(string path)
        {
            // OrdinalIgnoreCase: KEYS are ASCII identifiers; duplicate keys from bad packs must not crash Options.
            Dictionary<string, string> dictionary = new Dictionary<string, string>(System.StringComparer.Ordinal);
            string lastKey = null;
            if (File.Exists(path))
            {
                foreach (string readAllLine in File.ReadAllLines(path))
                {
                    if (readAllLine == null)
                    {
                        continue;
                    }

                    // Keep internal spaces of values; only trim for empty-line detection.
                    if (readAllLine.Length == 0 || readAllLine.Trim().Length == 0)
                    {
                        continue;
                    }

                    string str = readAllLine;
                    // Skip comments (our sync scripts used to append "# sync2" which was wrongly
                    // treated as a multi-line continuation of the previous value — that leaked into UI).
                    var trimmedStart = str.TrimStart();
                    if (trimmedStart.Length > 0 && trimmedStart[0] == '#')
                    {
                        continue;
                    }

                    // Continuation of a multi-line value (broken packs put real newlines instead of \n).
                    // Real keys are UPPER_SNAKE_ASCII (e.g. SETTINGS_SPEED); Finnish "Pidä" / bullets are not.
                    int length = str.IndexOf(' ');
                    string maybeKey = length > 0 ? str.Substring(0, length) : str.Trim();
                    if (!IsValidTranslationKey(maybeKey))
                    {
                        if (lastKey != null && dictionary.ContainsKey(lastKey))
                        {
                            dictionary[lastKey] = dictionary[lastKey] + "\n" + str.TrimEnd();
                        }

                        continue;
                    }

                    if (length <= 0)
                    {
                        continue;
                    }

                    string key = maybeKey;
                    string value = str.Substring(length + 1).Replace("\\n", "\n");
                    // Last wins — never throw ArgumentException (crashed Options UI on fi/hu/no/ro/sv).
                    dictionary[key] = value;
                    lastKey = key;
                }
            }
            else
            {
                Utils.Log((object)("Localization file: " + path + " doesn't exists!"));
            }

            return dictionary;
        }

        /// <summary>
        /// Translation keys are ASCII UPPER_SNAKE identifiers (MOD_DESCRIPTION, SETTINGS_*, …).
        /// Lines that fail this check are treated as multi-line value continuations, not keys.
        /// </summary>
        private static bool IsValidTranslationKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length < 2)
            {
                return false;
            }

            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                bool ok = (c >= 'A' && c <= 'Z')
                          || (c >= '0' && c <= '9')
                          || c == '_';
                if (!ok)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

