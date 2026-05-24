using System.Collections.Generic;

namespace SnowyLib
{
    public class AutoDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    where TValue : new()
    {
        public new TValue this[TKey key]
        {
            get
            {
                if (!ContainsKey(key))
                    base[key] = new TValue();

                return base[key];
            }
            set => base[key] = value;
        }
    }
}
