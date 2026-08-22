#if DATAKEEPER_LOCALIZATION

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace DataKeeper.Extensions
{
   
    public static class LocalizedStringExtensions
    {
        public static string GetLocalizedStringSafe(this LocalizedString localizedString, string fallback = "")
        {
            if (!CanResolve(localizedString)) return fallback;

            try
            {
                var value = localizedString.GetLocalizedString();
                return string.IsNullOrEmpty(value) ? fallback : value;
            }
            catch (Exception e)
            {
                Warn(localizedString, e);
                return fallback;
            }
        }

        public static string GetLocalizedStringSafe(this LocalizedString localizedString, string fallback, params object[] arguments)
        {
            if (!CanResolve(localizedString)) return fallback;

            try
            {
                var value = localizedString.GetLocalizedString(arguments);
                return string.IsNullOrEmpty(value) ? fallback : value;
            }
            catch (Exception e)
            {
                Warn(localizedString, e);
                return fallback;
            }
        }

        public static string GetLocalizedStringSafe(this LocalizedString localizedString, string fallback, IList<object> arguments)
        {
            if (!CanResolve(localizedString)) return fallback;

            try
            {
                var value = localizedString.GetLocalizedString(arguments);
                return string.IsNullOrEmpty(value) ? fallback : value;
            }
            catch (Exception e)
            {
                Warn(localizedString, e);
                return fallback;
            }
        }

        public static string GetLocalizedStringOrKey(this LocalizedString localizedString)
            => localizedString.GetLocalizedStringSafe(localizedString.KeyOrEmpty());

       
        public static bool TryGetLocalizedString(this LocalizedString localizedString, out string value)
        {
            value = localizedString.GetLocalizedStringSafe();
            return !string.IsNullOrEmpty(value);
        }

        public static string KeyOrEmpty(this LocalizedString localizedString)
        {
            if (localizedString == null) return "";

            var entry = localizedString.TableEntryReference;
            if (entry.ReferenceType == TableEntryReference.Type.Name && !string.IsNullOrEmpty(entry.Key))
                return entry.Key;

            return entry.ReferenceType == TableEntryReference.Type.Id ? entry.KeyId.ToString() : "";
        }

        private static bool CanResolve(LocalizedString localizedString)
            => localizedString != null && !localizedString.IsEmpty;

        private static void Warn(LocalizedString localizedString, Exception e)
            => Debug.LogWarning($"[Localization] '{localizedString.TableReference}/{localizedString.KeyOrEmpty()}' could not be resolved: {e.Message}");
    }
}
#endif