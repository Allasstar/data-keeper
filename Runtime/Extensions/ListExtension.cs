using System;
using System.Collections.Generic;
using System.Linq;

namespace DataKeeper.Extensions
{
    public static class ListExtension
    {
        private static readonly System.Random _systemRandom = new System.Random();

        public static void Swap<T>(this List<T> list, int indexA, int indexB)
        {
            if (indexA == indexB) return;
            (list[indexA], list[indexB]) = (list[indexB], list[indexA]);
        }

        public static T Pop<T>(this List<T> list)
        {
            if (list.Count == 0) return default;

            T item = list[0];
            list.RemoveAt(0);
            return item;
        }

        public static T PopLast<T>(this List<T> list)
        {
            if (list.Count == 0) return default;

            int lastIndex = list.Count - 1;
            T item = list[lastIndex];
            list.RemoveAt(lastIndex);
            return item;
        }

        public static T Random<T>(this List<T> list)
        {
            return list.Count == 0 ? default : list[UnityEngine.Random.Range(0, list.Count)];
        }

        public static T RandomSystem<T>(this List<T> list)
        {
            return list.Count == 0 ? default : list[_systemRandom.Next(0, list.Count)];
        }

        public static bool HasIndex<T>(this List<T> list, int index)
        {
            return index >= 0 && index < list.Count;
        }

        public static T Get<T>(this List<T> list, int index)
        {
            return list.HasIndex(index) ? list[index] : default;
        }

        public static bool TryGet<T>(this List<T> list, int index, out T value)
        {
            bool hasIndex = list.HasIndex(index);
            value = hasIndex ? list[index] : default;
            return hasIndex;
        }

        public static List<T> Clone<T>(this List<T> list)
        {
            return new List<T>(list);
        }

        public static void Shuffle<T>(this List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _systemRandom.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        public static T FindRandom<T>(this List<T> list, Predicate<T> match)
        {
            if (list.Count == 0) return default;

            List<T> matchingItems = list.Where(item => match(item)).ToList();
            return matchingItems.Count > 0 ? matchingItems[_systemRandom.Next(matchingItems.Count)] : default;
        }

        public static void RemoveAll<T>(this List<T> list, Predicate<T> match)
        {
            list.RemoveAll(match);
        }

        public static int IndexOf<T>(this List<T> list, Predicate<T> match)
        {
            return list.FindIndex(match);
        }
    }
}