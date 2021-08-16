using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using FunicularSwitch;
using OpenCvSharp;

namespace Driver
{
    public class MockChickenDoorControl : IChickenDoorControl
    {
        private const double UpSpeed = 0.5;
        private const double DownSpeed = 0.1;
        private const double UpperRange = 10;
        private const double LowerRange = 0;

        private CancellationTokenSource _tokenSource;

        private double _currentSpeed = 100;
        private double _doorPosition = 0;
        private bool _moveUp = false;
        private bool _moveDown = false;
        private bool _isRunning = false;

        private VideoCapture _capture;

        public MockChickenDoorControl(VideoCapture capture)
        {
            _capture = capture;

            Run();
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
                        _isRunning = false;
                    }
                    else if (_moveUp)
                    {
                        _doorPosition += _currentSpeed;
                        if (_doorPosition > UpperRange)
                        {
                            _doorPosition = UpperRange;
                        }
                    }
                    else if (_moveDown)
                    {
                        _doorPosition -= _currentSpeed;
                        if (_doorPosition < LowerRange)
                        {
                            _doorPosition = LowerRange;
                        }
                    }
                    Thread.Sleep(100);
                }
            }, _tokenSource.Token);
        }

        public bool HallTopReached() => _doorPosition == UpperRange;

        public bool HallBottomReached() => _doorPosition == LowerRange;

        public bool TasterUpPressed => false;
        public bool TasterDownPressed => false;
        public Result<Unit> Drive(Direction direction, double speed)
        {
            if (speed < 0) speed = 0;
            if (speed > 1) speed = 1;

            Stop();

            Console.WriteLine($"Driving in direction: '{direction}' with speed {speed}");

            _currentSpeed = speed;

            return direction.Match(
                up =>
                {
                    _moveUp = true;
                    return No.Thing;
                },
                down =>
                {
                    _moveDown = true;
                    return No.Thing;
                });
        }

        public Result<Unit> Stop()
        {
            _moveUp = false;
            _moveDown = false;
            Console.WriteLine("Door stopped");
            return No.Thing;
        }

        public Result<Unit> TurnLightOn()
        {
            return Unit.Instance;
        }

        public Result<Unit> TurnLightOff()
        {
            return Unit.Instance;
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
        }
    }
}
