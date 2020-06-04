using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PointingPoker.Data
{
    public static class Utilities
    {
        public static bool TryUpdate<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TValue, TValue> updateFunction, out TValue updatedValue)
        {
            while (key != null && dictionary.TryGetValue(key, out var existingValue))
            {
                var newValue = updateFunction(existingValue);
                if (dictionary.TryUpdate(key, newValue, existingValue))
                {
                    updatedValue = newValue;
                    return true;
                }
            }

            updatedValue = default(TValue);
            return false;
        }

        public static bool TryUpdate<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TValue, TValue> updateFunction)
        {
            while (key != null && dictionary.TryGetValue(key, out var existingValue))
            {
                var newValue = updateFunction(existingValue);
                if (dictionary.TryUpdate(key, newValue, existingValue))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryUpdate<TKey, TValue>(this ConcurrentDictionary<TKey, CountingItem<TValue>> dictionary, TKey key, Func<TValue, TValue> updateFunction)
        {
            while (key != null && dictionary.TryGetValue(key, out var existingValue))
            {
                var newValue = updateFunction(existingValue.Item);
                if (dictionary.TryUpdate(key, new CountingItem<TValue>(existingValue.Count, newValue), existingValue))
                {
                    return true;
                }
            }

            return false;
        }

        public static void SafeInvoke(this Action action)
        {
            try
            {
                action.Invoke();
            }
            catch
            {

            }
        }

        public static void SafeInvoke<T>(this Action<T> action, T arg)
        {
            try
            {
                action.Invoke(arg);
            }
            catch
            {

            }
        }

        public static List<T> RemoveAllFluent<T>(this List<T> list, Predicate<T> condition)
        {
            list.RemoveAll(condition);
            return list;
        }

        public static List<T> AddFluent<T>(this List<T> list, T value)
        {
            list.Add(value);
            return list;
        }
    }
}
