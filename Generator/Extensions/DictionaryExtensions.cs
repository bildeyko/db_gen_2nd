using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generator.Extensions
{
    public static class DictionaryExtensions
    {
        public static IDictionary<V, K> FlipDict<V,K>(this IDictionary<K, V> instance)
        {
            IDictionary<V, K> newDict = new Dictionary<V, K>();
            var allKeys = instance.Keys.ToArray();
            foreach (var key in allKeys)
            {
                newDict.Add(instance[key], key);
            }
            return newDict;
        }

        public static Dictionary<K, V>
            Merge<K, V>(this IDictionary<K, V> instance, IDictionary<K, V> dict)
        {
            List<IDictionary<K, V>> dictionaries = new List<IDictionary<K, V>> { instance, dict };

            var result = dictionaries.SelectMany(d => d)
                         .ToLookup(pair => pair.Key, pair => pair.Value)
                         .ToDictionary(group => group.Key, group => group.First());

            return result;
        }
    }
}
