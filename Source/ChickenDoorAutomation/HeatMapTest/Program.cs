using System;
using System.Drawing;
using Driver;

namespace HeatMapTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Color c = HeatMap.ColorFromTemperature(5);

            Console.WriteLine(c);
        }
    }
}
