using System;
using System.Collections.Generic;

namespace DataKeeper.Extensions
{
    public static class ListExtension
    {
        private static readonly Random _systemRandom = new Random();

        // --------------------------------------------------------
        // SWAP
        // --------------------------------------------------------
        public static void Swap<T>(this List<T> list, int a, int b)
        {
            if ((uint)a >= (uint)list.Count || (uint)b >= (uint)list.Count) return;
            if (a == b) return;

            T temp = list[a];
            list[a] = list[b];
            list[b] = temp;
        }

        // --------------------------------------------------------
        // POP FIRST
        // --------------------------------------------------------
        public static T Pop<T>(this List<T> list)
        {
            if (list.Count == 0) return default;
            T value = list[0];
            list.RemoveAt(0);
            return value;
        }

        // --------------------------------------------------------
        // POP LAST
        // --------------------------------------------------------
        public static T PopLast<T>(this List<T> list)
        {
            int count = list.Count;
            if (count == 0) return default;

            int last = count - 1;
            T value = list[last];
            list.RemoveAt(last);
            return value;
        }

        // --------------------------------------------------------
        // RANDOM UNITY
        // --------------------------------------------------------
        public static T Random<T>(this List<T> list)
        {
            int count = list.Count;
            return count == 0 ? default : list[UnityEngine.Random.Range(0, count)];
        }

        // --------------------------------------------------------
        // RANDOM SYSTEM
        // --------------------------------------------------------
        public static T RandomSystem<T>(this List<T> list)
        {
            int count = list.Count;
            return count == 0 ? default : list[_systemRandom.Next(count)];
        }

        public static T RandomSystem<T>(this List<T> list, int seed)
        {
            int count = list.Count;
            if (count == 0) return default;

            Random rnd = new Random(seed);
            return list[rnd.Next(count)];
        }

        // --------------------------------------------------------
        // HAS INDEX
        // --------------------------------------------------------
        public static bool HasIndex<T>(this List<T> list, int index)
        {
            return (uint)index < (uint)list.Count;
        }

        // --------------------------------------------------------
        // GET / TRYGET
        // --------------------------------------------------------
        public static T Get<T>(this List<T> list, int index)
        {
            return (uint)index < (uint)list.Count ? list[index] : default;
        }

        public static bool TryGet<T>(this List<T> list, int index, out T value)
        {
            if ((uint)index < (uint)list.Count)
            {
                value = list[index];
                return true;
            }

            value = default;
            return false;
        }

        // --------------------------------------------------------
        // CLONE
        // --------------------------------------------------------
        public static List<T> Clone<T>(this List<T> list)
        {
            return new List<T>(list); // fast internal copy
        }

        // --------------------------------------------------------
        // SHUFFLE (FISHER-YATES)
        // --------------------------------------------------------
        public static List<T> Shuffle<T>(this List<T> list)
        {
            PerformShuffle(list);
            return list;
        }

        public static List<T> ShuffleNew<T>(this List<T> list)
        {
            var newList = new List<T>(list);
            PerformShuffle(newList);
            return newList;
        }

        private static void PerformShuffle<T>(List<T> list)
        {
            int n = list.Count;
            if (n <= 1) return;

            for (int i = n - 1; i > 0; i--)
            {
                int j = _systemRandom.Next(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        // --------------------------------------------------------
        // FIND RANDOM MATCH (reservoir sampling)
        // --------------------------------------------------------
        public static T FindRandom<T>(this List<T> list, Predicate<T> match)
        {
            T selected = default;
            int count = 0;

            for (int i = 0; i < list.Count; i++)
            {
                T item = list[i];
                if (!match(item)) continue;

                count++;
                if (_systemRandom.Next(count) == 0)
                    selected = item;
            }

            return count > 0 ? selected : default;
        }

        // --------------------------------------------------------
        // INDEX OF MATCH
        // --------------------------------------------------------
        public static int IndexOf<T>(this List<T> list, Predicate<T> match)
        {
            return list.FindIndex(match);
        }

        // --------------------------------------------------------
        // CIRCULAR INDEXING
        // --------------------------------------------------------
        public static int NextIndex<T>(this List<T> list, int index)
        {
            int count = list.Count;
            if (count == 0) return -1;
            return index + 1 < count ? index + 1 : 0;
        }

        public static int PreviousIndex<T>(this List<T> list, int index)
        {
            int count = list.Count;
            if (count == 0) return -1;
            return index > 0 ? index - 1 : count - 1;
        }

        // --------------------------------------------------------
        // CIRCULAR NEXT/PREV VALUE
        // --------------------------------------------------------
        public static T Next<T>(this List<T> list, T value)
        {
            int index = list.IndexOf(value);
            if (index < 0) return default;

            int next = index + 1 < list.Count ? index + 1 : 0;
            return list[next];
        }

        public static T Previous<T>(this List<T> list, T value)
        {
            int index = list.IndexOf(value);
            if (index < 0) return default;

            int prev = index > 0 ? index - 1 : list.Count - 1;
            return list[prev];
        }
    }
}
