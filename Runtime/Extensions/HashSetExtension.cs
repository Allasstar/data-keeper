using System;
using System.Collections.Generic;

namespace DataKeeper.Extensions
{
    public static class HashSetExtension
    {
        private static readonly System.Random _systemRandom = new System.Random();

        // -------------------------------------------------------
        // RANDOM ELEMENT (Reservoir Sampling – optimal for HashSet)
        // -------------------------------------------------------
        public static T Random<T>(this HashSet<T> hashSet)
        {
            if (hashSet == null || hashSet.Count == 0)
                return default;

            int index = UnityEngine.Random.Range(0, hashSet.Count);
            int i = 0;

            foreach (var item in hashSet)
            {
                if (i == index)
                    return item;
                i++;
            }

            return default; // fallback (should never happen)
        }

        public static T RandomSystem<T>(this HashSet<T> hashSet, int seed)
        {
            if (hashSet == null || hashSet.Count == 0)
                return default;

            var rnd = new System.Random(seed);
            return RandomInternal(hashSet, rnd);
        }

        public static T RandomSystem<T>(this HashSet<T> hashSet)
        {
            if (hashSet == null || hashSet.Count == 0)
                return default;
            
            return RandomInternal(hashSet, _systemRandom);
        }

        private static T RandomInternal<T>(HashSet<T> set, System.Random rnd)
        {
            int index = rnd.Next(set.Count);
            int i = 0;

            foreach (var item in set)
            {
                if (i == index)
                    return item;
                i++;
            }

            return default;
        }

        // -------------------------------------------------------
        // SIMPLE HELPERS
        // -------------------------------------------------------
        public static bool IsEmpty<T>(this HashSet<T> hashSet)
        {
            return hashSet == null || hashSet.Count == 0;
        }

        public static HashSet<T> Clone<T>(this HashSet<T> hashSet)
        {
            return hashSet == null ? null : new HashSet<T>(hashSet);
        }

        // -------------------------------------------------------
        // TRY GET RANDOM
        // -------------------------------------------------------
        public static bool TryGetRandom<T>(this HashSet<T> hashSet, out T value)
        {
            if (hashSet == null || hashSet.Count == 0)
            {
                value = default;
                return false;
            }

            value = hashSet.Random();
            return true;
        }

        // -------------------------------------------------------
        // FIND RANDOM (Optimized)
        // -------------------------------------------------------
        // No LINQ, no allocations except an array/list if matches > 1
        public static T FindRandom<T>(this HashSet<T> hashSet, Predicate<T> match)
        {
            if (hashSet == null || hashSet.Count == 0)
                return default;

            // Collect matches without LINQ
            List<T> matches = null;

            foreach (var item in hashSet)
            {
                if (match(item))
                {
                    matches ??= new List<T>();
                    matches.Add(item);
                }
            }

            if (matches == null || matches.Count == 0)
                return default;

            return matches[_systemRandom.Next(matches.Count)];
        }

        // -------------------------------------------------------
        // CONTAINS via Predicate
        // -------------------------------------------------------
        public static bool Contains<T>(this HashSet<T> hashSet, Predicate<T> match)
        {
            if (hashSet == null)
                return false;

            foreach (var item in hashSet)
                if (match(item))
                    return true;

            return false;
        }

        // -------------------------------------------------------
        // SAFE RemoveWhere (fixes recursion mistake)
        // -------------------------------------------------------
        public static void RemoveWhere<T>(this HashSet<T> hashSet, Predicate<T> match)
        {
            if (hashSet == null || hashSet.Count == 0)
                return;

            List<T> toRemove = null;

            foreach (var item in hashSet)
            {
                if (match(item))
                {
                    toRemove ??= new List<T>();
                    toRemove.Add(item);
                }
            }

            if (toRemove != null)
                foreach (var rem in toRemove)
                    hashSet.Remove(rem);
        }

        // -------------------------------------------------------
        // SHUFFLE (HashSet cannot be shuffled directly)
        // -------------------------------------------------------
        public static HashSet<T> Shuffle<T>(this HashSet<T> hashSet)
        {
            if (hashSet == null || hashSet.Count <= 1)
                return hashSet;

            var list = new List<T>(hashSet);
            ShuffleList(list);

            hashSet.Clear();
            foreach (var item in list)
                hashSet.Add(item);

            return hashSet;
        }

        public static HashSet<T> ShuffleNew<T>(this HashSet<T> hashSet)
        {
            if (hashSet == null || hashSet.Count <= 1)
                return hashSet?.Clone();

            var list = new List<T>(hashSet);
            ShuffleList(list);
            return new HashSet<T>(list);
        }

        private static void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _systemRandom.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // -------------------------------------------------------
        // ADD RANGE (fast, simple)
        // -------------------------------------------------------
        public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items)
        {
            if (hashSet == null || items == null)
                return;

            foreach (var item in items)
                hashSet.Add(item);
        }
    }
}
