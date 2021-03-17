using System;
using System.Device.Gpio;
using System.Threading;
using System.Threading.Tasks;

namespace ChickenDoorDriver.Motor
{
    class Pin
    {
        public const int MotorUp = 13;
        public const int MotorDown = 13;
        public const int HallUp = 13;
        public const int HallDown = 13;
        public const int Enabled1 = 13;
        public const int Enabled2 = 13;
    }
    public class PiMotorDriver : IMotorDriver
    {
        private int _currentSpeed = 100;
        private MotorDirection _currentDirection = MotorDirection.None;
        private const int UpSpeed = 70;
        private const int DownSpeed = 35;

        private GpioController _controller;

        private CancellationTokenSource _tokenSource;

        private void Init()
        {
            _controller = new GpioController(PinNumberingScheme.Board);
            _controller.OpenPin(Pin.HallDown, PinMode.InputPullUp);
            _controller.OpenPin(Pin.HallUp, PinMode.InputPullUp);
            _controller.OpenPin(Pin.Enabled1, PinMode.Output);
            _controller.OpenPin(Pin.Enabled2, PinMode.Output);
            _controller.OpenPin(Pin.MotorDown, PinMode.Output);
            _controller.OpenPin(Pin.MotorUp, PinMode.Output);

            // TODO PWM

            Run();
        }

        private void Run()
        {
            _tokenSource = new CancellationTokenSource();
            CancellationToken ct = _tokenSource.Token;

            var task = Task.Run(() =>
            {
                bool isRunning = true;
                while (isRunning)
                {
                    if (ct.IsCancellationRequested)
                    {
                        _controller.Write(Pin.MotorUp, PinValue.Low);
                        _controller.Write(Pin.MotorDown, PinValue.Low);
                        _currentDirection = MotorDirection.None;
                        isRunning = false;
                    } else if (_controller.Read(Pin.HallUp) == PinValue.High)
                    {
                        _controller.Write(Pin.MotorUp, PinValue.Low);
                        _currentDirection = MotorDirection.None;
                    } else if (_controller.Read(Pin.HallDown) == PinValue.High)
                    {
                        _controller.Write(Pin.MotorDown, PinValue.Low);
                        _currentDirection = MotorDirection.None;
                    }
                    Thread.Sleep(100);
                }
            }, _tokenSource.Token);
        }

        private void Drive(MotorDirection direction, int speed)
        {
            _controller.Write(Pin.MotorUp, PinValue.Low);
            _controller.Write(Pin.MotorDown, PinValue.Low);
            _currentDirection = direction;
            _currentSpeed = speed;
            switch (direction)
            {
                case MotorDirection.None:
                    break;
                case MotorDirection.Up:
                    _controller.Write(Pin.MotorUp, PinValue.High);
                    break;
                case MotorDirection.Down:
                    _controller.Write(Pin.MotorDown, PinValue.High);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        public void Up() => Drive(MotorDirection.Up, UpSpeed);

        public void Down() => Drive(MotorDirection.Down, DownSpeed);

        public bool IsUp() => _currentDirection == MotorDirection.Up;

        public bool IsDown() => _currentDirection != MotorDirection.Down;

        public bool IsMoving() => _currentDirection != MotorDirection.None;
    }
}
