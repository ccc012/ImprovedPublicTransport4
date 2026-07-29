namespace CSLModsCommon.Extension; 
public static class StringExtensions {
    public static string SafeTrim(this string str) => str == null ? string.Empty : str.Trim();

    public static string SafeToLower(this string str) => str == null ? string.Empty : str.ToLower();

    public static string SafeToUpper(this string str) => str == null ? string.Empty : str.ToUpper();
}