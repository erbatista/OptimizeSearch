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
                IndexItem(item, item.GetType());
            });
        }

        private void IndexItem(T item, Type type, HashSet<object> visited = null)
        {
            if (visited == null) visited = new HashSet<object>();
            if (item == null || visited.Contains(item)) return; // Prevent infinite recursion
            visited.Add(item);

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                object value = prop.GetValue(item, null);
                if (value == null) continue;

                Type propType = prop.PropertyType;

                if (propType == typeof(string))
                {
                    if (value is string strValue && !string.IsNullOrEmpty(strValue))
                    {
                        var words = strValue.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
                else if (propType == typeof(int))
                {
                    if (value is int intValue)
                    {
                        lock (_intIndex)
                        {
                            if (!_intIndex.TryGetValue(intValue, out var set))
                            {
                                set = new HashSet<T>();
                                _intIndex[intValue] = set;
                            }
                            set.Add(item);
                        }
                    }
                }
                else if (propType == typeof(byte[]))
                {
                    if (value is byte[] byteValue && byteValue.Length > 0)
                    {
                        var hexKey = BitConverter.ToString(byteValue).Replace("-", "");
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
                else if (!propType.IsPrimitive && propType != typeof(object) && !propType.IsValueType && propType.IsClass)
                {
                    // Recursively index complex properties
                    IndexItem(item, propType, visited);
                }
            }
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
            return ContainsMatchRecursive(item, item.GetType(), searchString, new HashSet<object>());
        }

        private bool ContainsMatchRecursive(object item, Type type, string searchString, HashSet<object> visited)
        {
            if (item == null || visited.Contains(item)) return false;
            visited.Add(item);

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                object value = prop.GetValue(item, null);
                if (value == null) continue;

                Type propType = prop.PropertyType;

                if (propType == typeof(string))
                {
                    if (value is string strValue && strValue != null && strValue.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
                else if (propType == typeof(int))
                {
                    if (int.TryParse(searchString, out int intSearch) && value is int intValue && intValue == intSearch)
                        return true;
                }
                else if (propType == typeof(byte[]))
                {
                    if (value is byte[] byteValue && byteValue != null)
                    {
                        var hexValue = BitConverter.ToString(byteValue).Replace("-", "");
                        if (hexValue.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                            return true;
                    }
                }
                else if (!propType.IsPrimitive && propType != typeof(object) && !propType.IsValueType && propType.IsClass)
                {
                    if (ContainsMatchRecursive(value, propType, searchString, visited))
                        return true;
                }
            }
            return false;
        }
    }
}