using WpfApp1.Types;

namespace WpfApp1
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            string assemblyPath = @"path\to\MessageDefinitions.dll";
            var vmTypes = MessageFactory.CreateMessageViewModelTypes(assemblyPath);
            MessageFactory.RegisterDataTemplates(this, vmTypes);

            // Create instances for testing
            var instances = new List<object>();
            foreach (var vmType in vmTypes.Values)
            {
                object instance = Activator.CreateInstance(vmType);
                if (instance != null)
                {
                    var contentProp = vmType.BaseType.GetProperty("Content");
                    var contentType = vmType.BaseType.GetGenericArguments()[0];
                    var contentInstance = Activator.CreateInstance(contentType);
                    contentProp.SetValue(instance, contentInstance);
                    instances.Add(instance);
                }
            }

            MyListBox.ItemsSource = instances;
        }
    }
}