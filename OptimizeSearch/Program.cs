using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimizeSearch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    public class OptimizedSearcher<T>
    {
        private readonly Dictionary<string, HashSet<T>> _stringIndex;
        private readonly Dictionary<int, HashSet<T>> _intIndex;
        private readonly Dictionary<string, HashSet<T>> _byteArrayIndex;
        private readonly PropertyInfo[] _stringProperties;
        private readonly PropertyInfo[] _intProperties;
        private readonly PropertyInfo[] _byteArrayProperties;
        private readonly IEnumerable<T> _items;

        public OptimizedSearcher(IEnumerable<T> items)
        {
            _items = items ?? throw new ArgumentNullException("items");
            _stringIndex = new Dictionary<string, HashSet<T>>(StringComparer.OrdinalIgnoreCase);
            _intIndex = new Dictionary<int, HashSet<T>>();
            _byteArrayIndex = new Dictionary<string, HashSet<T>>(StringComparer.OrdinalIgnoreCase);

            _stringProperties = typeof(T).GetProperties().Where(p => p.PropertyType == typeof(string)).ToArray();
            _intProperties = typeof(T).GetProperties().Where(p => p.PropertyType == typeof(int)).ToArray();
            _byteArrayProperties = typeof(T).GetProperties().Where(p => p.PropertyType == typeof(byte[])).ToArray();

            BuildIndex();
        }

        private void BuildIndex()
        {
            Parallel.ForEach(_items, item =>
            {
                // Index string properties
                foreach (var prop in _stringProperties)
                {
                    if (prop.GetValue(item, null) is string value && !string.IsNullOrEmpty(value))
                    {
                        var words = value.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
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
                foreach (var prop in _intProperties)
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
                foreach (var prop in _byteArrayProperties)
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
            var stringTokens = searchString.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
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
                foreach (var key in _byteArrayIndex.Keys.Where(k =>
                             k.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    results.UnionWith(_byteArrayIndex[key]);
                }
            }

            // Final filter for exact matches
            return results.Count > 0
                ? results.Where(item => ContainsMatch(item, searchString))
                : _items.Where(item => ContainsMatch(item, searchString));
        }

        private bool ContainsMatch(T item, string searchString)
        {
            foreach (var prop in _stringProperties)
            {
                if (prop.GetValue(item, null) is string value && value != null &&
                    value.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            if (int.TryParse(searchString, out int intSearch))
            {
                foreach (var prop in _intProperties)
                {
                    if (prop.GetValue(item, null) is int value && value == intSearch)
                        return true;
                }
            }

            foreach (var prop in _byteArrayProperties)
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

    // Example Usage
    public class MyItem
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public byte[] Data { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var items = new List<MyItem>
            {
                new MyItem {Name = "Apple", Value = 42, Data = new byte[] {0xCA, 0xFE}},
                new MyItem {Name = "Banana", Value = 123, Data = new byte[] {0xBA, 0xBE}},
                new MyItem {Name = "Cherry", Value = 42, Data = new byte[] {0xDE, 0xAD}}
            };

            var searcher = new OptimizedSearcher<MyItem>(items);
            var results = searcher.Search("42");
            foreach (var item in results)
            {
                Console.WriteLine($"Found");
            }
        }
    }
}
