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
        /*
         Improve UI/UX Performance: Enhance the responsiveness of the WPF application by optimizing XAML layouts and reducing rendering times by 20% within the next quarter.
           Master MVVM Implementation: Fully adopt or refine the Model-View-ViewModel (MVVM) pattern in the product’s codebase, ensuring at least 90% of new features follow this architecture by the end of the year.
           Reduce Bugs in Key Features: Identify and resolve 10 high-priority bugs or performance issues in the product’s core functionality, improving user satisfaction based on feedback metrics.
           Enhance Accessibility: Implement accessibility features (e.g., screen reader support, keyboard navigation) in at least two major modules of the product to comply with WCAG standards by mid-2025.
           Automate Testing: Develop or expand unit tests for the WPF application, achieving at least 70% code coverage for critical components within six months.
           Optimize Data Binding: Refactor data-binding logic in the product to improve performance and maintainability, targeting a 15% reduction in memory usage during key workflows.
           Learn and Integrate a New WPF Feature: Research and implement a modern WPF capability (e.g., advanced animations, custom controls, or .NET Core upgrades) into the product to enhance its visual appeal or functionality by Q3 2025.
           Collaborate on Feature Development: Work with the team to design and deliver a new product feature (e.g., a dashboard, reporting tool, or integration) from concept to release within the next development cycle.
           Document Code and Processes: Create or update technical documentation for at least three key areas of the WPF codebase, improving onboarding efficiency for new developers by year-end.
           Mentor or Share Knowledge: Conduct two knowledge-sharing sessions (e.g., lunch-and-learn or code review) with the team on WPF best practices or lessons learned from the product, fostering team growth.
         */
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