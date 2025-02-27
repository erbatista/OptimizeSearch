using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Markup;

namespace WpfApp1.Types
{

    public class MessageFactory
    {
        private static readonly string AssemblyName = "DynamicMessages";
        private static readonly string ModuleName = "DynamicMessagesModule";
        private static readonly string AppNamespace = "YourAppNamespace"; // Match your app's XAML namespace

        public static Dictionary<string, Type> CreateMessageViewModelTypes(string assemblyPath)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var messageTypes = assembly.GetTypes()
                .Where(t => t.IsClass &&
                           t.Namespace == "lib.messages.definition" &&
                           !t.IsAbstract)
                .ToList();

            var asmName = new AssemblyName(AssemblyName);
            asmName.Version = new Version(1, 0, 0, 0); // Optional: Versioning for clarity
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(ModuleName);

            var createdTypes = new Dictionary<string, Type>();

            foreach (var messageType in messageTypes)
            {
                var newTypeName = $"{AppNamespace}.{messageType.Name}Vm";
                var typeBuilder = moduleBuilder.DefineType(
                    newTypeName,
                    TypeAttributes.Public | TypeAttributes.Class,
                    typeof(Message<>).MakeGenericType(messageType)
                );

                var ctorBuilder = typeBuilder.DefineConstructor(
                    MethodAttributes.Public,
                    CallingConventions.Standard,
                    Type.EmptyTypes
                );
                var ilGen = ctorBuilder.GetILGenerator();
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
                ilGen.Emit(OpCodes.Ret);

                var createdType = typeBuilder.CreateType();
                createdTypes[messageType.Name] = createdType;
            }

            return createdTypes;
        }

        public static void RegisterDataTemplates(Window window, Dictionary<string, Type> vmTypes)
        {
            var resources = new ResourceDictionary();

            foreach (var kvp in vmTypes)
            {
                var vmType = kvp.Value;
                var typeName = vmType.Name; // e.g., TextMessageVm
                var xaml = $@"
                <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                              DataType='{{x:Type local:{typeName}}}'>
                    <TextBlock Text='{{Binding Content.ToString}}' />
                </DataTemplate>";

                try
                {
                    // Parse the XAML, providing the dynamic assembly context
                    var template = (DataTemplate)XamlReader.Parse(xaml, new ParserContext
                    {
                        XmlnsDictionary = { { "local", AppNamespace } },
                        XamlTypeMapper = new XamlTypeMapper(new[] { vmType.Assembly.FullName })
                    });
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