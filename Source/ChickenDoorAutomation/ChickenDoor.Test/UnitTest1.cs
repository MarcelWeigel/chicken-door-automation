using System;
using System.Threading.Tasks;
using CoordinateSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChickenDoor.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            var coord = new Coordinate(49.00262, 8.5220912, DateTime.UtcNow.Date);
            Console.WriteLine($"Sun rises at {coord.CelestialInfo.SunRise} and goes down at {coord.CelestialInfo.SunSet}");
        }
    }
}
