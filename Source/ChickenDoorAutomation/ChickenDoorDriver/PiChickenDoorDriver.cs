using System;
using System.Data.SqlTypes;
using System.Device.Gpio;
using System.Threading;
using Application.Driver;
using Domain.Light;
using FunicularSwitch;

namespace ChickenDoorDriver
{
    public class PiChickenDoorDriver : IChickenDoorDriver
    {
        private const int PinLed = 18;
        private const int PinPhotoelectricBarrier = 15;

        private GpioController _controller;

        private Light _light;
        private bool _isRunning = true;

        public PiChickenDoorDriver()
        {
            Init();
        }

        public void Init()
        {
            Console.WriteLine($"Start {typeof(PiChickenDoorDriver)} init.");
            _controller = new GpioController();
            _controller.OpenPin(PinLed, PinMode.Output);
            _controller.OpenPin(PinPhotoelectricBarrier, PinMode.Input);

            _light = new Light(LightMode.Manuel, LightState.Off);
            SetLightSignal();

            var thread = new Thread(Process);
            thread.Start();
            Console.WriteLine($"Finished {typeof(PiChickenDoorDriver)} init.");
        }

        private void Process()
        {
            Console.WriteLine($"I'm the watcher task hahah.");
            while (_isRunning)
            {
                if (_light.LightMode == LightMode.Auto)
                {
                    var input = _controller.Read(PinPhotoelectricBarrier);
                    _light.LightState = input == PinValue.High ? LightState.Off : LightState.On;

                    SetLightSignal();
                }

                Thread.Sleep(100);
            }
        }

        public Result<Light> GetLight()
        {
            return _light;
        }

        public Result<Unit> TurnLightOn()
        {
            _light.LightState = LightState.On;
            try
            {
                Console.WriteLine($"Set light to {_light.LightState}.");
                SetLightSignal();
            }
            catch (Exception e)
            {
                return Result.Error<Unit>(e.Message);
            }

            return Unit.Instance;
        }

        public Result<Unit> TurnLightOff()
        {
            _light.LightState = LightState.Off;
            try
            {
                Console.WriteLine($"Set light to {_light.LightState}.");
                SetLightSignal();
            }
            catch (Exception e)
            {
                return Result.Error<Unit>(e.Message);
            }
            

            return Unit.Instance;
        }

        private void SetLightSignal()
        {
            _controller.Write(PinLed, _light.LightState == LightState.On);
        }

        public Result<Unit> SwitchToManuel()
        {
            _light.LightMode = LightMode.Manuel;

            return Unit.Instance;
        }

        public Result<Unit> SwitchToAuto()
        {
            _light.LightMode = LightMode.Auto;

            return Unit.Instance;
        }

        public void Dispose()
        {
            _isRunning = false;
            _controller.Dispose();
        }
    }
}
