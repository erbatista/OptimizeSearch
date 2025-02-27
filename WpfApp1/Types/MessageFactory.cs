using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Markup;

namespace WpfApp1.Types
{

    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Linq;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Markup;
    using System.Windows.Controls;

    public class MessageFactory
    {
        private static readonly string AssemblyName = "DynamicMessages";
        private static readonly string ModuleName = "DynamicMessagesModule";
        private static readonly string AppNamespace = "YourAppNamespace"; // Match your app's XAML namespace

        public static Dictionary<string, Type> CreateMessageViewModelTypes(string assemblyPath)
        {
            // Load the external assembly
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            var messageTypes = assembly.GetTypes()
                .Where(t => t.IsClass &&
                           t.Namespace == "lib.messages.definition" &&
                           !t.IsAbstract)
                .ToList();

            // Define a dynamic assembly (in-memory only)
            AssemblyName asmName = new AssemblyName(AssemblyName);
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(ModuleName);

            var createdTypes = new Dictionary<string, Type>();

            foreach (var messageType in messageTypes)
            {
                string newTypeName = $"{AppNamespace}.{messageType.Name}Vm";
                TypeBuilder typeBuilder = moduleBuilder.DefineType(
                    newTypeName,
                    TypeAttributes.Public | TypeAttributes.Class,
                    typeof(Message<>).MakeGenericType(messageType)
                );

                ConstructorBuilder ctorBuilder = typeBuilder.DefineConstructor(
                    MethodAttributes.Public,
                    CallingConventions.Standard,
                    Type.EmptyTypes
                );
                ILGenerator ilGen = ctorBuilder.GetILGenerator();
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
                ilGen.Emit(OpCodes.Ret);

                Type createdType = typeBuilder.CreateType();
                createdTypes[messageType.Name] = createdType;
            }

            return createdTypes;
        }

        public static void RegisterDataTemplates(Window window, Dictionary<string, Type> vmTypes)
        {
            ResourceDictionary resources = new ResourceDictionary();

            foreach (var kvp in vmTypes)
            {
                Type vmType = kvp.Value;
                string typeName = vmType.Name; // e.g., TextMessageVm
                string xaml = $@"
                <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                              DataType='{{x:Type local:{typeName}}}'>
                    <TextBlock Text='{{Binding Content.ToString}}' />
                </DataTemplate>";

                try
                {
                    var parserContext = new ParserContext
                    {
                        XmlnsDictionary = { { "local", AppNamespace } },
                        XamlTypeMapper = new XamlTypeMapper(new[] { vmType.Assembly.FullName })
                    };
                    var template = (DataTemplate)XamlReader.Parse(xaml, parserContext);
                    resources.Add(new DataTemplateKey(vmType), template);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to parse template for {typeName}: {ex.Message}");
                }
            }

            window.Resources.MergedDictionaries.Add(resources);
        }
    }
}