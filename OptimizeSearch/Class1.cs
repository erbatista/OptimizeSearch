namespace OptimizeSearch
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;

    namespace YourNamespace
    {
        public partial class FilteredDataGrid : UserControl
        {
            #region Dependency Properties

            // ItemsSource Property
            public static readonly DependencyProperty ItemsSourceProperty =
                DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(FilteredDataGrid),
                    new PropertyMetadata(null, OnItemsSourceChanged));

            public IEnumerable ItemsSource
            {
                get => (IEnumerable)GetValue(ItemsSourceProperty);
                set => SetValue(ItemsSourceProperty, value);
            }

            // FilterText Property
            public static readonly DependencyProperty FilterTextProperty =
                DependencyProperty.Register(nameof(FilterText), typeof(string), typeof(FilteredDataGrid),
                    new PropertyMetadata(string.Empty, OnFilterTextChanged));

            public string FilterText
            {
                get => (string)GetValue(FilterTextProperty);
                set => SetValue(FilterTextProperty, value);
            }

            // FilteredItems (read-only)
            private static readonly DependencyPropertyKey FilteredItemsPropertyKey =
                DependencyProperty.RegisterReadOnly(nameof(FilteredItems), typeof(ICollectionView), typeof(FilteredDataGrid),
                    new PropertyMetadata(null));

            public static readonly DependencyProperty FilteredItemsProperty = FilteredItemsPropertyKey.DependencyProperty;

            public ICollectionView FilteredItems
            {
                get => (ICollectionView)GetValue(FilteredItemsProperty);
                private set => SetValue(FilteredItemsPropertyKey, value);
            }

            #endregion

            public FilteredDataGrid()
            {
                InitializeComponent();
            }

            #region Property Changed Callbacks

            private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                if (d is FilteredDataGrid control)
                {
                    control.UpdateFilteredItems();
                }
            }

            private static void OnFilterTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                if (d is FilteredDataGrid control)
                {
                    control.FilteredItems?.Refresh();
                }
            }

            #endregion

            private void UpdateFilteredItems()
            {
                if (ItemsSource != null)
                {
                    FilteredItems = CollectionViewSource.GetDefaultView(ItemsSource);
                    FilteredItems.Filter = FilterPredicate;
                }
                else
                {
                    FilteredItems = null;
                }
            }

            private bool FilterPredicate(object item)
            {
                if (string.IsNullOrEmpty(FilterText))
                    return true;

                // Customize this logic based on your data type and filtering needs
                return item.ToString().Contains(FilterText, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}