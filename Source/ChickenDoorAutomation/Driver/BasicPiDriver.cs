using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.Pwm;
using System.Device.Pwm.Drivers;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Driver;
using ChickenDoorDriver;
using FunicularSwitch;
using OpenCvSharp;

namespace Driver
{
    public class BasicPiDriver : IDriver
    {
        class Pin
        {
            public const int MotorLeft = 24;
            public const int MotorRight = 23;
            public const int MotorEnable = 12;
            public const int HallBottom = 27;
            public const int HallTop = 17;
            public const int PhotoelectricBarrier = 22;
            public const int EmergencyTop = 10;
            public const int DC12_1 = 14;
            public const int DC12_2 = 15;
            public const int TasterUp = 19;
            public const int TasterDown = 26;
        }

        private double _currentSpeed = 1;
        private DoorDirection _currentDirection = DoorDirection.None;
        private DoorState _currentDoorState = DoorState.Init;
        private const double UpSpeed = 0.5; //0.70;
        private const double DownSpeed = 0.1; //0.35;

        private GpioController _controller;
        private bool _isRunning;

        private CancellationTokenSource _tokenSource;

        private PwmChannel _pwmMotor;

        private VideoCapture _capture;

        readonly TimeSpan _closeTime = TimeSpan.FromHours(20).Add(TimeSpan.FromMinutes(10));
        readonly TimeSpan _openTime = TimeSpan.FromHours(6).Add(TimeSpan.FromMinutes(15));

        public Result<Unit> Init()
        {
            Console.WriteLine($"Init driver at {DateTime.Now}. Door will close at {_closeTime} and open at {_openTime}");

            _controller = new GpioController(PinNumberingScheme.Logical, new RaspberryPi3Driver());
            _controller.OpenPin(Pin.HallBottom, PinMode.InputPullUp);
            _controller.OpenPin(Pin.HallTop, PinMode.InputPullUp);
            _controller.OpenPin(Pin.PhotoelectricBarrier, PinMode.InputPullUp);
            _controller.OpenPin(Pin.MotorEnable, PinMode.Output);
            _controller.OpenPin(Pin.MotorLeft, PinMode.Output);
            _controller.OpenPin(Pin.MotorRight, PinMode.Output);
            _controller.OpenPin(Pin.EmergencyTop, PinMode.InputPullUp);
            _controller.OpenPin(Pin.DC12_1, PinMode.Output);
            _controller.OpenPin(Pin.DC12_2, PinMode.Output);
            _controller.OpenPin(Pin.TasterUp, PinMode.Output);
            _controller.OpenPin(Pin.TasterDown, PinMode.Output);

            _controller.Write(Pin.MotorEnable, PinValue.High);

            _controller.Write(Pin.MotorLeft, PinValue.Low);
            _controller.Write(Pin.MotorRight, PinValue.Low);
            _controller.Write(Pin.DC12_1, PinValue.Low);
            _controller.Write(Pin.DC12_2, PinValue.Low);
            _controller.Write(Pin.TasterUp, PinValue.Low);
            _controller.Write(Pin.TasterDown, PinValue.Low);

            _pwmMotor = new SoftwarePwmChannel(Pin.MotorEnable, 200, 0.1);
            _pwmMotor.Start();

            _capture = new VideoCapture(0);

            Run();

            Thread.Sleep(100);

            return Unit.Instance;
        }

        private void Run()
        {
            _tokenSource = new CancellationTokenSource();
            CancellationToken ct = _tokenSource.Token;

            var task = Task.Run(async () =>
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

                    var now = DateTime.Now;
                    if (now.TimeOfDay > _closeTime && now.TimeOfDay < _closeTime.Add(TimeSpan.FromMinutes(15))
                                                      && _currentDoorState == DoorState.Closed)
                    {
                        Drive(DoorDirection.Up, UpSpeed);
                    } else if (now.TimeOfDay > _openTime && now.TimeOfDay < _openTime.Add(TimeSpan.FromMinutes(15))
                                                              && _currentDoorState == DoorState.Open)
                    {
                        Drive(DoorDirection.Down, DownSpeed);
                    }

                    if (_controller.Read(Pin.HallTop) == PinValue.Low && _currentDirection == DoorDirection.Up)
                    {
                        Console.WriteLine("Reached top stopping");
                        ReachedStop();
                    }
                    if (_controller.Read(Pin.HallBottom) == PinValue.Low && _currentDirection == DoorDirection.Down)
                    {
                        Console.WriteLine("Reached bottom stopping");
                        ReachedStop();
                    }

                    //if (_controller.Read(Pin.PhotoelectricBarrier) == PinValue.Low && _currentDirection == DoorDirection.Down)
                    //{
                    //    Console.WriteLine("Direction was down");
                    //    Drive(DoorDirection.Up, UpSpeed);
                    //}

                    if (_controller.Read(Pin.TasterUp) == PinValue.High)
                    {
                        //if (_currentDirection == DoorDirection.None || _currentDirection == DoorDirection.Down)
                        //{
                            Console.WriteLine("Go Up");
                            Drive(DoorDirection.Up, UpSpeed);
                        //} 
                        //else if (_currentDirection == DoorDirection.Up)
                        //{
                        //    Console.WriteLine("Double Up stop Motor");
                        //    EmergencyStop();
                        //}
                        
                    } else if (_controller.Read(Pin.TasterDown) == PinValue.High)
                    {
                        //if (_currentDirection == DoorDirection.None || _currentDirection == DoorDirection.Up)
                        //{
                            Console.WriteLine("Go Down");
                            Drive(DoorDirection.Down, DownSpeed);
                        //}
                        //else if (_currentDirection == DoorDirection.Up)
                        //{
                        //    Console.WriteLine("Double Down stop Motor");
                        //    EmergencyStop();
                        //}
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(100), ct);
                }
            }, _tokenSource.Token);
        }

        private Result<Unit> Drive(DoorDirection direction, double speed)
        {
            Console.WriteLine($"Drive in direction: '{direction}'.");

            _controller.Write(Pin.MotorLeft, PinValue.Low);
            _controller.Write(Pin.MotorRight, PinValue.Low);
            _currentDirection = direction;
            _currentSpeed = speed;
            _pwmMotor.DutyCycle = _currentSpeed;
            switch (direction)
            {
                case DoorDirection.None:
                    break;
                case DoorDirection.Up:
                    _controller.Write(Pin.MotorLeft, PinValue.High);
                    SetCurrentDoorState(DoorState.Opening);
                    break;
                case DoorDirection.Down:
                    _controller.Write(Pin.MotorRight, PinValue.High);
                    SetCurrentDoorState(DoorState.Closing);
                    break;
                default:
                    SetCurrentDoorState(DoorState.Error);
                    return Result.Error<Unit>($"{nameof(direction)} type has no member '${direction}'.");
            }
            return Unit.Instance;
        }

        void SetCurrentDoorState(DoorState state)
        {
            _currentDoorState = state;
            Console.WriteLine($"Current door state set to: {_currentDoorState}");
        }

        public Result<Unit> EmergencyStop()
        {
            _controller.Write(Pin.MotorLeft, PinValue.Low);
            _controller.Write(Pin.MotorRight, PinValue.Low);
            _currentDirection = DoorDirection.None;
            _currentSpeed = 0;

            return Unit.Instance;
        }

        public Result<Unit> ReachedStop()
        {
            _controller.Write(Pin.MotorLeft, PinValue.Low);
            _controller.Write(Pin.MotorRight, PinValue.Low);

            _currentSpeed = 0;
            if (_currentDirection == DoorDirection.Up)
            {
                SetCurrentDoorState(DoorState.Open);
            } else if (_currentDirection == DoorDirection.Down)
            {
                SetCurrentDoorState(DoorState.Closed);
            }
            _currentDirection = DoorDirection.None;

            return Unit.Instance;
        }

        public Result<Unit> CloseDoor() => Drive(DoorDirection.Down, DownSpeed);

        public Result<Unit> OpenDoor() => Drive(DoorDirection.Up, UpSpeed);

        public Result<Unit> TurnLightOn()
        {
            _controller.Write(Pin.DC12_1, PinValue.High);

            return Unit.Instance;
        }

        public Result<Unit> TurnLightOff()
        {
            _controller.Write(Pin.DC12_1, PinValue.Low);

            return Unit.Instance;
        }

        public Result<bool> IsOpeningDoor() => _currentDirection == DoorDirection.Up;

        public Result<bool> IsClosingDoor() => _currentDirection == DoorDirection.Down;

        public Result<DoorDirection> GetDirection() => _currentDirection;
        public Result<SensorData> ReadSensorData()
        {
            return new SensorData();
        }

        public Result<DoorInfo> GetDoorInfo()
        {
            return new DoorInfo
            {
                DoorState = DoorState.Init,
                DoorDirection = _currentDirection,
                Position = 20
            };
        }

        private readonly List<SensorData> _data = new List<SensorData>();
        private void RecordData(SensorData data)
        {
            _data.Add(data);
            if (_data.Count == 400)
            {
                var dataFile = File.Create($"Data_{DateTime.Now.ToString("yyyyMMddHHmm")}.json");
                var fileWriter = new StreamWriter(dataFile);
                fileWriter.WriteLine(JsonSerializer.Serialize(_data.ToArray()));
                fileWriter.Dispose();
                _data.Clear();
            }
        }

        public Result<string> ReadVideoCapture()
        {
            string imgSrc = "";
            using (var frame = new Mat())
            {

                _capture.Read(frame);

                var base64 = Convert.ToBase64String(frame.ToBytes());
                imgSrc = $"data:image/gif;base64,{base64}";
            }

            return imgSrc;
        }

        public void Dispose()
        {
            Console.WriteLine("Clean up sensors");

            _capture.Dispose();
        }
    }
}
