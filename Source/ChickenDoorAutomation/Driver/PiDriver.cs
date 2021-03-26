using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Threading;
using System.Threading.Tasks;
using Application.Driver;
using ChickenDoorDriver;
using FunicularSwitch;

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

        private int _currentSpeed = 100;
        private DoorDirection _currentDirection = DoorDirection.None;
        private const int UpSpeed = 70;
        private const int DownSpeed = 35;

        private GpioController _controller;
        private bool _isRunning = false;

        private CancellationTokenSource _tokenSource;

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

            // TODO PWM

            _controller.Write(Pin.MotorLeftEnable, PinValue.High);
            _controller.Write(Pin.MotorRightEnable, PinValue.High);
            _controller.Write(Pin.MotorLeft, PinValue.Low);
            _controller.Write(Pin.MotorRight, PinValue.Low);

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
                    Thread.Sleep(100);
                }
            }, _tokenSource.Token);
        }

        private Result<Unit> Drive(DoorDirection direction, int speed)
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
}
