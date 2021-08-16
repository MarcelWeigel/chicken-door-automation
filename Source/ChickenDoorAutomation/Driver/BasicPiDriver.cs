using System;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.Pwm;
using System.Device.Pwm.Drivers;
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
        readonly IChickenDoorControl _chickenDoorControl;
        DoorDirection _currentDirection = DoorDirection.None;
        DoorState _currentDoorState = DoorState.Unknown;
        const double UpSpeed = 0.5; //0.70;
        const double DownSpeed = 0.1; //0.35;

        CancellationTokenSource? _tokenSource;
        Task? _task;

        readonly TimeSpan _closeTime = TimeSpan.FromHours(20).Add(TimeSpan.FromMinutes(10));
        readonly TimeSpan _openTime = TimeSpan.FromHours(6).Add(TimeSpan.FromMinutes(15));

        public BasicPiDriver(IChickenDoorControl chickenDoorControl)
        {
            _chickenDoorControl = chickenDoorControl;
            Console.WriteLine($"Start driver at {DateTime.Now} with control {chickenDoorControl.GetType().Name}. Door will close at {_closeTime} and open at {_openTime}");
        }

        public Result<Unit> Start()
        {
            Run();
            Thread.Sleep(100);
            return No.Thing;
        }

        public async Task Stop()
        {
            if (_task != null && _tokenSource != null)
            {
                _tokenSource.Cancel();
                await _task;
                _task = null;
                _tokenSource = null;
            }
        }

        void Run()
        {
            _tokenSource = new CancellationTokenSource();
            var ct = _tokenSource.Token;

            _task = Task.Run(async () =>
            {
                while (true)
                {
                    if (ct.IsCancellationRequested)
                    {
                        //TODO: why stop pwm channel here? will be broken on restart
                        _chickenDoorControl.Shutdown();
                        _currentDirection = DoorDirection.None;
                        break;
                    }

                    var now = DateTime.Now;
                    if (now.TimeOfDay > _closeTime
                        && now.TimeOfDay < _closeTime.Add(TimeSpan.FromMinutes(15))
                        && _currentDoorState != DoorState.Open
                        && _currentDoorState != DoorState.Opening)
                    {
                        Drive(Direction.Up, UpSpeed);
                    }
                    else if (now.TimeOfDay > _openTime
                             && now.TimeOfDay < _openTime.Add(TimeSpan.FromMinutes(15))
                             && _currentDoorState != DoorState.Closed
                             && _currentDoorState != DoorState.Closing
                    )
                    {
                        Drive(Direction.Down, DownSpeed);
                    }

                    if (_chickenDoorControl.HallTopReached() && _currentDirection == DoorDirection.Up)
                    {
                        Console.WriteLine("Reached top stopping");
                        ReachedStop();
                    }
                    if (_chickenDoorControl.HallBottomReached() && _currentDirection == DoorDirection.Down)
                    {
                        Console.WriteLine("Reached bottom stopping");
                        ReachedStop();
                    }

                    if (_chickenDoorControl.TasterUpPressed)
                    {
                        Console.WriteLine("Taster up pressed");
                        Drive(Direction.Up, UpSpeed);
                    }
                    else if (_chickenDoorControl.TasterDownPressed)
                    {
                        Console.WriteLine("Taster down pressed");
                        Drive(Direction.Down, DownSpeed);
                    }

                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(100), ct);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
            }, _tokenSource.Token);
        }

        Result<Unit> Drive(Direction direction, double speed)
        {
            Console.WriteLine($"Drive in direction: '{direction}'.");

            _chickenDoorControl.Drive(direction, speed);
            SetCurrentDoorState(direction.Match(up => DoorState.Opening, down => DoorState.Closing));
            _currentDirection = direction.Match(up => DoorDirection.Up, down => DoorDirection.Down);
            return No.Thing;
        }

        void SetCurrentDoorState(DoorState state)
        {
            _currentDoorState = state;
            Console.WriteLine($"Current door state set to: {_currentDoorState}");
        }

        public Result<Unit> EmergencyStop()
        {
            _chickenDoorControl.Stop();
            _currentDirection = DoorDirection.None;
            SetCurrentDoorState(DoorState.Unknown);
            return No.Thing;
        }

        public Result<Unit> ReachedStop()
        {
            _chickenDoorControl.Stop();
            if (_currentDirection == DoorDirection.Up)
            {
                SetCurrentDoorState(DoorState.Open);
            }
            else if (_currentDirection == DoorDirection.Down)
            {
                SetCurrentDoorState(DoorState.Closed);
            }
            _currentDirection = DoorDirection.None;

            return No.Thing;
        }

        public Result<Unit> CloseDoor() => Drive(Direction.Down, DownSpeed);

        public Result<Unit> OpenDoor() => Drive(Direction.Up, UpSpeed);

        public Result<Unit> TurnLightOn()
        {
            _chickenDoorControl.TurnLightOn();
            return No.Thing;
        }

        public Result<Unit> TurnLightOff()
        {
            _chickenDoorControl.TurnLightOff();
            return No.Thing;
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
                DoorState = _currentDoorState,
                DoorDirection = _currentDirection,
                Position = 20
            };
        }

        public Result<string> ReadVideoCapture() => _chickenDoorControl.ReadVideoCapture();

        public void Dispose()
        {
        }
    }

    public class HardwareFactory
    {
        public static GpioController CreateGpioController()
        {
            var controller = new GpioController(PinNumberingScheme.Logical, new RaspberryPi3Driver());
            controller.OpenPin(ChickenDoorControl.Pin.HallBottom, PinMode.InputPullUp);
            controller.OpenPin(ChickenDoorControl.Pin.HallTop, PinMode.InputPullUp);
            controller.OpenPin(ChickenDoorControl.Pin.PhotoelectricBarrier, PinMode.InputPullUp);
            controller.OpenPin(ChickenDoorControl.Pin.MotorEnable, PinMode.Output);
            controller.OpenPin(ChickenDoorControl.Pin.MotorLeft, PinMode.Output);
            controller.OpenPin(ChickenDoorControl.Pin.MotorRight, PinMode.Output);
            controller.OpenPin(ChickenDoorControl.Pin.EmergencyTop, PinMode.InputPullUp);
            controller.OpenPin(ChickenDoorControl.Pin.DC12_1, PinMode.Output);
            controller.OpenPin(ChickenDoorControl.Pin.DC12_2, PinMode.Output);
            controller.OpenPin(ChickenDoorControl.Pin.TasterUp, PinMode.Output);
            controller.OpenPin(ChickenDoorControl.Pin.TasterDown, PinMode.Output);

            controller.Write(ChickenDoorControl.Pin.MotorEnable, PinValue.High);
            controller.Write(ChickenDoorControl.Pin.MotorLeft, PinValue.Low);
            controller.Write(ChickenDoorControl.Pin.MotorRight, PinValue.Low);
            controller.Write(ChickenDoorControl.Pin.DC12_1, PinValue.Low);
            controller.Write(ChickenDoorControl.Pin.DC12_2, PinValue.Low);
            controller.Write(ChickenDoorControl.Pin.TasterUp, PinValue.Low);
            controller.Write(ChickenDoorControl.Pin.TasterDown, PinValue.Low);

            return controller;
        }

        public static PwmChannel CreateMotor()
        {
            var pwmMotor = new SoftwarePwmChannel(ChickenDoorControl.Pin.MotorEnable, 200, 0.1);
            pwmMotor.Start();
            return pwmMotor;
        }

        public static VideoCapture CreateVideoCapture() => new VideoCapture(0);
    }

    public interface IChickenDoorControl
    {
        bool HallTopReached();
        bool HallBottomReached();
        bool TasterUpPressed { get; }
        bool TasterDownPressed { get; }
        Result<Unit> Drive(Direction direction, double speed);
        Result<Unit> Stop();
        Result<Unit> TurnLightOn();
        Result<Unit> TurnLightOff();
        Result<string> ReadVideoCapture();
        void Shutdown();
    }

    public class ChickenDoorControl : IChickenDoorControl
    {
        readonly GpioController _controller;
        readonly PwmChannel _pwmMotor;
        readonly VideoCapture _capture;

        public ChickenDoorControl(GpioController controller, PwmChannel pwmMotor, VideoCapture capture)
        {
            _controller = controller;
            _pwmMotor = pwmMotor;
            _capture = capture;
        }

        public bool HallTopReached() => _controller.Read(Pin.HallTop) == PinValue.Low;
        public bool HallBottomReached() => _controller.Read(Pin.HallBottom) == PinValue.Low;

        public bool TasterUpPressed => _controller.Read(Pin.TasterUp) == PinValue.High;
        public bool TasterDownPressed => _controller.Read(Pin.TasterDown) == PinValue.High;

        public Result<Unit> Drive(Direction direction, double speed)
        {
            if (speed < 0) speed = 0;
            if (speed > 1) speed = 1;
            
            Stop();

            Console.WriteLine($"Driving in direction: '{direction}' with speed {speed}");
            _pwmMotor.DutyCycle = speed;
            return direction.Match(
                up =>
                {
                    _controller.Write(Pin.MotorLeft, PinValue.High);
                    return No.Thing;
                },
                down =>
                {
                    _controller.Write(Pin.MotorRight, PinValue.High);
                    return No.Thing;
                });
        }

        public Result<Unit> Stop()
        {
            _controller.Write(Pin.MotorLeft, PinValue.Low);
            _controller.Write(Pin.MotorRight, PinValue.Low);
            Console.WriteLine("Door stopped");
            return No.Thing;
        }

        public Result<Unit> TurnLightOn()
        {
            _controller.Write(Pin.DC12_1, PinValue.High);
            Console.WriteLine("Light turned on");
            return No.Thing;
        }

        public Result<Unit> TurnLightOff()
        {
            _controller.Write(Pin.DC12_1, PinValue.Low);
            Console.WriteLine("Light turned off");
            return No.Thing;
        }

        public Result<string> ReadVideoCapture()
        {
            using var frame = new Mat();
            _capture.Read(frame);

            var base64 = Convert.ToBase64String(frame.ToBytes());
            var imgSrc = $"data:image/gif;base64,{base64}";
            return imgSrc;
        }

        public void Shutdown()
        {
            Stop();
            _pwmMotor.Stop();
            Console.WriteLine("Shut down");
        }

        public static class Pin
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
    }

    public abstract class Direction
    {
        public static readonly Direction Up = new Up_();
        public static readonly Direction Down = new Down_();

        public class Up_ : Direction
        {
            public Up_() : base(UnionCases.Up)
            {
            }
        }

        public class Down_ : Direction
        {
            public Down_() : base(UnionCases.Down)
            {
            }
        }

        internal enum UnionCases
        {
            Up,
            Down
        }

        internal UnionCases UnionCase { get; }
        Direction(UnionCases unionCase) => UnionCase = unionCase;

        public override string ToString() => Enum.GetName(typeof(UnionCases), UnionCase) ?? UnionCase.ToString();
        bool Equals(Direction other) => UnionCase == other.UnionCase;

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Direction)obj);
        }

        public override int GetHashCode() => (int)UnionCase;
    }

    public static class DirectionExtension
    {
        public static T Match<T>(this Direction direction, Func<Direction.Up_, T> up, Func<Direction.Down_, T> down)
        {
            switch (direction.UnionCase)
            {
                case Direction.UnionCases.Up:
                    return up((Direction.Up_)direction);
                case Direction.UnionCases.Down:
                    return down((Direction.Down_)direction);
                default:
                    throw new ArgumentException($"Unknown type derived from Direction: {direction.GetType().Name}");
            }
        }

        public static async Task<T> Match<T>(this Direction direction, Func<Direction.Up_, Task<T>> up, Func<Direction.Down_, Task<T>> down)
        {
            switch (direction.UnionCase)
            {
                case Direction.UnionCases.Up:
                    return await up((Direction.Up_)direction).ConfigureAwait(false);
                case Direction.UnionCases.Down:
                    return await down((Direction.Down_)direction).ConfigureAwait(false);
                default:
                    throw new ArgumentException($"Unknown type derived from Direction: {direction.GetType().Name}");
            }
        }

        public static async Task<T> Match<T>(this Task<Direction> direction, Func<Direction.Up_, T> up, Func<Direction.Down_, T> down) => (await direction.ConfigureAwait(false)).Match(up, down);
        public static async Task<T> Match<T>(this Task<Direction> direction, Func<Direction.Up_, Task<T>> up, Func<Direction.Down_, Task<T>> down) => await(await direction.ConfigureAwait(false)).Match(up, down).ConfigureAwait(false);
    }
}
