using System;
using System.Linq;

namespace DataKeeper.Extensions
{
    public static class StringExtension
    {
        public static string ToTitleCase(this string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 2)
                return value;
            
            value = value.ToLower();
            return char.ToUpper(value[0]) + value.Substring(1);
        }
        
        public static string ToCamelCase(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Split the string into words
            var words = value.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
                return value;

            // Capitalize the first letter of each word, except the first word
            for (int i = 1; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }

            // Concatenate the words
            string result = string.Concat(words);

            // Make sure the first letter is lowercase
            return char.ToLower(result[0]) + result.Substring(1);
        }
        
        public static string ToUpperCaseEachWord(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Split the input string into words
            string[] words = value.Split(' ');

            // Capitalize the first letter of each word
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    // Capitalize the first character of the word
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                }
            }

            // Join the words back together with a space separator
            return string.Join(" ", words);
        }
        
        public static string RemoveWhitespace(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }
        
        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }
        
        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }
    }
}
