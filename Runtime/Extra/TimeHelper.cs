using System;

namespace DataKeeper.Extra
{
    public static class TimeHelper
    {
        public const int DAY_1_IN_SEC = 86400;
        public const int MIN_30_IN_SEC = 1800;
    
        public static int CurrentDayInSec(int addDays = 0)
        {
            var today = DateTime.Today.AddDays(addDays);
            var sec = new DateTimeOffset(today).ToUnixTimeSeconds();
            return (int)sec;
        }
    
        public static int CurrentTimeInSec(int addSeconds = 0)
        {
            return (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + addSeconds;
        }

        public static string ToMSFormat(int seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            return $"{span.Minutes}:{span.Seconds}";
        }
    
        public static string ToHHMMSSTextedFormat(int seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            return $"{span.Hours:00}h {span.Minutes:00}m {span.Seconds:00}s";
        }
    
        public static string ToHHMMTextedFormat(int seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            return $"{span.Hours:00}h {span.Minutes:00}m";
        }
    
        public static string ToHMSFormat(int seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            return $"{span.Hours}:{span.Minutes}:{span.Seconds}";
        }

        public static string ToMMSSTextedFormat(int seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            return $"{span.Minutes:00}m {span.Seconds:00}s";
        }
    
        public static string ToMMSSFormat(int seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            return $"{span.Minutes:00}:{span.Seconds:00}";
        }

        public static string ToSingleTextFormat(int seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
        
            if (span.Days > 0)
                return $"{span.Days}d";
        
            if (span.Hours > 0)
                return $"{span.Hours}h";
        
            return span.Minutes > 0 ? $"{span.Minutes}m" : $"{span.Seconds}s";
        }
    
        public static string ToHHMMorMMSSTextedFormat(int seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            return span.Minutes > 0 ? $"{span.Hours:00}h {span.Minutes:00}m" : $"{span.Minutes:00}m {span.Seconds:00}s";
        }

        public static string ToDDHHorHHMMorMMSSTextedFormat(int seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            if (span.Days >= 1) return $"{span.Days}d {span.Hours:00}h";
            return span.Hours > 0 ? $"{span.Hours:00}h {span.Minutes:00}m" : $"{span.Minutes:00}m {span.Seconds:00}s";
        }
    
        public static string ToDDHHMMorHHMMSSTextedFormat(int seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            return span.Days > 1 
                ? $"{span.Days}d {span.Hours:00}h {span.Minutes:00}m" 
                : $"{span.Hours:00}h {span.Minutes:00}m {span.Seconds:00}s";
        }
    
        public static string ToDDHHMMSSTextedFormat(int seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            return $"{span.Days}d {span.Hours:00}h {span.Minutes:00}m {span.Seconds:00}s";
        }
    }
}
