using System;
using System.IO;
using System.Text;

namespace DataKeeper.Forge.Export
{
    // Tokens: {category}, {name}, {n} (1-based, zero-padded to the batch size), {seed}.
    public static class ExportNaming
    {
        public static string Format(string template, SfxCategory category, string name, int n, int count, uint seed)
        {
            if (string.IsNullOrWhiteSpace(template)) template = ExportSettings.DefaultTemplate;

            var digits = Math.Max(2, count.ToString().Length);
            var result = template
                .Replace("{category}", ToSnakeCase(category.ToString()))
                .Replace("{name}", ToSnakeCase(name))
                .Replace("{n}", n.ToString().PadLeft(digits, '0'))
                .Replace("{seed}", seed.ToString());

            return Sanitize(result);
        }

        // "UIClick" -> "ui_click", "Big Hit-2" -> "big_hit_2".
        public static string ToSnakeCase(string text)
        {
            var builder = new StringBuilder(text.Length + 4);
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c == ' ' || c == '-' || c == '_' || c == '.')
                {
                    AppendSeparator(builder);
                    continue;
                }

                if (char.IsUpper(c) && i > 0)
                {
                    var previous = text[i - 1];
                    var nextIsLower = i + 1 < text.Length && char.IsLower(text[i + 1]);
                    if (char.IsLower(previous) || char.IsDigit(previous) || (char.IsUpper(previous) && nextIsLower))
                        AppendSeparator(builder);
                }

                builder.Append(char.ToLowerInvariant(c));
            }

            if (builder.Length > 0 && builder[^1] == '_') builder.Length--;
            return builder.ToString();
        }

        private static void AppendSeparator(StringBuilder builder)
        {
            if (builder.Length > 0 && builder[^1] != '_') builder.Append('_');
        }

        private static string Sanitize(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(fileName.Length);
            foreach (var c in fileName) builder.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            return builder.ToString();
        }
    }
}
