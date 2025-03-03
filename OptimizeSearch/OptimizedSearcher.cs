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
        private readonly IEnumerable<T> _items;

        public OptimizedSearcher(IEnumerable<T> items)
        {
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _stringIndex = new Dictionary<string, HashSet<T>>(StringComparer.OrdinalIgnoreCase);

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
                else if (propType == typeof(int) || propType == typeof(uint) || propType == typeof(double))
                {
                    string strValue = value.ToString()!;
                    lock (_stringIndex)
                    {
                        if (!_stringIndex.TryGetValue(strValue, out var set))
                        {
                            set = new HashSet<T>();
                            _stringIndex[strValue] = set;
                        }
                        set.Add(parentItem);
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
                            if (element == null) continue;

                            Type elementType = element.GetType();
                            if (elementType == typeof(string))
                            {
                                if (element is string strElement && !string.IsNullOrEmpty(strElement))
                                {
                                    var words = strElement.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
                            else if (elementType == typeof(int) || elementType == typeof(uint) || elementType == typeof(double))
                            {
                                string strElement = element.ToString()!;
                                lock (_stringIndex)
                                {
                                    if (!_stringIndex.TryGetValue(strElement, out var set))
                                    {
                                        set = new HashSet<T>();
                                        _stringIndex[strElement] = set;
                                    }
                                    set.Add(parentItem);
                                }
                            }
                            else if (elementType == typeof(byte[]))
                            {
                                if (element is byte[] byteElement && byteElement.Length > 0)
                                {
                                    string strElement = byteElement.Length switch
                                    {
                                        6 => FormatMacAddress(byteElement),
                                        16 => new Guid(byteElement).ToString(),
                                        _ => throw new ArgumentException($"Unexpected byte[] length {byteElement.Length}")
                                    };

                                    var words = strElement.Split(new[] { ' ', '-', ':' }, StringSplitOptions.RemoveEmptyEntries);
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
                            else if (!elementType.IsPrimitive && elementType != typeof(object) && !elementType.IsValueType && elementType.IsClass)
                            {
                                IndexItem(parentItem, element, elementType, visited);
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

            var searchTerms = searchString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (searchTerms.Length == 0) return _items;

            var results = new HashSet<T>();

            if (useAndCondition)
            {
                // AND: Intersect sets for each term
                bool firstTerm = true;
                foreach (var term in searchTerms)
                {
                    var trimmedTerm = term.Trim();
                    var termResults = new HashSet<T>();

                    // Split term into tokens and union all matches
                    var stringTokens = trimmedTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    bool hasMatches = false;
                    foreach (var token in stringTokens)
                    {
                        if (_stringIndex.TryGetValue(token, out var tokenSet))
                        {
                            termResults.UnionWith(tokenSet);
                            hasMatches = true;
                        }
                    }

                    // Add exact term match (e.g., "3.14" or "00:11")
                    if (_stringIndex.TryGetValue(trimmedTerm, out var exactSet))
                    {
                        termResults.UnionWith(exactSet);
                        hasMatches = true;
                    }

                    if (!hasMatches)
                        return Enumerable.Empty<T>(); // No matches for this term, short-circuit AND

                    if (firstTerm)
                    {
                        results.UnionWith(termResults);
                        firstTerm = false;
                    }
                    else
                    {
                        results.IntersectWith(termResults); // Narrow to items matching all terms so far
                    }
                }

                // Final exact substring check
                return results.Where(item => searchTerms.All(term => ContainsMatch(item, term.Trim())));
            }
            else
            {
                // OR: Union all matches across terms
                foreach (var term in searchTerms)
                {
                    var trimmedTerm = term.Trim();
                    var stringTokens = trimmedTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var token in stringTokens)
                    {
                        if (_stringIndex.TryGetValue(token, out var tokenSet))
                        {
                            results.UnionWith(tokenSet);
                        }
                    }

                    // Include exact term match
                    if (_stringIndex.TryGetValue(trimmedTerm, out var exactSet))
                    {
                        results.UnionWith(exactSet);
                    }
                }

                if (results.Count == 0)
                    results.UnionWith(_items);

                return results.Where(item => searchTerms.Any(term => ContainsMatch(item, term.Trim())));
            }
        }

        private bool ContainsMatch(T item, string? searchString)
        {
            if (string.IsNullOrEmpty(searchString)) return true;
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
                else if (propType == typeof(int) || propType == typeof(uint) || propType == typeof(double))
                {
                    if (value.ToString() is string strValue && strValue.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
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
                            if (element == null) continue;

                            Type elementType = element.GetType();
                            if (elementType == typeof(string))
                            {
                                if (element is string strElement && strElement != null && strElement.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                                    return true;
                            }
                            else if (elementType == typeof(int) || elementType == typeof(uint) || elementType == typeof(double))
                            {
                                if (element.ToString() is string strElement && strElement.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                                    return true;
                            }
                            else if (elementType == typeof(byte[]))
                            {
                                if (element is byte[] byteElement && byteElement != null)
                                {
                                    string formattedElement = byteElement.Length switch
                                    {
                                        6 => FormatMacAddress(byteElement),
                                        16 => new Guid(byteElement).ToString(),
                                        _ => throw new ArgumentException($"Unexpected byte[] length {byteElement.Length}")
                                    };
                                    if (formattedElement.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                                        return true;
                                }
                            }
                            else if (!elementType.IsPrimitive && elementType != typeof(object) && !elementType.IsValueType && elementType.IsClass)
                            {
                                if (ContainsMatchRecursive(parentItem, element, elementType, searchString, visited))
                                    return true;
                            }
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
}