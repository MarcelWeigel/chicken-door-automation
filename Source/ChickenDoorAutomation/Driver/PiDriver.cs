using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.I2c;
using System.Device.Pwm;
using System.Device.Pwm.Drivers;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Driver;
using ChickenDoorDriver;
using FunicularSwitch;
using Iot.Device.Amg88xx;
using Iot.Device.Bh1750fvi;
using Iot.Device.Bmxx80;
using Iot.Device.Bmxx80.PowerMode;
using Iot.Device.Imu;
using Iot.Device.Vl53L0X;
using UnitsNet;
using MeasurementMode = Iot.Device.Magnetometer.MeasurementMode;

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

        private Bme280 _bme280;
        private int _measurementTime;
        private Vl53L0X _vl53L0X;
        private Bh1750fvi _bh1750Fvi;
        private Amg88xx _amg88xx;
        private Mpu9250 _mpu9250;

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

            _bh1750Fvi = new Bh1750fvi(I2cDevice.Create(new I2cConnectionSettings(1, Bh1750fviExtenstion.DefaultI2cAddress))); // 23

            _vl53L0X = new Vl53L0X(I2cDevice.Create(new I2cConnectionSettings(1, Vl53L0X.DefaultI2cAddress))); // 29

            _bme280 = new Bme280(I2cDevice.Create(new I2cConnectionSettings(1, Bme280.SecondaryI2cAddress))); // 76

            _measurementTime = _bme280.GetMeasurementDuration();
            _bme280.SetPowerMode(Bmx280PowerMode.Normal);
            //Thread.Sleep(_measurementTime);

            //_bme280.TryReadTemperature(out var tempValue);
            //_bme280.TryReadPressure(out var preValue);
            //_bme280.TryReadHumidity(out var humValue);
            //_bme280.TryReadAltitude(out var altValue);

            //Console.WriteLine($"Temperature: {tempValue.DegreesCelsius:0.#}\u00B0C");
            //Console.WriteLine($"Pressure: {preValue.Hectopascals:#.##} hPa");
            //Console.WriteLine($"Relative humidity: {humValue.Percent:#.##}%");
            //Console.WriteLine($"Estimated altitude: {altValue.Meters:#} m");

            _amg88xx = new Amg88xx(I2cDevice.Create(new I2cConnectionSettings(1, Amg88xx.AlternativeI2cAddress))); // 69

            _mpu9250 = new Mpu9250(I2cDevice.Create(new I2cConnectionSettings(1, Mpu9250.DefaultI2cAddress))); // 68
            _mpu9250.MagnetometerMeasurementMode = MeasurementMode.ContinuousMeasurement100Hz;

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
                }
            }, _tokenSource.Token);
        }

        private double[] ConvertTemperatureImage(Temperature[,] temperatureImage)
        {
            var b = new double[8*8];
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    b[8 * x + y] = temperatureImage[x, y].DegreesCelsius;
                }
            }

            return b;
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
        
        public Result<SensorData> ReadSensorData()
        {
            _bme280.TryReadTemperature(out var tempValue);
            _bme280.TryReadPressure(out var preValue);
            _bme280.TryReadHumidity(out var humValue);
            _bme280.TryReadAltitude(out var altValue);
            var data = new SensorData
            {
                HeatMap = ReadHeatMap(),
                HallTop = _controller.Read(Pin.HallTop) == PinValue.Low,
                HallBottom = _controller.Read(Pin.HallBottom) == PinValue.Low,
                PhotoelectricBarrier = _controller.Read(Pin.PhotoelectricBarrier) == PinValue.Low,
                Taster = _controller.Read(Pin.EmergencyTop) == PinValue.High,
                Gyroscope = Convert(_mpu9250.GetGyroscopeReading()),
                Accelerometer = Convert(_mpu9250.GetAccelerometer()),
                Magnetometer = Convert(_mpu9250.ReadMagnetometer(true)),
                Distance = _vl53L0X.Distance,
                Illuminance = _bh1750Fvi.Illuminance.Lux,
                Temperature = tempValue.DegreesCelsius,
                Pressure = preValue.Hectopascals,
                Humidity = humValue.Percent,
                Altitude = altValue.Centimeters
            };

            RecordData(data);

            return data;
        }

        private readonly List<SensorData> _data = new List<SensorData>();
        private void RecordData(SensorData data)
        {
            _data.Add(data);
            if (_data.Count == 400)
            {
                var dataFile = System.IO.File.Create($"Data_{DateTime.Now.ToString("yyyyMMddHHmm")}.json");
                var fileWriter = new System.IO.StreamWriter(dataFile);
                fileWriter.WriteLine(JsonSerializer.Serialize(this._data.ToArray()));
                fileWriter.Dispose();
                _data.Clear();
            }
        }

        private double[] Convert(Vector3 v)
        {
            return new double[] {v.X, v.Y, v.Z};
        }

        private double[] ReadHeatMap()
        {
            _amg88xx.ReadImage();
            var temperatureImage = _amg88xx.TemperatureImage;
            return ConvertTemperatureImage(temperatureImage);
        }
    }

    public static class Bh1750fviExtenstion
    {
        public const byte DefaultI2cAddress = 0x23;
        public const byte SecondaryI2cAddress = 0x5c;
    }

}
