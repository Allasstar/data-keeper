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

        public static T RandomSystem<T>(this List<T> list, int seed)
        {
            System.Random systemRandom = new System.Random(seed);
            return list.Count == 0 ? default : list[systemRandom.Next(0, list.Count)];
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
            if (list.Count <= 1) return;
    
            // Fisher-Yates shuffle algorithm - iterate backwards from last element
            for (int currentIndex = list.Count - 1; currentIndex > 0; currentIndex--)
            {
                int randomIndex = _systemRandom.Next(currentIndex + 1);
                list.Swap(currentIndex, randomIndex);
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

        /// <summary>
        /// Returns the next index in a circular manner within the list.
        /// </summary>
        /// <param name="list">The list on which the operation is performed.</param>
        /// <param name="index">The current index from which the next index is calculated.</param>
        /// <typeparam name="T">The type of elements contained in the list.</typeparam>
        /// <returns>The next index in the list. Returns -1 if the list is empty.</returns>
        public static int NextIndex<T>(this List<T> list, int index)
        {
            if (list.Count == 0) return -1;
            return (index + 1) % list.Count;
        }

        /// <summary>
        /// Returns the previous index in a circular manner within the list.
        /// </summary>
        /// <param name="list">The list on which the operation is performed.</param>
        /// <param name="index">The current index from which the previous index is calculated.</param>
        /// <typeparam name="T">The type of elements contained in the list.</typeparam>
        /// <returns>The previous index in the list. Returns -1 if the list is empty.</returns>
        public static int PreviousIndex<T>(this List<T> list, int index)
        {
            if (list.Count == 0) return -1;
            return (index + list.Count - 1) % list.Count;
        }

        /// <summary>
        /// Retrieves the next element in the list relative to a given value, in a circular manner.
        /// </summary>
        /// <param name="list">The list to operate on.</param>
        /// <param name="value">The current value for which the next value is being determined.</param>
        /// <typeparam name="T">The type of elements contained in the list.</typeparam>
        /// <returns>The next element in the list after the specified value, or default if the list is empty or if the value is not found.</returns>
        public static T Next<T>(this List<T> list, T value)
        {
            var index = list.IndexOf(value);
            var nextIndex = list.NextIndex(index);
            return list.Get(nextIndex);
        }

        /// <summary>
        /// Retrieves the previous element in the list relative to a given value, in a circular manner.
        /// </summary>
        /// <param name="list">The list on which the operation is performed.</param>
        /// <param name="value">The value whose previous element is to be found.</param>
        /// <typeparam name="T">The type of elements contained in the list.</typeparam>
        /// <returns>The previous element in the list. Returns the default value for the type if the value is not found or the list is empty.</returns>
        public static T Previous<T>(this List<T> list, T value)
        {
            var index = list.IndexOf(value);
            var previousIndex = list.PreviousIndex(index);
            return list.Get(previousIndex);
        }
    }
}
