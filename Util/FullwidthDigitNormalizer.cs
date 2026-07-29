namespace ImprovedPublicTransport.Util
{
    internal static class FullwidthDigitNormalizer
    {
        internal static bool ContainsFullwidthDigits(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] >= '\uFF10' && text[i] <= '\uFF19')
                {
                    return true;
                }
            }

            return false;
        }

        internal static string NormalizeDigits(string text)
        {
            if (!ContainsFullwidthDigits(text))
            {
                return text;
            }

            char[] chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] >= '\uFF10' && chars[i] <= '\uFF19')
                {
                    chars[i] = (char)('0' + (chars[i] - '\uFF10'));
                }
            }

            return new string(chars);
        }
    }
}
