using System;

namespace DataKeeper.Extensions
{
    public static class ObjectExtensions
    {
        public static bool IsNull(this object obj)
        {
            if (obj is UnityEngine.Object unityObj)
            {
                return unityObj == null;
            }
            
            return obj == null;
        }
        
        /// <summary>
        /// Safely casts an object to the specified type T.
        /// Returns the cast object if successful, otherwise returns the default value for T.
        /// </summary>
        /// <typeparam name="T">The target type to cast to</typeparam>
        /// <param name="obj">The object to cast</param>
        /// <returns>The cast object or default(T) if cast fails</returns>
        public static T Cast<T>(this object obj)
        {
            if (obj is T result)
                return result;
        
            return default(T);
        }

        /// <summary>
        /// Safely casts an object to the specified type T with a custom default value.
        /// Returns the cast object if successful, otherwise returns the provided default value.
        /// </summary>
        /// <typeparam name="T">The target type to cast to</typeparam>
        /// <param name="obj">The object to cast</param>
        /// <param name="defaultValue">The default value to return if cast fails</param>
        /// <returns>The cast object or the provided default value if cast fails</returns>
        public static T Cast<T>(this object obj, T defaultValue)
        {
            if (obj is T result)
                return result;
        
            return defaultValue;
        }

        /// <summary>
        /// Attempts to cast an object to the specified type T.
        /// Returns true if successful and outputs the cast object, otherwise returns false.
        /// </summary>
        /// <typeparam name="T">The target type to cast to</typeparam>
        /// <param name="obj">The object to cast</param>
        /// <param name="result">The cast result if successful</param>
        /// <returns>True if cast was successful, false otherwise</returns>
        public static bool TryCast<T>(this object obj, out T result)
        {
            if (obj is T castResult)
            {
                result = castResult;
                return true;
            }
        
            result = default(T);
            return false;
        }

        /// <summary>
        /// Forcefully casts an object to the specified type T.
        /// Throws InvalidCastException if the cast is not possible.
        /// </summary>
        /// <typeparam name="T">The target type to cast to</typeparam>
        /// <param name="obj">The object to cast</param>
        /// <returns>The cast object</returns>
        /// <exception cref="InvalidCastException">Thrown when the cast is not possible</exception>
        public static T ForceCast<T>(this object obj)
        {
            if (obj is T result)
                return result;
        
            throw new InvalidCastException($"Cannot cast object of type '{obj?.GetType().Name ?? "null"}' to type '{typeof(T).Name}'");
        }

        /// <summary>
        /// Converts an object to the specified type T using Convert.ChangeType.
        /// Useful for converting between compatible types (e.g., string to int, int to double).
        /// </summary>
        /// <typeparam name="T">The target type to convert to</typeparam>
        /// <param name="obj">The object to convert</param>
        /// <returns>The converted object</returns>
        /// <exception cref="InvalidCastException">Thrown when conversion is not possible</exception>
        /// <exception cref="FormatException">Thrown when the format is invalid</exception>
        /// <exception cref="OverflowException">Thrown when the value is too large or small</exception>
        public static T ConvertTo<T>(this object obj)
        {
            if (obj == null)
                return default(T);

            if (obj is T directCast)
                return directCast;

            try
            {
                return (T)Convert.ChangeType(obj, typeof(T));
            }
            catch (Exception ex) when (ex is InvalidCastException || ex is FormatException || ex is OverflowException)
            {
                throw;
            }
        }

        /// <summary>
        /// Safely converts an object to the specified type T using Convert.ChangeType.
        /// Returns the converted object if successful, otherwise returns the default value for T.
        /// </summary>
        /// <typeparam name="T">The target type to convert to</typeparam>
        /// <param name="obj">The object to convert</param>
        /// <returns>The converted object or default(T) if conversion fails</returns>
        public static T SafeConvertTo<T>(this object obj)
        {
            try
            {
                return obj.ConvertTo<T>();
            }
            catch
            {
                return default(T);
            }
        }
    }
}
