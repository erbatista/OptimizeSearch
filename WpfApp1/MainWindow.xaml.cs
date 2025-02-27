using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfApp1.Types;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var assemblyPath = @"path\to\MessageDefinitions.dll";
            var vmTypes = MessageFactory.CreateMessageViewModelTypes(assemblyPath);
            MessageFactory.RegisterDataTemplates(this, vmTypes);

            // Test with some instances
            var instances = new List<object>();
            foreach (var vmType in vmTypes.Values)
            {
                var instance = Activator.CreateInstance(vmType);
                if (instance != null)
                {
                    // Optionally set Content for testing
                    var contentProp = vmType.BaseType.GetProperty("Content");
                    var contentInstance = Activator.CreateInstance(vmType.BaseType.GetGenericArguments()[0]);
                    contentProp.SetValue(instance, contentInstance);
                    instances.Add(instance);
                }
            }

            MyListBox.ItemsSource = instances;
        }
    }
}