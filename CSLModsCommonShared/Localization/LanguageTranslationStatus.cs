namespace CSLModsCommon.Localization;

public class LanguageTranslationStatus {
    public string LanguageId { get; }
    public string LanguageName { get; }
    public string Locale { get; }

    public int TotalStrings { get; }
    public int TranslatedStrings { get; }

    public int TranslationProgress { get; }
    public int ApprovalProgress { get; }

    public LanguageTranslationStatus(string languageId, string languageName, string locale, int totalStrings, int translatedStrings, int translationProgress, int approvalProgress) {
        LanguageId = languageId;
        LanguageName = languageName;
        Locale = locale;

        TotalStrings = totalStrings;
        TranslatedStrings = translatedStrings;

        TranslationProgress = translationProgress;
        ApprovalProgress = approvalProgress;
    }

    public override string ToString() => $"{LanguageName} ({Locale}) - " +
               $"Total: {TotalStrings}, " +
               $"Translated: {TranslatedStrings}, " +
               $"Translation: {TranslationProgress}%, " +
               $"Approval: {ApprovalProgress}%";
}