using System;

namespace TestHATTwaffle
{
    class Program
    {
        static void Main(string[] args)
        {
            DataServices _dataServices = new DataServices();

            Console.WriteLine("Enter series");
            string series = Console.ReadLine();

            Console.WriteLine("Enter search");
            string search = Console.ReadLine();

            _dataServices.Search(series, search);
        }
    }
}
