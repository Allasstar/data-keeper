using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DataKeeper.Extensions
{
    public static class EnumerableExtensions
    {
        private static readonly System.Random _random = new System.Random();

        // -----------------------------------------------------------
        // POP (SAFE VERSION)
        // -----------------------------------------------------------
        public static (T item, IEnumerable<T> remaining) Pop<T>(this IEnumerable<T> source)
        {
            if (source == null) 
                return (default, Enumerable.Empty<T>());

            using var e = source.GetEnumerator();

            if (!e.MoveNext())
                return (default, Enumerable.Empty<T>());

            // Grab first element
            var first = e.Current;

            // Copy remaining now (safe)
            var remaining = new List<T>();
            while (e.MoveNext())
                remaining.Add(e.Current);

            return (first, remaining);
        }

        // -----------------------------------------------------------
        // RANDOM (UNITY RNG) — RESERVOIR SAMPLING
        // -----------------------------------------------------------
        public static T Random<T>(this IEnumerable<T> source)
        {
            if (source == null)
                return default;

            T selected = default;
            int count = 0;

            foreach (var item in source)
            {
                count++;
                if (UnityEngine.Random.Range(0, count) == 0)
                    selected = item;
            }

            return count > 0 ? selected : default;
        }

        // -----------------------------------------------------------
        // RANDOM (System RNG)
        // -----------------------------------------------------------
        public static T RandomSystem<T>(this IEnumerable<T> source)
            => RandomInternal(source, _random);

        public static T RandomSystem<T>(this IEnumerable<T> source, int seed)
            => RandomInternal(source, new System.Random(seed));

        private static T RandomInternal<T>(IEnumerable<T> source, System.Random rnd)
        {
            if (source == null)
                return default;

            T selected = default;
            int count = 0;

            foreach (var item in source)
            {
                count++;
                if (rnd.Next(count) == 0)
                    selected = item;
            }

            return count > 0 ? selected : default;
        }

        // -----------------------------------------------------------
        // HAS INDEX — FAST FOR COLLECTIONS, STREAM SAFE
        // -----------------------------------------------------------
        public static bool HasIndex<T>(this IEnumerable<T> source, int index)
        {
            if (index < 0)
                return false;

            if (source is ICollection<T> col)
                return index < col.Count;

            using var e = source.GetEnumerator();
            for (int i = 0; i <= index; i++)
                if (!e.MoveNext()) return false;

            return true;
        }

        // -----------------------------------------------------------
        // GET / TRYGET
        // -----------------------------------------------------------
        public static T Get<T>(this IEnumerable<T> source, int index)
            => source.ElementAtOrDefault(index);

        public static bool TryGet<T>(this IEnumerable<T> source, int index, out T value)
        {
            value = default;
            if (index < 0) return false;

            if (source is IList<T> list)
            {
                if (index < list.Count)
                {
                    value = list[index];
                    return true;
                }
                return false;
            }

            using var e = source.GetEnumerator();
            for (int i = 0; i <= index; i++)
                if (!e.MoveNext()) return false;

            value = e.Current;
            return true;
        }

        // -----------------------------------------------------------
        // FIND RANDOM MATCH — RESERVOIR SAMPLING
        // -----------------------------------------------------------
        public static T FindRandom<T>(this IEnumerable<T> source, Predicate<T> match)
        {
            T selected = default;
            int count = 0;

            foreach (var item in source)
            {
                if (!match(item)) continue;

                count++;
                if (_random.Next(count) == 0)
                    selected = item;
            }

            return count > 0 ? selected : default;
        }

        // -----------------------------------------------------------
        // REMOVE ALL
        // -----------------------------------------------------------
        public static IEnumerable<T> RemoveAll<T>(this IEnumerable<T> source, Predicate<T> match)
        {
            foreach (var item in source)
                if (!match(item))
                    yield return item;
        }

        // -----------------------------------------------------------
        // INDEX OF
        // -----------------------------------------------------------
        public static int IndexOf<T>(this IEnumerable<T> source, Predicate<T> match)
        {
            int index = 0;

            foreach (var item in source)
            {
                if (match(item))
                    return index;
                index++;
            }

            return -1;
        }

        // -----------------------------------------------------------
        // NEXT INDEX / PREVIOUS INDEX — FAST FOR COLLECTIONS
        // -----------------------------------------------------------
        public static int NextIndex<T>(this IEnumerable<T> source, int index)
        {
            int count = source is ICollection<T> col ? col.Count : source.Count();
            if (count == 0) return -1;
            return (index + 1) % count;
        }

        public static int PreviousIndex<T>(this IEnumerable<T> source, int index)
        {
            int count = source is ICollection<T> col ? col.Count : source.Count();
            if (count == 0) return -1;
            return (index + count - 1) % count;
        }

        // -----------------------------------------------------------
        // NEXT (CYCLIC)
        // -----------------------------------------------------------
        public static T Next<T>(this IEnumerable<T> source, T value)
        {
            bool found = false;
            T first = default;
            bool hasFirst = false;

            foreach (var item in source)
            {
                if (!hasFirst)
                {
                    first = item;
                    hasFirst = true;
                }

                if (found)
                    return item;

                if (EqualityComparer<T>.Default.Equals(item, value))
                    found = true;
            }

            return found && hasFirst ? first : default;
        }

        // -----------------------------------------------------------
        // PREVIOUS (CYCLIC)
        // -----------------------------------------------------------
        public static T Previous<T>(this IEnumerable<T> source, T value)
        {
            T first = default;
            bool hasFirst = false;

            T prev = default;
            bool hasPrev = false;

            foreach (var item in source)
            {
                if (!hasFirst)
                {
                    first = item;
                    hasFirst = true;
                }

                if (EqualityComparer<T>.Default.Equals(item, value))
                    return hasPrev ? prev : GetLast(source);

                prev = item;
                hasPrev = true;
            }

            return default;
        }

        private static T GetLast<T>(IEnumerable<T> source)
        {
            T last = default;
            bool any = false;

            foreach (var item in source)
            {
                last = item;
                any = true;
            }

            return any ? last : default;
        }
    }
}
