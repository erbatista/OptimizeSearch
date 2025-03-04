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
                        lock (_stringIndex)
                        {
                            if (!_stringIndex.TryGetValue(strValue, out var fullSet))
                            {
                                fullSet = new HashSet<T>();
                                _stringIndex[strValue] = fullSet;
                            }
                            fullSet.Add(parentItem);

                            var words = strValue.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
                        string strValue = intValue.ToString();
                        lock (_stringIndex)
                        {
                            if (!_stringIndex.TryGetValue(strValue, out var strSet))
                            {
                                strSet = new HashSet<T>();
                                _stringIndex[strValue] = strSet;
                            }
                            strSet.Add(parentItem);
                        }
                    }
                }
                else if (propType == typeof(byte[]))
                {
                    if (value is byte[] byteValue && byteValue.Length > 0)
                    {
                        string strValue = FormatByteArray(byteValue, prop.Name);

                        lock (_stringIndex)
                        {
                            if (!_stringIndex.TryGetValue(strValue, out var fullSet))
                            {
                                fullSet = new HashSet<T>();
                                _stringIndex[strValue] = fullSet;
                            }
                            fullSet.Add(parentItem);

                            var words = strValue.Split(new[] { ' ', '-', ':' }, StringSplitOptions.RemoveEmptyEntries);
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
                                    lock (_stringIndex)
                                    {
                                        if (!_stringIndex.TryGetValue(strElement, out var fullSet))
                                        {
                                            fullSet = new HashSet<T>();
                                            _stringIndex[strElement] = fullSet;
                                        }
                                        fullSet.Add(parentItem);

                                        var words = strElement.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
                            else if (elementType == typeof(int))
                            {
                                if (element is int intElement)
                                {
                                    lock (_intIndex)
                                    {
                                        if (!_intIndex.TryGetValue(intElement, out var set))
                                        {
                                            set = new HashSet<T>();
                                            _intIndex[intElement] = set;
                                        }
                                        set.Add(parentItem);
                                    }
                                    string strElement = intElement.ToString();
                                    lock (_stringIndex)
                                    {
                                        if (!_stringIndex.TryGetValue(strElement, out var strSet))
                                        {
                                            strSet = new HashSet<T>();
                                            _stringIndex[strElement] = strSet;
                                        }
                                        strSet.Add(parentItem);
                                    }
                                }
                            }
                            else if (elementType == typeof(byte[]))
                            {
                                if (element is byte[] byteElement && byteElement.Length > 0)
                                {
                                    string strElement = FormatByteArray(byteElement, nameof(element));

                                    lock (_stringIndex)
                                    {
                                        if (!_stringIndex.TryGetValue(strElement, out var fullSet))
                                        {
                                            fullSet = new HashSet<T>();
                                            _stringIndex[strElement] = fullSet;
                                        }
                                        fullSet.Add(parentItem);

                                        var words = strElement.Split(new[] { ' ', '-', ':' }, StringSplitOptions.RemoveEmptyEntries);
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

        private string FormatByteArray(byte[] byteValue, string propertyName)
        {
            return byteValue.Length switch
            {
                6 => FormatMacAddress(byteValue),
                16 => new Guid(byteValue).ToString(),
                _ => BitConverter.ToString(byteValue).Replace("-", "")
            };
        }

        public async Task<IEnumerable<T>> SearchAsync(string? searchString, bool useAndCondition = true)
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(searchString)) return _items;

                var searchTerms = searchString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (searchTerms.Length == 0) return _items;

                var results = new HashSet<T>();

                if (useAndCondition)
                {
                    bool firstTerm = true;
                    foreach (var term in searchTerms)
                    {
                        var trimmedTerm = term.Trim();
                        var termResults = new HashSet<T>();

                        if (int.TryParse(trimmedTerm, out int intValue) && _intIndex.TryGetValue(intValue, out var intMatches))
                        {
                            termResults.UnionWith(intMatches);
                        }

                        if (_stringIndex.TryGetValue(trimmedTerm, out var exactSet))
                        {
                            termResults.UnionWith(exactSet);
                        }

                        var stringTokens = trimmedTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var token in stringTokens)
                        {
                            if (_stringIndex.TryGetValue(token, out var tokenSet))
                            {
                                termResults.UnionWith(tokenSet);
                            }
                        }

                        if (termResults.Count == 0)
                        {
                            termResults.UnionWith(_items);
                        }

                        if (firstTerm)
                        {
                            results.UnionWith(termResults);
                            firstTerm = false;
                        }
                        else
                        {
                            results.IntersectWith(termResults);
                        }
                    }

                    return results.Where(item => searchTerms.All(term => ContainsMatch(item, term.Trim())));
                }
                else
                {
                    foreach (var term in searchTerms)
                    {
                        var trimmedTerm = term.Trim();

                        if (int.TryParse(trimmedTerm, out int intValue) && _intIndex.TryGetValue(intValue, out var intMatches))
                        {
                            results.UnionWith(intMatches);
                        }

                        if (_stringIndex.TryGetValue(trimmedTerm, out var exactSet))
                        {
                            results.UnionWith(exactSet);
                        }

                        var stringTokens = trimmedTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var token in stringTokens)
                        {
                            if (_stringIndex.TryGetValue(token, out var tokenSet))
                            {
                                results.UnionWith(tokenSet);
                            }
                        }
                    }

                    if (results.Count == 0)
                        results.UnionWith(_items);

                    return results.Where(item => searchTerms.Any(term => ContainsMatch(item, term.Trim())));
                }
            });
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
                else if (propType == typeof(int))
                {
                    if (int.TryParse(searchString, out int intSearch) && value is int intValue && intValue == intSearch)
                        return true;
                    if (value.ToString() is string strValue && strValue.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
                else if (propType == typeof(byte[]))
                {
                    if (value is byte[] byteValue && byteValue != null)
                    {
                        string formattedValue = FormatByteArray(byteValue, prop.Name);
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
                            else if (elementType == typeof(int))
                            {
                                if (int.TryParse(searchString, out int intSearch) && element is int intElement && intElement == intSearch)
                                    return true;
                                if (element.ToString() is string strElement && strElement.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                                    return true;
                            }
                            else if (elementType == typeof(byte[]))
                            {
                                if (element is byte[] byteElement && byteElement != null)
                                {
                                    string formattedElement = FormatByteArray(byteElement, nameof(element));
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