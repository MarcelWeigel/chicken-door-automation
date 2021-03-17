using System;
using System.Device.Gpio;
using System.Threading;

namespace ChickenDoorAutomation
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Blinking LED. Press Ctrl+C to end.");
            int pin_LED = 18;
            int pin_PhotoelectricBarrier = 15;
            using var controller = new GpioController();
            controller.OpenPin(pin_LED, PinMode.Output);
            controller.OpenPin(pin_PhotoelectricBarrier, PinMode.Input);
            while (true)
            {
                var input = controller.Read(pin_PhotoelectricBarrier);
                controller.Write(pin_LED, input);
                
                Thread.Sleep(100);
            }
        }
    }
}
