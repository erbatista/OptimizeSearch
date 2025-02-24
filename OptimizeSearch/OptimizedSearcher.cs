﻿#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System;

namespace OptimizeSearch
{
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
                if (item is not null)
                {
                    IndexItem(item, item, item.GetType());
                }
            });
        }

        private void IndexItem(T parentItem, 
            object? currentItem, 
            Type type, 
            HashSet<object>? visited = null)
        {
            visited ??= new HashSet<object>();
            if (currentItem == null || !visited.Add(currentItem)) return;

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                var value = prop.GetValue(currentItem, null);
                if (value == null) continue;

                var propType = prop.PropertyType;

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
                        var strValue = byteValue.Length switch
                        {
                            <= 6 => FormatMacAddress(byteValue),
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
            if (bytes.Length > 6) throw new ArgumentException("MAC address must be 6 bytes.");
            return string.Join(":", bytes.Select(b => b.ToString("X2")));
        }

        public IEnumerable<T> Search(string? searchString)
        {
            if (string.IsNullOrEmpty(searchString)) return _items;

            // Split search string by comma for multiple terms
            var searchTerms = searchString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (searchTerms.Length == 0) return _items;

            var results = new HashSet<T>();

            // Pre-filter using indexes for the first term
            if (int.TryParse(searchTerms[0], out int intValue))
            {
                if (_intIndex.TryGetValue(intValue, out var intMatches))
                {
                    results.UnionWith(intMatches);
                }
            }

            var stringTokens = searchTerms[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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

            // If no pre-filter results, start with all items
            if (results.Count == 0 && stringResults == null)
                results.UnionWith(_items);

            // Filter for all terms (exact matching)
            return results.Where(item => searchTerms.All(term => ContainsMatch(item, term.Trim()))); // Trim to remove extra spaces
        }

        private bool ContainsMatch(T? item, string searchString) =>
            item is not null 
            && ContainsMatchRecursive(item, item, item.GetType(), searchString, new HashSet<object>());

        private bool ContainsMatchRecursive(T parentItem, object? currentItem, Type type, string? searchString, HashSet<object> visited)
        {
            if (currentItem == null || !visited.Add(currentItem)) return false;

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                var value = prop.GetValue(currentItem, null);
                if (value == null) continue;

                var propType = prop.PropertyType;

                if (propType == typeof(string))
                {
                    if (value is string strValue && strValue.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
                else if (propType == typeof(int))
                {
                    if (int.TryParse(searchString, out var intSearch) && value is int intValue && intValue == intSearch)
                        return true;
                }
                else if (propType == typeof(byte[]))
                {
                    if (value is byte[] byteValue)
                    {
                        var formattedValue = byteValue.Length switch
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
}