using System;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.I2c;
using System.Device.Pwm;
using System.Device.Pwm.Drivers;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Driver;
using ChickenDoorDriver;
using FunicularSwitch;
using Iot.Device.Amg88xx;
using Iot.Device.Bh1750fvi;
using Iot.Device.Bmxx80;
using Iot.Device.Bmxx80.PowerMode;
using Iot.Device.Vl53L0X;
using UnitsNet;

namespace Driver
{

    /**
     * Freund und Helfer auf dem Pi
     * i2cdetect -r -y 1
     */
    public class PiDriver : IDriver
    {
        class Pin
        {
            public const int MotorLeft = 24;
            public const int MotorRight = 23;
            public const int MotorEnable = 12;
            public const int HallBottom = 17;
            public const int HallTop = 27;
            public const int PhotoelectricBarrier = 22;
            public const int EmergencyTop = 10;
        }

        private double _currentSpeed = 1;
        private DoorDirection _currentDirection = DoorDirection.None;
        private const double UpSpeed = 0.5; //0.70;
        private const double DownSpeed = 0.1; //0.35;

        private GpioController _controller;
        private bool _isRunning;

        private CancellationTokenSource _tokenSource;

        private PwmChannel _pwmMotor;

        //private Bme280 _bme280;
        private int _measurementTime;
        private Vl53L0X _vl53L0X;
        private Bh1750fvi _bh1750Fvi;
        private Amg88xx _amg88xx;

        public Result<Unit> Init()
        {
            _controller = new GpioController(PinNumberingScheme.Logical, new RaspberryPi3Driver());
            _controller.OpenPin(Pin.HallBottom, PinMode.InputPullUp);
            _controller.OpenPin(Pin.HallTop, PinMode.InputPullUp);
            _controller.OpenPin(Pin.PhotoelectricBarrier, PinMode.InputPullUp);
            _controller.OpenPin(Pin.MotorEnable, PinMode.Output);
            _controller.OpenPin(Pin.MotorLeft, PinMode.Output);
            _controller.OpenPin(Pin.MotorRight, PinMode.Output);
            _controller.OpenPin(Pin.EmergencyTop, PinMode.InputPullDown);

            _pwmMotor = new SoftwarePwmChannel(Pin.MotorEnable, 200, 0.1);
            _pwmMotor.Start();

            _controller.Write(Pin.MotorLeft, PinValue.Low);
            _controller.Write(Pin.MotorRight, PinValue.Low);


            Console.WriteLine($"Init sensor");

            _bh1750Fvi = new Bh1750fvi(I2cDevice.Create(new I2cConnectionSettings(1, Bh1750fviExtenstion.DefaultI2cAddress)));

            _vl53L0X = new Vl53L0X(I2cDevice.Create(new I2cConnectionSettings(1, Vl53L0X.DefaultI2cAddress)));

            //_bme280 = new Bme280(I2cDevice.Create(new I2cConnectionSettings(1, Bme280.SecondaryI2cAddress)));

            //_measurementTime = _bme280.GetMeasurementDuration();

            //_bme280.SetPowerMode(Bmx280PowerMode.Forced);
            //Thread.Sleep(_measurementTime);

            //_bme280.TryReadTemperature(out var tempValue);
            //_bme280.TryReadPressure(out var preValue);
            //_bme280.TryReadHumidity(out var humValue);
            //_bme280.TryReadAltitude(out var altValue);

            //Console.WriteLine($"Temperature: {tempValue.DegreesCelsius:0.#}\u00B0C");
            //Console.WriteLine($"Pressure: {preValue.Hectopascals:#.##} hPa");
            //Console.WriteLine($"Relative humidity: {humValue.Percent:#.##}%");
            //Console.WriteLine($"Estimated altitude: {altValue.Meters:#} m");

            _amg88xx = new Amg88xx(I2cDevice.Create(new I2cConnectionSettings(1, Amg88xx.AlternativeI2cAddress)));

            Thread.Sleep(100);


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

                while (_isRunning)
                {
                    if (ct.IsCancellationRequested)
                    {
                        _controller.Write(Pin.MotorLeft, PinValue.Low);
                        _controller.Write(Pin.MotorRight, PinValue.Low);
                        _currentDirection = DoorDirection.None;
                        _pwmMotor.Stop();
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

                    if (_controller.Read(Pin.EmergencyTop) == PinValue.High)
                    {
                        _controller.Write(Pin.MotorLeft, PinValue.Low);
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

                    //try
                    //{
                    //    Console.WriteLine($"Distance: '{_vl53L0X.Distance}'");
                    //}
                    //catch (Exception e)
                    //{
                    //    Console.WriteLine(e);
                    //}

                    //try
                    //{
                    //    Console.WriteLine($"Light: '{_bh1750Fvi.Illuminance}'");
                    //}
                    //catch (Exception e)
                    //{
                    //    Console.WriteLine(e);
                    //}


                    Thread.Sleep(500);

                    Console.WriteLine($"Start Read Image");
                    _amg88xx.ReadImage();
                    Console.WriteLine($"Finished Read Image");
                    Console.WriteLine($"Get Image");
                    var temperatureImage = _amg88xx.TemperatureImage;
                    Console.WriteLine($"Start Convert Image");
                    //PrintTemperatureImage(temperatureImage);
                    var image = ConvertTemperatureImage(temperatureImage);
                    ConsoleWriteImage(image);
                    Console.WriteLine($"Finished Convert Image");

                }
            }, _tokenSource.Token);
        }


        private void PrintTemperatureImage(Temperature[,] temperatureImage)
        {
            for (int x = 0; x < 8; x++)
            {
                Console.Write("[ ");
                for (int y = 0; y < 8; y++)
                {
                    var temperature = temperatureImage[x, y];
                    var t = temperature.DegreesCelsius;
                    Console.Write(t.ToString("N1") + ", ");
                }
                Console.WriteLine(" ]");
                Console.WriteLine("");
            }
        }


        private Bitmap ConvertTemperatureImage(Temperature[,] temperatureImage)
        {
            var b = new Bitmap(8, 8);
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    var t = temperatureImage[x, y];
                    b.SetPixel(x, y, ConvertTemperatureToColor(t));
                }
            }

            return b;
        }

        private double f = 255 / 40;

        private Color ConvertTemperatureToColor(Temperature temperature)
        {
            var t = temperature.DegreesCelsius;
            var value = (int)(t * f) % 255;
            return Color.FromArgb(value, value, value);
        }

        static int[] cColors = { 0x000000, 0x000080, 0x008000, 0x008080, 0x800000, 0x800080, 0x808000, 0xC0C0C0, 0x808080, 0x0000FF, 0x00FF00, 0x00FFFF, 0xFF0000, 0xFF00FF, 0xFFFF00, 0xFFFFFF };

        public static void ConsoleWritePixel(Color cValue)
        {
            Color[] cTable = cColors.Select(x => Color.FromArgb(x)).ToArray();
            char[] rList = new char[] { (char)9617, (char)9618, (char)9619, (char)9608 }; // 1/4, 2/4, 3/4, 4/4
            int[] bestHit = new int[] { 0, 0, 4, int.MaxValue }; //ForeColor, BackColor, Symbol, Score

            for (int rChar = rList.Length; rChar > 0; rChar--)
            {
                for (int cFore = 0; cFore < cTable.Length; cFore++)
                {
                    for (int cBack = 0; cBack < cTable.Length; cBack++)
                    {
                        int R = (cTable[cFore].R * rChar + cTable[cBack].R * (rList.Length - rChar)) / rList.Length;
                        int G = (cTable[cFore].G * rChar + cTable[cBack].G * (rList.Length - rChar)) / rList.Length;
                        int B = (cTable[cFore].B * rChar + cTable[cBack].B * (rList.Length - rChar)) / rList.Length;
                        int iScore = (cValue.R - R) * (cValue.R - R) + (cValue.G - G) * (cValue.G - G) + (cValue.B - B) * (cValue.B - B);
                        if (!(rChar > 1 && rChar < 4 && iScore > 50000)) // rule out too weird combinations
                        {
                            if (iScore < bestHit[3])
                            {
                                bestHit[3] = iScore; //Score
                                bestHit[0] = cFore;  //ForeColor
                                bestHit[1] = cBack;  //BackColor
                                bestHit[2] = rChar;  //Symbol
                            }
                        }
                    }
                }
            }
            Console.ForegroundColor = (ConsoleColor)bestHit[0];
            Console.BackgroundColor = (ConsoleColor)bestHit[1];
            Console.Write(rList[bestHit[2] - 1]);
        }

        public static void ConsoleWriteImage(Bitmap source)
        {
            int sMax = 39;
            decimal percent = Math.Min(decimal.Divide(sMax, source.Width), decimal.Divide(sMax, source.Height));
            Size dSize = new Size((int)(source.Width * percent), (int)(source.Height * percent));
            Bitmap bmpMax = new Bitmap(source, dSize.Width * 2, dSize.Height);
            for (int i = 0; i < dSize.Height; i++)
            {
                for (int j = 0; j < dSize.Width; j++)
                {
                    ConsoleWritePixel(bmpMax.GetPixel(j * 2, i));
                    ConsoleWritePixel(bmpMax.GetPixel(j * 2 + 1, i));
                }
                System.Console.WriteLine();
            }
            Console.ResetColor();
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
