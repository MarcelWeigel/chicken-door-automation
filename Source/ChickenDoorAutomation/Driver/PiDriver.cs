using System;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.I2c;
using System.Threading;
using System.Threading.Tasks;
using Application.Driver;
using ChickenDoorDriver;
using FunicularSwitch;
using Iot.Device.Bh1750fvi;
using Iot.Device.Bmxx80;
using Iot.Device.Bmxx80.PowerMode;
using Iot.Device.Vl53L0X;

namespace Driver
{
    public class PiDriver : IDriver
    {
        class Pin
        {
            public const int MotorLeft = 24;
            public const int MotorRight = 23;
            public const int MotorLeftEnable = 25;
            public const int MotorRightEnable = 18;
            public const int HallBottom = 17;
            public const int HallTop = 27;
            public const int PhotoelectricBarrier = 22;
        }

        private double _currentSpeed = 1;
        private DoorDirection _currentDirection = DoorDirection.None;
        private const double UpSpeed = 0.5; //0.70;
        private const double DownSpeed = 0.1; //0.35;

        private GpioController _controller;
        private bool _isRunning;

        private CancellationTokenSource _tokenSource;

        //private PwmChannel _pwmMotorLeft; 
        //private PwmChannel _pwmMotorRight; 

        private Bme280 _bme280;
        private int _measurementTime;
        private Vl53L0X _vl53L0X;
        private Bh1750fvi _bh1750Fvi;

        public Result<Unit> Init()
        {
            _controller = new GpioController(PinNumberingScheme.Logical, new RaspberryPi3Driver());
            _controller.OpenPin(Pin.HallBottom, PinMode.InputPullUp);
            _controller.OpenPin(Pin.HallTop, PinMode.InputPullUp);
            _controller.OpenPin(Pin.PhotoelectricBarrier, PinMode.Input);
            _controller.OpenPin(Pin.MotorLeftEnable, PinMode.Output);
            _controller.OpenPin(Pin.MotorRightEnable, PinMode.Output);
            _controller.OpenPin(Pin.MotorLeft, PinMode.Output);
            _controller.OpenPin(Pin.MotorRight, PinMode.Output);

            //_pwmMotorLeft = PwmChannel.Create(0, Pin.MotorLeftEnable, 500, UpSpeed);
            //_pwmMotorRight = PwmChannel.Create(18, 0, 500, DownSpeed);

            _controller.Write(Pin.MotorLeftEnable, PinValue.High);
            _controller.Write(Pin.MotorRightEnable, PinValue.High);
            _controller.Write(Pin.MotorLeft, PinValue.Low);
            _controller.Write(Pin.MotorRight, PinValue.Low);


            Console.WriteLine($"Init sensor");

            _bh1750Fvi = new Bh1750fvi(I2cDevice.Create(new I2cConnectionSettings(1, Bh1750fviExtenstion.DefaultI2cAddress)));

            _vl53L0X = new Vl53L0X(I2cDevice.Create(new I2cConnectionSettings(1, Vl53L0X.DefaultI2cAddress)));

            _bme280 = new Bme280(I2cDevice.Create(new I2cConnectionSettings(1, Bme280.SecondaryI2cAddress)));

            _measurementTime = _bme280.GetMeasurementDuration();

            _bme280.SetPowerMode(Bmx280PowerMode.Forced);
            Thread.Sleep(_measurementTime);

            _bme280.TryReadTemperature(out var tempValue);
            _bme280.TryReadPressure(out var preValue);
            _bme280.TryReadHumidity(out var humValue);
            _bme280.TryReadAltitude(out var altValue);

            Console.WriteLine($"Temperature: {tempValue.DegreesCelsius:0.#}\u00B0C");
            Console.WriteLine($"Pressure: {preValue.Hectopascals:#.##} hPa");
            Console.WriteLine($"Relative humidity: {humValue.Percent:#.##}%");
            Console.WriteLine($"Estimated altitude: {altValue.Meters:#} m");


            Console.WriteLine($"Finished Init sensor");

            Run();

            return Unit.Instance;
        }

        private void Run()
        {
            _tokenSource = new CancellationTokenSource();
            CancellationToken ct = _tokenSource.Token;

            var task = Task.Run(() =>
            {
                _isRunning = true;

                //_pwmMotorLeft.Start();
                //_pwmMotorRight.Start();

                while (_isRunning)
                {
                    if (ct.IsCancellationRequested)
                    {
                        _controller.Write(Pin.MotorLeft, PinValue.Low);
                        _controller.Write(Pin.MotorRight, PinValue.Low);
                        _currentDirection = DoorDirection.None;
                        //_pwmMotorLeft.Stop();
                        //_pwmMotorRight.Stop();
                        _isRunning = false;
                    }
                    if (_controller.Read(Pin.HallTop) == PinValue.Low)
                    {
                        _controller.Write(Pin.MotorLeft, PinValue.Low);
                        _currentDirection = DoorDirection.None;
                    }
                    if (_controller.Read(Pin.HallBottom) == PinValue.Low)
                    {
                        _controller.Write(Pin.MotorRight, PinValue.Low);
                        _currentDirection = DoorDirection.None;
                    }

                    if (_controller.Read(Pin.PhotoelectricBarrier) == PinValue.Low)
                    {
                        if (_currentDirection == DoorDirection.Down)
                        {
                            _controller.Write(Pin.MotorRight, PinValue.Low);
                            _controller.Write(Pin.MotorLeft, PinValue.High);
                            _currentDirection = DoorDirection.Up;
                        }
                    }

                    try
                    {
                        Console.WriteLine($"Distance: '{_vl53L0X.Distance}'");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    try
                    {
                        Console.WriteLine($"Light: '{_bh1750Fvi.Illuminance}'");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }


                    Thread.Sleep(1000);

                }
            }, _tokenSource.Token);
        }

        private Result<Unit> Drive(DoorDirection direction, double speed)
        {
            if (_controller == null)
            {
                return Result.Error<Unit>("Controller was not initialized.");
            }

            _controller.Write(Pin.MotorLeft, PinValue.Low);
            _controller.Write(Pin.MotorRight, PinValue.Low);
            _currentDirection = direction;
            _currentSpeed = speed;
            switch (direction)
            {
                case DoorDirection.None:
                    break;
                case DoorDirection.Up:
                    _controller.Write(Pin.MotorLeft, PinValue.High);
                    break;
                case DoorDirection.Down:
                    _controller.Write(Pin.MotorRight, PinValue.High);
                    break;
                default:
                    return Result.Error<Unit>($"{nameof(direction)} type has no member '${direction}'.");
            }
            return Unit.Instance;
        }

        public Result<Unit> EmergencyStop()
        {
            if (_controller == null)
            {
                return Result.Error<Unit>("Controller was not initialized.");
            }

            _controller.Write(Pin.MotorLeft, PinValue.Low);
            _controller.Write(Pin.MotorRight, PinValue.Low);
            _currentDirection = DoorDirection.None;
            _currentSpeed = 0;

            return Unit.Instance;
        }

        public Result<Unit> CloseDoor() => Drive(DoorDirection.Down, DownSpeed);

        public Result<Unit> OpenDoor() => Drive(DoorDirection.Up, UpSpeed);

        public Result<bool> IsOpeningDoor() => _currentDirection == DoorDirection.Up;

        public Result<bool> IsClosingDoor() => _currentDirection == DoorDirection.Down;

        public Result<DoorDirection> GetDirection() => _currentDirection;
    }

    public static class Bh1750fviExtenstion
    {
        public const byte DefaultI2cAddress = 0x23;
        public const byte SecondaryI2cAddress = 0x5c;
    }

}
