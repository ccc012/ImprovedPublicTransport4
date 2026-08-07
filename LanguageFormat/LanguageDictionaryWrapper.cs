using System;
using System.Collections.Generic;
using ImprovedPublicTransport.TranslationFramework;

namespace ImprovedPublicTransport.LanguageFormat
{
    public class LanguageDictionaryWrapper : ILanguage
    {
        private readonly string _localeName;
        private readonly Dictionary<string, string> _dictionary;

    public LanguageDictionaryWrapper(string localeName, Dictionary<string, string> dictionary)
    {
        this._localeName = localeName ?? throw new ArgumentNullException(nameof(localeName), "LanguageDictionaryWrapper: localeName can't be null!");
        this._dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary), "LanguageDictionaryWrapper: dictionary can't be null!");
    }

        public bool HasTranslation(string id)
        {
            return _dictionary.ContainsKey(id);
        }

        public string GetTranslation(string id)
        {
            return _dictionary[id];
        }

        public string LocaleName()
        {
            return _localeName;
        }
    }
}