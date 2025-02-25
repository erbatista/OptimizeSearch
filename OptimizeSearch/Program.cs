using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimizeSearch
{
#nullable enable

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
        private readonly IEnumerable<T> _items;

        public OptimizedSearcher(IEnumerable<T> items)
        {
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _stringIndex = new Dictionary<string, HashSet<T>>(StringComparer.OrdinalIgnoreCase);
            _intIndex = new Dictionary<int, HashSet<T>>();

            BuildIndex();
        }

        private void BuildIndex()
        {
            Parallel.ForEach(_items, item =>
            {
                IndexItem(item, item, item.GetType());
            });
        }

        private void IndexItem(T parentItem, object? currentItem, Type type, HashSet<object>? visited = null)
        {
            visited ??= new HashSet<object>();
            if (currentItem == null || visited.Contains(currentItem)) return;
            visited.Add(currentItem);

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                object? value = prop.GetValue(currentItem, null);
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
                                set.Add(parentItem);
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
                            set.Add(parentItem);
                        }
                    }
                }
                else if (propType == typeof(byte[]))
                {
                    if (value is byte[] byteValue && byteValue.Length > 0)
                    {
                        string strValue = byteValue.Length switch
                        {
                            6 => FormatMacAddress(byteValue),
                            16 => new Guid(byteValue).ToString(),
                            _ => throw new ArgumentException($"Unexpected byte[] length {byteValue.Length} for property {prop.Name}")
                        };

                        var words = strValue.Split(new[] { ' ', '-', ':' }, StringSplitOptions.RemoveEmptyEntries);
                        lock (_stringIndex)
                        {
                            foreach (var word in words)
                            {
                                if (!_stringIndex.TryGetValue(word, out var set))
                                {
                                    set = new HashSet<T>();
                                    _stringIndex[word] = set;
                                }
                                set.Add(parentItem);
                            }
                        }
                    }
                }
                else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propType) && propType != typeof(string) && propType != typeof(byte[]))
                {
                    if (value is System.Collections.IEnumerable enumerable)
                    {
                        foreach (var element in enumerable)
                        {
                            if (element != null)
                            {
                                IndexItem(parentItem, element, element.GetType(), visited);
                            }
                        }
                    }
                }
                else if (!propType.IsPrimitive && propType != typeof(object) && !propType.IsValueType && propType.IsClass)
                {
                    IndexItem(parentItem, value, propType, visited);
                }
            }
        }

        private string FormatMacAddress(byte[] bytes)
        {
            if (bytes.Length != 6) throw new ArgumentException("MAC address must be 6 bytes.");
            return string.Join(":", bytes.Select(b => b.ToString("X2")));
        }

        public IEnumerable<T> Search(string? searchString, bool useAndCondition = true)
        {
            if (string.IsNullOrEmpty(searchString)) return _items;

            // Split search string by comma for multiple terms
            var searchTerms = searchString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (searchTerms.Length == 0) return _items;

            var results = new HashSet<T>();

            if (useAndCondition)
            {
                // AND: Intersect results for all terms
                bool firstTerm = true;
                foreach (var term in searchTerms)
                {
                    var termResults = new HashSet<T>();

                    if (int.TryParse(term, out int intValue))
                    {
                        if (_intIndex.TryGetValue(intValue, out var intMatches))
                        {
                            termResults.UnionWith(intMatches);
                        }
                    }

                    var stringTokens = term.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    HashSet<T>? stringResults = null;
                    foreach (var token in stringTokens)
                    {
                        if (_stringIndex.TryGetValue(token, out var tokenSet))
                        {
                            stringResults ??= new HashSet<T>(tokenSet);
                            stringResults.IntersectWith(tokenSet);
                        }
                        else
                        {
                            stringResults = null;
                            break;
                        }
                    }
                    if (stringResults != null)
                        termResults.UnionWith(stringResults);

                    if (termResults.Count == 0)
                        return Enumerable.Empty<T>(); // No matches for this term, short-circuit AND

                    if (firstTerm)
                    {
                        results.UnionWith(termResults);
                        firstTerm = false;
                    }
                    else
                    {
                        results.IntersectWith(termResults); // Narrow down to items matching all terms
                    }
                }
            }
            else
            {
                // OR: Union results for any term
                foreach (var term in searchTerms)
                {
                    if (int.TryParse(term, out int intValue))
                    {
                        if (_intIndex.TryGetValue(intValue, out var intMatches))
                        {
                            results.UnionWith(intMatches);
                        }
                    }

                    var stringTokens = term.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    HashSet<T>? stringResults = null;
                    foreach (var token in stringTokens)
                    {
                        if (_stringIndex.TryGetValue(token, out var tokenSet))
                        {
                            stringResults ??= new HashSet<T>(tokenSet);
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
                }

                // If no pre-filter matches, start with all items
                if (results.Count == 0)
                    results.UnionWith(_items);
            }

            // Final filter for exact matches
            return results.Where(item => useAndCondition
                ? searchTerms.All(term => ContainsMatch(item, term.Trim()))
                : searchTerms.Any(term => ContainsMatch(item, term.Trim())));
        }

        private bool ContainsMatch(T item, string? searchString)
        {
            if (string.IsNullOrEmpty(searchString)) return true; // Empty term matches everything
            return ContainsMatchRecursive(item, item, item.GetType(), searchString, new HashSet<object>());
        }

        private bool ContainsMatchRecursive(T parentItem, object? currentItem, Type type, string searchString, HashSet<object> visited)
        {
            if (currentItem == null || visited.Contains(currentItem)) return false;
            visited.Add(currentItem);

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                object? value = prop.GetValue(currentItem, null);
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
                        string formattedValue = byteValue.Length switch
                        {
                            6 => FormatMacAddress(byteValue),
                            16 => new Guid(byteValue).ToString(),
                            _ => throw new ArgumentException($"Unexpected byte[] length {byteValue.Length} for property {prop.Name}")
                        };
                        if (formattedValue.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                            return true;
                    }
                }
                else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propType) && propType != typeof(string) && propType != typeof(byte[]))
                {
                    if (value is System.Collections.IEnumerable enumerable)
                    {
                        foreach (var element in enumerable)
                        {
                            if (element != null && ContainsMatchRecursive(parentItem, element, element.GetType(), searchString, visited))
                                return true;
                        }
                    }
                }
                else if (!propType.IsPrimitive && propType != typeof(object) && !propType.IsValueType && propType.IsClass)
                {
                    if (ContainsMatchRecursive(parentItem, value, propType, searchString, visited))
                        return true;
                }
            }
            return false;
        }
    }

    // Example Usage
    public abstract class BaseItem
    {
        public string? CommonProperty { get; set; }
    }

    public class ComplexData
    {
        public string? Details { get; set; }
        public int Number { get; set; }
        public byte[]? MacAddress { get; set; }
        public byte[]? GuidBytes { get; set; }
    }

    public class MyItemA : BaseItem
    {
        public string? Name { get; set; }
        public int Value { get; set; }
        public List<ComplexData>? ComplexList { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var items = new List<BaseItem>
        {
            new MyItemA
            {
                CommonProperty = "Shared",
                Name = "Apple",
                Value = 42,
                ComplexList = new List<ComplexData>
                {
                    new ComplexData
                    {
                        Details = "Tasty Fruit",
                        Number = 100,
                        MacAddress = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 },
                        GuidBytes = Guid.Parse("550e8400-e29b-41d4-a716-446655440000").ToByteArray()
                    },
                    new ComplexData
                    {
                        Details = "Crisp",
                        Number = 50,
                        MacAddress = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF }
                    }
                }
            }
        };

            var searcher = new OptimizedSearcher<BaseItem>(items);
            var results = searcher.Search("Tasty,00:11"); // Must match both "Tasty" and "00:11"
            foreach (var item in results)
            {
                if (item is MyItemA a)
                    Console.WriteLine($"MyItemA: {a.Name} - {string.Join(", ", a.ComplexList!.Select(c => c.Details))}");
            }
        }
    }
}