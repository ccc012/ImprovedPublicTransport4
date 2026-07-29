using System.Collections.Generic;
using System.IO;
using ImprovedPublicTransport.TranslationFramework;
using ImprovedPublicTransport.Util;

namespace ImprovedPublicTransport.LanguageFormat
{
    public class PlainTextLanguageDeserializer : ILanguageDeserializer
    {
        private static readonly Dictionary<string, string> LocaleAliases = new Dictionary<string, string>
        {
            { "kr", "ko" },
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

        private static string NormalizeLocaleName(string localeName)
        {
            if (string.IsNullOrEmpty(localeName))
            {
                return localeName;
            }

            var normalized = localeName.Trim().ToLowerInvariant();
            return LocaleAliases.TryGetValue(normalized, out var canonicalLocale) ? canonicalLocale : normalized;
        }

        private static Dictionary<string, string> Load(string path)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            if (File.Exists(path))
            {
                foreach (string readAllLine in File.ReadAllLines(path))
                {
                    if (readAllLine != null)
                    {
                        string str = readAllLine.Trim();
                        if (str.Length != 0)
                        {
                            int length = str.IndexOf(' ');
                            if (length > 0)
                                dictionary.Add(str.Substring(0, length), str.Substring(length + 1).Replace("\\n", "\n"));
                        }
                    }
                }
            }
            else
                Utils.Log((object)("Localization file: " + path + " doesn't exists!"));
            return dictionary;
        }
    }
}
