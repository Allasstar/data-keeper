using System.Globalization;
using UnityEngine;

namespace DataKeeper.Extensions
{
    public static class ParseExtension
    {
        public static Rect FromString(this Rect vector, string input)
        {
            input = input.Trim('(', ')');
            input = input.Replace("x:", "");
            input = input.Replace("y:", "");
            input = input.Replace("width:", "");
            input = input.Replace("height:", "");
            string[] values = input.Split(',');

            if (values.Length != 4)
            {
                throw new System.ArgumentException("Input string must be in the format '(x:x, y:y, width:z, height:w)'");
            }

            CultureInfo culture = CultureInfo.InvariantCulture;

            var x = float.Parse(values[0].Trim(), culture);
            var y = float.Parse(values[1].Trim(), culture);
            var z = float.Parse(values[2].Trim(), culture);
            var w = float.Parse(values[3].Trim(), culture);
            
            return new Rect(x, y, z, w);
        }
        
        public static Color FromString(this Color vector, string input)
        {
            if (!input.StartsWith("RGBA"))
            {
                throw new System.ArgumentException("Input string must be in the format 'RGBA(r, g, b, a)'");
            }

            input = input.Replace("RGBA", "");
            input = input.Trim('(', ')');
            string[] values = input.Split(',');

            if (values.Length != 4)
            {
                throw new System.ArgumentException("Input string must be in the format 'RGBA(r, g, b, a)'");
            }

            CultureInfo culture = CultureInfo.InvariantCulture;

            var r = float.Parse(values[0].Trim(), culture);
            var g = float.Parse(values[1].Trim(), culture);
            var b = float.Parse(values[2].Trim(), culture);
            var a = float.Parse(values[3].Trim(), culture);
            
            return new Color(r, g, b, a);
        }
        
        public static Vector4 FromString(this Vector4 vector, string input)
        {
            input = input.Trim('(', ')');
            string[] values = input.Split(',');

            if (values.Length != 4)
            {
                throw new System.ArgumentException("Input string must be in the format '(x, y, z, w)'");
            }

            CultureInfo culture = CultureInfo.InvariantCulture;

            var x = float.Parse(values[0].Trim(), culture);
            var y = float.Parse(values[1].Trim(), culture);
            var z = float.Parse(values[2].Trim(), culture);
            var w = float.Parse(values[3].Trim(), culture);
            
            return new Vector4(x, y, z, w);
        }
        
        public static Vector3 FromString(this Vector3 vector, string input)
        {
            input = input.Trim('(', ')');
            string[] values = input.Split(',');

            if (values.Length != 3)
            {
                throw new System.ArgumentException("Input string must be in the format '(x, y, z)'");
            }
            
            CultureInfo culture = CultureInfo.InvariantCulture;

            var x = float.Parse(values[0].Trim(), culture);
            var y = float.Parse(values[1].Trim(), culture);
            var z = float.Parse(values[2].Trim(), culture);
            
            return new Vector3(x, y, z);
        }
        
        public static Vector2 FromString(this Vector2 vector, string input)
        {
            input = input.Trim('(', ')');
            string[] values = input.Split(',');

            if (values.Length != 2)
            {
                throw new System.ArgumentException("Input string must be in the format '(x, y)'");
            }

            CultureInfo culture = CultureInfo.InvariantCulture;
            
            var x = float.Parse(values[0].Trim(), culture);
            var y = float.Parse(values[1].Trim(), culture);

            return new Vector2(x, y);
        }
    }
}