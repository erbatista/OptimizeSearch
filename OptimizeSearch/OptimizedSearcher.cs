using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace OptimizeSearch
{
    public class OptimizedSearcher<T>
    {
        private readonly Dictionary<string, HashSet<T>> _stringIndex;
        private readonly Dictionary<int, HashSet<T>> _intIndex;
        private readonly Dictionary<string, HashSet<T>> _byteArrayIndex;
        private readonly IEnumerable<T> _items;

        public OptimizedSearcher(IEnumerable<T> items)
        {
            _items = items ?? throw new ArgumentNullException("items");
            _stringIndex = new Dictionary<string, HashSet<T>>(StringComparer.OrdinalIgnoreCase);
            _intIndex = new Dictionary<int, HashSet<T>>();
            _byteArrayIndex = new Dictionary<string, HashSet<T>>(StringComparer.OrdinalIgnoreCase);

            BuildIndex();
        }

        private void BuildIndex()
        {
            Parallel.ForEach(_items, item =>
            {
                Type concreteType = item.GetType();
                var stringProperties = concreteType.GetProperties().Where(p => p.PropertyType == typeof(string)).ToArray();
                var intProperties = concreteType.GetProperties().Where(p => p.PropertyType == typeof(int)).ToArray();
                var byteArrayProperties = concreteType.GetProperties().Where(p => p.PropertyType == typeof(byte[])).ToArray();

                // Index string properties
                foreach (var prop in stringProperties)
                {
                    if (prop.GetValue(item, null) is string value && !string.IsNullOrEmpty(value))
                    {
                        var words = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        lock (_stringIndex)
                        {
                            foreach (var word in words)
                            {
                                if (!_stringIndex.TryGetValue(word, out var set))
                                {
                                    set = new HashSet<T>();
                                    _stringIndex[word] = set;
                                }
                                set.Add(item);
                            }
                        }
                    }
                }

                // Index int properties
                foreach (var prop in intProperties)
                {
                    if (prop.GetValue(item, null) is int value)
                    {
                        lock (_intIndex)
                        {
                            if (!_intIndex.TryGetValue(value, out var set))
                            {
                                set = new HashSet<T>();
                                _intIndex[value] = set;
                            }
                            set.Add(item);
                        }
                    }
                }

                // Index byte[] properties
                foreach (var prop in byteArrayProperties)
                {
                    if (prop.GetValue(item, null) is byte[] value && value != null && value.Length > 0)
                    {
                        var hexKey = BitConverter.ToString(value).Replace("-", "");
                        lock (_byteArrayIndex)
                        {
                            if (!_byteArrayIndex.TryGetValue(hexKey, out var set))
                            {
                                set = new HashSet<T>();
                                _byteArrayIndex[hexKey] = set;
                            }
                            set.Add(item);
                        }
                    }
                }
            });
        }

        public IEnumerable<T> Search(string searchString)
        {
            if (string.IsNullOrEmpty(searchString)) return _items;

            var results = new HashSet<T>();

            // Try parsing as int first
            if (int.TryParse(searchString, out int intValue))
            {
                if (_intIndex.TryGetValue(intValue, out var intMatches))
                {
                    results.UnionWith(intMatches);
                }
            }

            // Search string index
            var stringTokens = searchString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            HashSet<T> stringResults = null;
            foreach (var token in stringTokens)
            {
                if (_stringIndex.TryGetValue(token, out var tokenSet))
                {
                    if (stringResults == null)
                        stringResults = new HashSet<T>(tokenSet);
                    else
                        stringResults.IntersectWith(tokenSet);
                }
                else
                {
                    stringResults = null;
                    break;
                }
            }
            if (stringResults != null)
                results.UnionWith(stringResults);

            // Search byte[] (exact hex match or substring)
            if (_byteArrayIndex.TryGetValue(searchString, out var byteMatches))
            {
                results.UnionWith(byteMatches);
            }
            else
            {
                foreach (var key in _byteArrayIndex.Keys.Where(k => k.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    results.UnionWith(_byteArrayIndex[key]);
                }
            }

            // Final filter for exact matches
            return results.Count > 0 ? results.Where(item => ContainsMatch(item, searchString)) : _items.Where(item => ContainsMatch(item, searchString));
        }

        private bool ContainsMatch(T item, string searchString)
        {
            Type concreteType = item.GetType();
            var stringProperties = concreteType.GetProperties().Where(p => p.PropertyType == typeof(string)).ToArray();
            var intProperties = concreteType.GetProperties().Where(p => p.PropertyType == typeof(int)).ToArray();
            var byteArrayProperties = concreteType.GetProperties().Where(p => p.PropertyType == typeof(byte[])).ToArray();

            foreach (var prop in stringProperties)
            {
                if (prop.GetValue(item, null) is string value && value != null && value.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            if (int.TryParse(searchString, out int intSearch))
            {
                foreach (var prop in intProperties)
                {
                    if (prop.GetValue(item, null) is int value && value == intSearch)
                        return true;
                }
            }

            foreach (var prop in byteArrayProperties)
            {
                if (prop.GetValue(item, null) is byte[] value && value != null)
                {
                    var hexValue = BitConverter.ToString(value).Replace("-", "");
                    if (hexValue.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }

            return false;
        }
    }
}