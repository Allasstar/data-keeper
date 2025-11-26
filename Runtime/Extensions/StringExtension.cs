using System;
using System.Text;

namespace DataKeeper.Extensions
{
    public static class StringExtension
    {
        public static string ToTitleCase(this string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 2)
                return value;

            // Only modify needed parts
            char first = char.ToUpper(value[0]);
            string rest = value.Substring(1).ToLower(); // unavoidable alloc

            return first + rest;
        }

        public static string ToCamelCase(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            ReadOnlySpan<char> span = value.AsSpan();
            Span<char> delimiters = stackalloc[] { ' ', '-', '_' };

            StringBuilder sb = new StringBuilder(value.Length);

            bool capitalizeNext = false;
            bool isFirstWord = true;

            for (int i = 0; i < span.Length; i++)
            {
                char c = span[i];

                // Delimiter triggers capitalization
                if (delimiters.Contains(c))
                {
                    capitalizeNext = true;
                    continue;
                }

                if (isFirstWord)
                {
                    sb.Append(char.ToLower(c));
                    isFirstWord = false;
                }
                else if (capitalizeNext)
                {
                    sb.Append(char.ToUpper(c));
                    capitalizeNext = false;
                }
                else
                {
                    sb.Append(char.ToLower(c));
                }
            }

            return sb.ToString();
        }

        public static string ToUpperCaseEachWord(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            StringBuilder sb = new StringBuilder(value.Length);

            bool capitalize = true;

            foreach (char c in value)
            {
                if (char.IsWhiteSpace(c))
                {
                    capitalize = true;
                    sb.Append(c);
                }
                else
                {
                    sb.Append(capitalize ? char.ToUpper(c) : c);
                    capitalize = false;
                }
            }

            return sb.ToString();
        }

        public static string RemoveWhitespace(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            StringBuilder sb = new StringBuilder(value.Length);

            foreach (char c in value)
            {
                if (!char.IsWhiteSpace(c))
                    sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
