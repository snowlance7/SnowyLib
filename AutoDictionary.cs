using System;
using System.Collections.Generic;

namespace SnowyLib
{
    /// <summary>
    /// Provides a dictionary that creates and adds values automatically using a factory function when a key is accessed
    /// and not present.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary. Must have a parameterless constructor.</typeparam>
    public class AutoDictionary<TKey, TValue> : Dictionary<TKey, TValue> where TValue : new()
    {
        private readonly Func<TKey, TValue> _factory;

        public AutoDictionary(Func<TKey, TValue> factory)
        {
            _factory = factory;
        }

        public new TValue this[TKey key]
        {
            get
            {
                if (!TryGetValue(key, out TValue value))
                {
                    value = _factory(key);
                    base[key] = value;
                }

                return value;
            }
            set => base[key] = value;
        }
    }
}
