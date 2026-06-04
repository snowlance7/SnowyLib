using System;
using System.Collections.Generic;

namespace SnowyLib
{
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
