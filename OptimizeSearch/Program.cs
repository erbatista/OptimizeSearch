using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimizeSearch
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    // Example Usage
    public class MyItem
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public byte[] Data { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var items = new List<MyItem>
            {
                new MyItem {Name = "Apple", Value = 42, Data = new byte[] {0xCA, 0xFE}},
                new MyItem {Name = "Banana", Value = 123, Data = new byte[] {0xBA, 0xBE}},
                new MyItem {Name = "Cherry", Value = 42, Data = new byte[] {0xDE, 0xAD}}
            };

            var searcher = new OptimizedSearcher<MyItem>(items);
            var results = searcher.Search("42");
            foreach (var item in results)
            {
                Console.WriteLine($"Found");
            }
        }
    }
}
