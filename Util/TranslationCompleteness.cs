using System;
using System.Collections.Generic;
using System.IO;

namespace ImprovedPublicTransport.Util
{
    /// <summary>
    /// Computes how much of a Translations/&lt;locale&gt;.txt file actually differs from en.txt,
    /// key by key, instead of a hand-typed number that goes stale every time keys are added. A key
    /// whose value is byte-identical to English is treated as untranslated - that is exactly how
    /// missing translations get filled in this project (temporary English placeholder until a real
    /// translation lands, see INSTRUCOES_PROXIMOS_PASSOS.md), so "identical to English" is a
    /// reliable proxy for "not done yet". English itself always reports 100%.
    /// </summary>
    public static class TranslationCompleteness
    {
        private static Dictionary<string, string> _englishCache;
        private static string _translationsPath;
        private static readonly Dictionary<string, Result> _localeCache = new Dictionary<string, Result>(StringComparer.OrdinalIgnoreCase);

        public readonly struct Result
        {
            public readonly int Total;
            public readonly int Translated;
            public readonly int Percent;

            public Result(int total, int translated)
            {
                Total = total;
                Translated = translated;
                Percent = total <= 0 ? 0 : (int)Math.Round(translated * 100.0 / total);
            }
        }

        /// <param name="localeStem">Translations/*.txt file stem, e.g. "de", "zh-cn", "pt-br".</param>
        public static Result ForLocale(string localeStem)
        {
            if (string.IsNullOrEmpty(localeStem))
            {
                return default;
            }

            // Translation files do not change at runtime - cache the per-locale result so the
            // Options panel rebuild does not re-read the .txt on every open.
            if (_localeCache.TryGetValue(localeStem, out var cached))
            {
                return cached;
            }

            var result = Compute(localeStem);
            _localeCache[localeStem] = result;
            return result;
        }

        private static Result Compute(string localeStem)
        {
            var english = LoadEnglish();
            if (english == null || english.Count == 0)
            {
                return default;
            }

            if (string.Equals(localeStem, "en", StringComparison.OrdinalIgnoreCase))
            {
                return new Result(english.Count, english.Count);
            }

            var target = Load(Path.Combine(_translationsPath, localeStem + ".txt"));
            if (target == null || target.Count == 0)
            {
                return new Result(english.Count, 0);
            }

            int translated = 0;
            foreach (var pair in english)
            {
                if (target.TryGetValue(pair.Key, out var value) &&
                    !string.Equals(value, pair.Value, StringComparison.Ordinal))
                {
                    translated++;
                }
            }

            return new Result(english.Count, translated);
        }

        private static Dictionary<string, string> LoadEnglish()
        {
            if (_englishCache != null)
            {
                return _englishCache;
            }

            var basePath = Utils.AssemblyPath;
            if (string.IsNullOrEmpty(basePath))
            {
                return null;
            }

            _translationsPath = Path.Combine(basePath, "Translations");
            _englishCache = Load(Path.Combine(_translationsPath, "en.txt"));
            return _englishCache;
        }

        /// <summary>
        /// Same key/value parsing rules as PlainTextLanguageDeserializer.Load (space-separated
        /// KEY value, "\n" literal for line breaks, "#"-prefixed comment lines skipped) - kept
        /// independent rather than shared so this stays a read-only, side-effect-free query that
        /// can never affect which language actually gets loaded into the game.
        /// </summary>
        private static Dictionary<string, string> Load(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var raw in File.ReadAllLines(path))
            {
                if (string.IsNullOrEmpty(raw))
                {
                    continue;
                }

                var trimmedStart = raw.TrimStart();
                if (trimmedStart.Length == 0 || trimmedStart[0] == '#')
                {
                    continue;
                }

                int idx = raw.IndexOf(' ');
                if (idx <= 0)
                {
                    continue;
                }

                var key = raw.Substring(0, idx);
                if (!IsValidTranslationKey(key))
                {
                    continue;
                }

                dictionary[key] = raw.Substring(idx + 1).Replace("\\n", "\n");
            }

            return dictionary;
        }

        private static bool IsValidTranslationKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length < 2)
            {
                return false;
            }

            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                bool ok = (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
                if (!ok)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
