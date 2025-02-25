using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimizeSearch
{
#nullable enable

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    // Example Usage
    public abstract class BaseItem
    {
        public string? CommonProperty { get; set; }
    }

    public class ComplexData
    {
        public string? Details { get; set; }
        public int Number { get; set; }
        public byte[]? MacAddress { get; set; }
        public byte[]? GuidBytes { get; set; }
    }

    public class MyItemA : BaseItem
    {
        public string? Name { get; set; }
        public int Value { get; set; }
        public List<ComplexData>? ComplexList { get; set; }
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
                ComplexList = new List<ComplexData>
                {
                    new ComplexData
                    {
                        Details = "Tasty Fruit",
                        Number = 100,
                        MacAddress = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 },
                        GuidBytes = Guid.Parse("550e8400-e29b-41d4-a716-446655440000").ToByteArray()
                    },
                    new ComplexData
                    {
                        Details = "Crisp",
                        Number = 50,
                        MacAddress = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF }
                    }
                }
            }
        };

            var searcher = new OptimizedSearcher<BaseItem>(items);
            var results = searcher.Search("Tasty,00:11"); // Must match both "Tasty" and "00:11"
            foreach (var item in results)
            {
                if (item is MyItemA a)
                    Console.WriteLine($"MyItemA: {a.Name} - {string.Join(", ", a.ComplexList!.Select(c => c.Details))}");
            }
        }
    }
}