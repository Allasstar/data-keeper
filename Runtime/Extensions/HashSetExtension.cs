using System;
using System.Collections.Generic;
using System.Linq;

namespace DataKeeper.Extensions
{
    public static class HashSetExtension
    {
        private static readonly System.Random _systemRandom = new System.Random();

        public static T Random<T>(this HashSet<T> hashSet)
        {
            if (hashSet.Count == 0) return default;
            return hashSet.ElementAt(UnityEngine.Random.Range(0, hashSet.Count));
        }

        public static T RandomSystem<T>(this HashSet<T> hashSet, int seed)
        {
            if (hashSet.Count == 0) return default;
            System.Random systemRandom = new System.Random(seed);
            return hashSet.ElementAt(systemRandom.Next(0, hashSet.Count));
        }

        public static T RandomSystem<T>(this HashSet<T> hashSet)
        {
            if (hashSet.Count == 0) return default;
            return hashSet.ElementAt(_systemRandom.Next(0, hashSet.Count));
        }

        public static bool IsEmpty<T>(this HashSet<T> hashSet)
        {
            return hashSet.Count == 0;
        }

        public static HashSet<T> Clone<T>(this HashSet<T> hashSet)
        {
            return new HashSet<T>(hashSet);
        }

        public static bool TryGetRandom<T>(this HashSet<T> hashSet, out T value)
        {
            if (hashSet.Count == 0)
            {
                value = default;
                return false;
            }
            value = hashSet.Random();
            return true;
        }

        public static T FindRandom<T>(this HashSet<T> hashSet, Predicate<T> match)
        {
            if (hashSet.Count == 0) return default;
            var matchingItems = hashSet.Where(item => match(item)).ToArray();
            return matchingItems.Length > 0 ? matchingItems[_systemRandom.Next(matchingItems.Length)] : default;
        }

        // New extensions from ListExtension that are applicable to HashSet
        public static bool Contains<T>(this HashSet<T> hashSet, Predicate<T> match)
        {
            return hashSet.Any(item => match(item));
        }

        public static void RemoveWhere<T>(this HashSet<T> hashSet, Predicate<T> match)
        {
            hashSet.RemoveWhere(match);
        }

        public static HashSet<T> Shuffle<T>(this HashSet<T> hashSet)
        {
            if (hashSet.Count <= 1) return hashSet;
        
            var list = hashSet.ToList();
            PerformShuffle(list);
            hashSet.Clear();
            foreach (var item in list)
            {
                hashSet.Add(item);
            }
            return hashSet;
        }

        public static HashSet<T> ShuffleNew<T>(this HashSet<T> hashSet)
        {
            if (hashSet.Count <= 1) return new HashSet<T>(hashSet);
        
            var list = hashSet.ToList();
            PerformShuffle(list);
            return new HashSet<T>(list);
        }

        private static void PerformShuffle<T>(List<T> list)
        {
            if (list.Count <= 1) return;

            // Fisher-Yates shuffle algorithm - iterate backwards from last element
            for (int currentIndex = list.Count - 1; currentIndex > 0; currentIndex--)
            {
                int randomIndex = _systemRandom.Next(currentIndex + 1);
                (list[currentIndex], list[randomIndex]) = (list[randomIndex], list[currentIndex]);
            }
        }

        public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                hashSet.Add(item);
            }
        }
    }
}