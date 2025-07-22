using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class MyViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<MyItem> _fullCollection;
        public ObservableCollection<MyItem> FullCollection
        {
            get => _fullCollection;
            set
            {
                _fullCollection = value;
                OnPropertyChanged(nameof(FullCollection));
                OnPropertyChanged(nameof(FirstThreeItems)); // Update derived property
            }
        }

        // Property exposing only the first three items
        public IEnumerable<MyItem> FirstThreeItems => FullCollection?.Take(3);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
