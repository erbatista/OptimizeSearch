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
                else if (propType == typeof(int) || propType == typeof(uint) || propType == typeof(long) || propType == typeof(ulong) || propType == typeof(double))
                {
                    string strValue = propType == typeof(double) ? ((double)value).ToString("F2") : value.ToString()!;
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
                            else if (elementType == typeof(int) || elementType == typeof(uint) || elementType == typeof(long) || elementType == typeof(ulong) || elementType == typeof(double))
                            {
                                string strElement = elementType == typeof(double) ? ((double)element).ToString("F2") : element.ToString()!;
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
            return await Task.Run(async () =>
            {
                if (string.IsNullOrEmpty(searchString)) return _items;

                var searchTerms = searchString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (searchTerms.Length == 0) return _items;

                var termResultsList = new List<HashSet<T>>(searchTerms.Length);

                await Task.WhenAll(searchTerms.Select(term =>
                    Task.Run(() =>
                    {
                        var trimmedTerm = term.Trim();
                        var termResults = new HashSet<T>();

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

                        lock (termResultsList) termResultsList.Add(termResults);
                    })
                ));

                var results = new HashSet<T>();

                if (useAndCondition)
                {
                    bool firstTerm = true;
                    foreach (var termResults in termResultsList)
                    {
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
                    foreach (var termResults in termResultsList)
                    {
                        results.UnionWith(termResults);
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
                }
                else if (propType == typeof(uint))
                {
                    if (uint.TryParse(searchString, out uint uintSearch) && value is uint uintValue && uintValue == uintSearch)
                        return true;
                }
                else if (propType == typeof(long))
                {
                    if (long.TryParse(searchString, out long longSearch) && value is long longValue && longValue == longSearch)
                        return true;
                }
                else if (propType == typeof(ulong))
                {
                    if (ulong.TryParse(searchString, out ulong ulongSearch) && value is ulong ulongValue && ulongValue == ulongSearch)
                        return true;
                }
                else if (propType == typeof(double))
                {
                    if (double.TryParse(searchString, out double doubleSearch) && value is double doubleValue && doubleValue == doubleSearch)
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
                            }
                            else if (elementType == typeof(uint))
                            {
                                if (uint.TryParse(searchString, out uint uintSearch) && element is uint uintElement && uintElement == uintSearch)
                                    return true;
                            }
                            else if (elementType == typeof(long))
                            {
                                if (long.TryParse(searchString, out long longSearch) && element is long longElement && longElement == longSearch)
                                    return true;
                            }
                            else if (elementType == typeof(ulong))
                            {
                                if (ulong.TryParse(searchString, out ulong ulongSearch) && element is ulong ulongElement && ulongElement == ulongSearch)
                                    return true;
                            }
                            else if (elementType == typeof(double))
                            {
                                if (double.TryParse(searchString, out double doubleSearch) && element is double doubleElement && doubleElement == doubleSearch)
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