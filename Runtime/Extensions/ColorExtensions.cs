using UnityEngine;

namespace DataKeeper.Extensions
{
    public static class ColorExtensions
    {
        #region To Hex
        /// <summary>
        /// Converts Color to hex string (with #)
        /// </summary>
        public static string ToHex(this Color color, bool includeAlpha = false)
        {
            if (includeAlpha)
            {
                return $"#{ColorUtility.ToHtmlStringRGBA(color)}";
            }
            return $"#{ColorUtility.ToHtmlStringRGB(color)}";
        }

        /// <summary>
        /// Converts Color to hex string without #
        /// </summary>
        public static string ToHexRaw(this Color color, bool includeAlpha = false)
        {
            if (includeAlpha)
            {
                return ColorUtility.ToHtmlStringRGBA(color);
            }
            return ColorUtility.ToHtmlStringRGB(color);
        }
        #endregion

        #region To RGB (0-255)
        /// <summary>
        /// Converts Color to RGB values (0-255)
        /// </summary>
        public static int[] ToRGB255(this Color color)
        {
            return new int[3]
            {
                Mathf.RoundToInt(color.r * 255),
                Mathf.RoundToInt(color.g * 255),
                Mathf.RoundToInt(color.b * 255)
            };
        }

        /// <summary>
        /// Converts Color to RGBA values (0-255)
        /// </summary>
        public static float[] ToRGBA255(this Color color)
        {
            return new float[4]
            {
                Mathf.RoundToInt(color.r * 255),
                Mathf.RoundToInt(color.g * 255),
                Mathf.RoundToInt(color.b * 255),
                Mathf.RoundToInt(color.a * 255)
            };
        }

        /// <summary>
        /// Converts Color to RGB string format "rgb(255, 255, 255)"
        /// </summary>
        public static string ToRGBString(this Color color)
        {
            var rgb = color.ToRGB255();
            return $"rgb({rgb[0]}, {rgb[1]}, {rgb[2]})";
        }

        /// <summary>
        /// Converts Color to RGBA string format "rgba(255, 255, 255, 255)"
        /// </summary>
        public static string ToRGBAString(this Color color)
        {
            var rgba = color.ToRGBA255();
            return $"rgba({rgba[0]}, {rgba[1]}, {rgba[2]}, {rgba[3]})";
        }
        #endregion

        #region To RGB (0-1)
        /// <summary>
        /// Converts Color to RGB values (0-1) - essentially just the r,g,b components
        /// </summary>
        public static float[] ToRGB01(this Color color)
        {
            return new float[3] { color.r, color.g, color.b };
        }

        /// <summary>
        /// Converts Color to RGBA values (0-1) - essentially just all components
        /// </summary>
        public static float[] ToRGBA01(this Color color)
        {
            return new float[4] { color.r, color.g, color.b, color.a };
        }

        /// <summary>
        /// Converts Color to RGB string format "rgb(1.0, 1.0, 1.0)"
        /// </summary>
        public static string ToRGB01String(this Color color)
        {
            return $"rgb({color.r:F3}, {color.g:F3}, {color.b:F3})";
        }

        /// <summary>
        /// Converts Color to RGBA string format "rgba(1.0, 1.0, 1.0, 1.0)"
        /// </summary>
        public static string ToRGBA01String(this Color color)
        {
            return $"rgba({color.r:F3}, {color.g:F3}, {color.b:F3}, {color.a:F3})";
        }
        #endregion

        #region From Hex
        /// <summary>
        /// Creates Color from hex string (with or without #)
        /// </summary>
        public static Color FromHex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                return color;
            }
            Debug.LogWarning($"Failed to parse hex color: {hex}");
            return Color.white;
        }

        /// <summary>
        /// Tries to create Color from hex string
        /// </summary>
        public static bool TryFromHex(string hex, out Color color)
        {
            return ColorUtility.TryParseHtmlString(hex, out color);
        }
        #endregion

        #region From RGB (0-255)
        /// <summary>
        /// Creates Color from RGB values (0-255)
        /// </summary>
        public static Color FromRGB255(int r, int g, int b, int a = 255)
        {
            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }

        /// <summary>
        /// Creates Color from RGB Vector3Int (0-255)
        /// </summary>
        public static Color FromRGB255(Vector3Int rgb, int alpha = 255)
        {
            return FromRGB255(rgb.x, rgb.y, rgb.z, alpha);
        }

        /// <summary>
        /// Creates Color from RGBA Vector4 (0-255)
        /// </summary>
        public static Color FromRGBA255(Vector4 rgba)
        {
            return FromRGB255((int)rgba.x, (int)rgba.y, (int)rgba.z, (int)rgba.w);
        }
        #endregion

        #region From RGB (0-1)
        /// <summary>
        /// Creates Color from RGB values (0-1)
        /// </summary>
        public static Color FromRGB01(float r, float g, float b, float a = 1f)
        {
            return new Color(r, g, b, a);
        }

        /// <summary>
        /// Creates Color from RGB Vector3 (0-1)
        /// </summary>
        public static Color FromRGB01(Vector3 rgb, float alpha = 1f)
        {
            return new Color(rgb.x, rgb.y, rgb.z, alpha);
        }

        /// <summary>
        /// Creates Color from RGBA Vector4 (0-1)
        /// </summary>
        public static Color FromRGBA01(Vector4 rgba)
        {
            return new Color(rgba.x, rgba.y, rgba.z, rgba.w);
        }
        #endregion

        #region HSV Conversion
        /// <summary>
        /// Converts Color to HSV values (H: 0-360, S: 0-1, V: 0-1)
        /// </summary>
        public static Vector3 ToHSV(this Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            return new Vector3(h * 360f, s, v);
        }

        /// <summary>
        /// Creates Color from HSV values (H: 0-360, S: 0-1, V: 0-1)
        /// </summary>
        public static Color FromHSV(float h, float s, float v, float a = 1f)
        {
            var color = Color.HSVToRGB(h / 360f, s, v);
            color.a = a;
            return color;
        }

        /// <summary>
        /// Creates Color from HSV Vector3 (H: 0-360, S: 0-1, V: 0-1)
        /// </summary>
        public static Color FromHSV(Vector3 hsv, float alpha = 1f)
        {
            return FromHSV(hsv.x, hsv.y, hsv.z, alpha);
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Gets the brightness/luminance of the color using standard formula
        /// </summary>
        public static float GetBrightness(this Color color)
        {
            return (color.r * 0.299f + color.g * 0.587f + color.b * 0.114f);
        }

        /// <summary>
        /// Gets the perceived brightness using sRGB gamma correction
        /// </summary>
        public static float GetPerceivedBrightness(this Color color)
        {
            float r = color.r <= 0.03928f ? color.r / 12.92f : Mathf.Pow((color.r + 0.055f) / 1.055f, 2.4f);
            float g = color.g <= 0.03928f ? color.g / 12.92f : Mathf.Pow((color.g + 0.055f) / 1.055f, 2.4f);
            float b = color.b <= 0.03928f ? color.b / 12.92f : Mathf.Pow((color.b + 0.055f) / 1.055f, 2.4f);
            return (r * 0.2126f + g * 0.7152f + b * 0.0722f);
        }

        /// <summary>
        /// Returns whether the color is considered "dark" (brightness < 0.5)
        /// </summary>
        public static bool IsDark(this Color color)
        {
            return color.GetBrightness() < 0.5f;
        }

        /// <summary>
        /// Returns whether the color is considered "light" (brightness >= 0.5)
        /// </summary>
        public static bool IsLight(this Color color)
        {
            return color.GetBrightness() >= 0.5f;
        }

        /// <summary>
        /// Returns the complementary color (inverted RGB)
        /// </summary>
        public static Color GetComplement(this Color color)
        {
            return new Color(1f - color.r, 1f - color.g, 1f - color.b, color.a);
        }

        /// <summary>
        /// Returns a contrasting color (black or white) based on brightness
        /// </summary>
        public static Color GetContrastingColor(this Color color)
        {
            return color.IsDark() ? Color.white : Color.black;
        }

        /// <summary>
        /// Adjusts the saturation of the color
        /// </summary>
        public static Color AdjustSaturation(this Color color, float saturationMultiplier)
        {
            var hsv = color.ToHSV();
            hsv.y = Mathf.Clamp01(hsv.y * saturationMultiplier);
            return FromHSV(hsv, color.a);
        }

        /// <summary>
        /// Adjusts the brightness/value of the color
        /// </summary>
        public static Color AdjustBrightness(this Color color, float brightnessMultiplier)
        {
            var hsv = color.ToHSV();
            hsv.z = Mathf.Clamp01(hsv.z * brightnessMultiplier);
            return FromHSV(hsv, color.a);
        }

        /// <summary>
        /// Shifts the hue of the color by the specified degrees
        /// </summary>
        public static Color ShiftHue(this Color color, float hueDegrees)
        {
            var hsv = color.ToHSV();
            hsv.x = (hsv.x + hueDegrees) % 360f;
            if (hsv.x < 0) hsv.x += 360f;
            return FromHSV(hsv, color.a);
        }

        /// <summary>
        /// Returns the grayscale version of the color
        /// </summary>
        public static Color ToGrayscale(this Color color)
        {
            float gray = color.GetBrightness();
            return new Color(gray, gray, gray, color.a);
        }

        /// <summary>
        /// Interpolates between two colors using HSV color space for more natural results
        /// </summary>
        public static Color LerpHSV(Color colorA, Color colorB, float t)
        {
            var hsvA = colorA.ToHSV();
            var hsvB = colorB.ToHSV();

            // Handle hue interpolation properly (shortest path around color wheel)
            float hueA = hsvA.x;
            float hueB = hsvB.x;
            float hueDiff = Mathf.Abs(hueB - hueA);

            if (hueDiff > 180f)
            {
                if (hueB > hueA)
                    hueA += 360f;
                else
                    hueB += 360f;
            }

            float hue = Mathf.Lerp(hueA, hueB, t) % 360f;
            if (hue < 0) hue += 360f;

            float saturation = Mathf.Lerp(hsvA.y, hsvB.y, t);
            float value = Mathf.Lerp(hsvA.z, hsvB.z, t);
            float alpha = Mathf.Lerp(colorA.a, colorB.a, t);

            return FromHSV(hue, saturation, value, alpha);
        }

        /// <summary>
        /// Creates an analogous color by shifting hue by specified degrees (typically 30-60)
        /// </summary>
        public static Color GetAnalogous(this Color color, float hueDegrees = 30f)
        {
            return color.ShiftHue(hueDegrees);
        }

        /// <summary>
        /// Creates a triadic color by shifting hue by 120 degrees
        /// </summary>
        public static Color GetTriadic(this Color color)
        {
            return color.ShiftHue(120f);
        }

        /// <summary>
        /// Creates a tetradic (square) color by shifting hue by 90 degrees
        /// </summary>
        public static Color GetTetradic(this Color color)
        {
            return color.ShiftHue(90f);
        }

        /// <summary>
        /// Creates a split-complementary color by shifting hue by specified degrees from complement (typically 150 or 210)
        /// </summary>
        public static Color GetSplitComplementary(this Color color, bool useFirst = true)
        {
            float hueShift = useFirst ? 150f : 210f;
            return color.ShiftHue(hueShift);
        }

        /// <summary>
        /// Generates a monochromatic palette by varying brightness
        /// </summary>
        public static Color[] GetMonochromaticPalette(this Color color, int count = 5)
        {
            Color[] palette = new Color[count];
            var hsv = color.ToHSV();

            for (int i = 0; i < count; i++)
            {
                float value = Mathf.Lerp(0.2f, 1f, i / (float)(count - 1));
                palette[i] = FromHSV(hsv.x, hsv.y, value, color.a);
            }

            return palette;
        }

        /// <summary>
        /// Generates a complementary color palette
        /// </summary>
        public static Color[] GetComplementaryPalette(this Color color)
        {
            return new Color[] { color, color.GetComplement() };
        }

        /// <summary>
        /// Generates an analogous color palette
        /// </summary>
        public static Color[] GetAnalogousPalette(this Color color, int count = 3, float hueSpread = 30f)
        {
            Color[] palette = new Color[count];
            float startHue = -(count - 1) * hueSpread * 0.5f;

            for (int i = 0; i < count; i++)
            {
                palette[i] = color.ShiftHue(startHue + i * hueSpread);
            }

            return palette;
        }

        /// <summary>
        /// Generates a triadic color palette
        /// </summary>
        public static Color[] GetTriadicPalette(this Color color)
        {
            return new Color[] 
            { 
                color, 
                color.ShiftHue(120f), 
                color.ShiftHue(240f) 
            };
        }

        /// <summary>
        /// Generates a tetradic (square) color palette
        /// </summary>
        public static Color[] GetTetradicPalette(this Color color)
        {
            return new Color[] 
            { 
                color, 
                color.ShiftHue(90f), 
                color.ShiftHue(180f), 
                color.ShiftHue(270f) 
            };
        }

        /// <summary>
        /// Calculates the contrast ratio between two colors (WCAG standard)
        /// </summary>
        public static float GetContrastRatio(this Color color, Color other)
        {
            float l1 = color.GetPerceivedBrightness() + 0.05f;
            float l2 = other.GetPerceivedBrightness() + 0.05f;
            return Mathf.Max(l1, l2) / Mathf.Min(l1, l2);
        }

        /// <summary>
        /// Checks if the contrast ratio meets WCAG AA standard (4.5:1 for normal text)
        /// </summary>
        public static bool HasGoodContrast(this Color color, Color other, bool largeText = false)
        {
            float ratio = color.GetContrastRatio(other);
            return ratio >= (largeText ? 3f : 4.5f);
        }

        /// <summary>
        /// Checks if the contrast ratio meets WCAG AAA standard (7:1 for normal text)
        /// </summary>
        public static bool HasExcellentContrast(this Color color, Color other, bool largeText = false)
        {
            float ratio = color.GetContrastRatio(other);
            return ratio >= (largeText ? 4.5f : 7f);
        }

        /// <summary>
        /// Finds the best contrasting color from a palette for accessibility
        /// </summary>
        public static Color GetBestContrastingColor(this Color color, Color[] palette, bool largeText = false)
        {
            Color best = palette[0];
            float bestRatio = color.GetContrastRatio(best);

            for (int i = 1; i < palette.Length; i++)
            {
                float ratio = color.GetContrastRatio(palette[i]);
                if (ratio > bestRatio)
                {
                    best = palette[i];
                    bestRatio = ratio;
                }
            }

            return best;
        }

        /// <summary>
        /// Adjusts color brightness to achieve good contrast against the specified background
        /// </summary>
        public static Color EnsureContrast(this Color color, Color background, bool largeText = false)
        {
            float targetRatio = largeText ? 3f : 4.5f;
            Color adjusted = color;
            
            // Try making it lighter first
            for (float brightness = 1f; brightness <= 2f; brightness += 0.1f)
            {
                adjusted = color.AdjustBrightness(brightness);
                if (adjusted.GetContrastRatio(background) >= targetRatio)
                    return adjusted;
            }

            // If that doesn't work, try making it darker
            for (float brightness = 0.9f; brightness >= 0.1f; brightness -= 0.1f)
            {
                adjusted = color.AdjustBrightness(brightness);
                if (adjusted.GetContrastRatio(background) >= targetRatio)
                    return adjusted;
            }

            // Fallback to high contrast color
            return background.IsDark() ? Color.white : Color.black;
        }

        /// <summary>
        /// Creates a tinted version of the color (mix with white)
        /// </summary>
        public static Color Tint(this Color color, float amount)
        {
            return Color.Lerp(color, Color.white, Mathf.Clamp01(amount));
        }

        /// <summary>
        /// Creates a shaded version of the color (mix with black)
        /// </summary>
        public static Color Shade(this Color color, float amount)
        {
            return Color.Lerp(color, Color.black, Mathf.Clamp01(amount));
        }

        /// <summary>
        /// Creates a toned version of the color (mix with gray)
        /// </summary>
        public static Color Tone(this Color color, float amount)
        {
            return Color.Lerp(color, Color.gray, Mathf.Clamp01(amount));
        }

        /// <summary>
        /// Returns the color temperature in Kelvin (approximate for common colors)
        /// 2000 - Very warm
        /// 3000 - Warm
        /// 4000 - Neutral warm
        /// 5500 - Daylight
        /// 7000 - Cool
        /// 9000 - Very cool
        /// </summary>
        public static float GetColorTemperature(this Color color)
        {
            // Simple approximation based on red/blue ratio
            float ratio = color.r / Mathf.Max(color.b, 0.001f);
            
            // Rough mapping to color temperature
            if (ratio > 2f) return 2000f;
            if (ratio > 1.5f) return 3000f;
            if (ratio > 1.2f) return 4000f;
            if (ratio > 0.8f) return 5500f;
            if (ratio > 0.6f) return 7000f;
            return 9000f;
        }

        /// <summary>
        /// Determines if the color is warm (reddish/yellowish) or cool (bluish/greenish)
        /// </summary>
        public static bool IsWarmColor(this Color color)
        {
            return (color.r + color.g * 0.5f) > (color.b + color.g * 0.5f);
        }
        #endregion
    }
}