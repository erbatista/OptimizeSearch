using System;
using System.Collections.Generic;

namespace OptimizeSearch
{
    // Example Usage with Complex Properties
    public abstract class BaseItem
    {
        public string CommonProperty { get; set; }
    }

    public class ComplexData
    {
        public string Details { get; set; }
        public int Number { get; set; }
        public byte[] ExtraData { get; set; }
    }

    public class MyItemA : BaseItem
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public ComplexData ComplexProperty { get; set; }
    }

    public class MyItemB : BaseItem
    {
        public byte[] Data { get; set; }
        public string Description { get; set; }
        public ComplexData NestedComplex { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var items = new List<BaseItem>
            {
                new MyItemA
                {
                    CommonProperty = "Shared",
                    Name = "Apple",
                    Value = 42,
                    ComplexProperty = new ComplexData { Details = "Tasty", Number = 100, ExtraData = new byte[] { 0xDE, 0xAD } }
                },
                new MyItemB
                {
                    CommonProperty = "Shared",
                    Data = new byte[] { 0xCA, 0xFE },
                    Description = "Cafe",
                    NestedComplex = new ComplexData { Details = "Rich", Number = 200, ExtraData = new byte[] { 0xBE, 0xEF } }
                }
            };

            var searcher = new OptimizedSearcher<BaseItem>(items);
            var results = searcher.Search("Tasty");
            foreach (var item in results)
            {
                if (item is MyItemA a)
                    Console.WriteLine($"MyItemA: {a.Name} - {a.ComplexProperty.Details}");
                else if (item is MyItemB b)
                    Console.WriteLine($"MyItemB: {b.Description} - {b.NestedComplex.Details}");
            }
        }
    }
}