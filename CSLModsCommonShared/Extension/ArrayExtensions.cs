using System;

namespace CSLModsCommon.Extension;

public static class ArrayExtensions {
    public static T[] Empty<T>() => new T[0];

    public static bool IsNullOrEmpty<T>(this T[] array) => array == null || array.Length == 0;

    public static string ToJoinedString<T>(this T[] array, string separator = ",") => array == null ? string.Empty : string.Join(separator, Array.ConvertAll(array, item => item?.ToString()));
}