using System;
using System.Threading;
using System.Threading.Tasks;
using ChickenDoorDriver;
using FunicularSwitch;
using OpenCvSharp;

namespace Driver
{
    public class MockChickenDoorControl : IChickenDoorControl
    {
        const double UpperRange = 10;
        const double LowerRange = 0;

        CancellationTokenSource? _tokenSource;

        double _currentSpeed = 100;
        double _doorPosition;
        bool _moveUp;
        bool _moveDown;
        bool _isRunning;

        readonly VideoCapture _capture;

        public MockChickenDoorControl(VideoCapture capture)
        {
            _capture = capture;
            Run();
        }

        void Run()
        {
            _tokenSource = new CancellationTokenSource();
            var ct = _tokenSource.Token;

            var task = Task.Run(async () =>
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
                    await Task.Delay(TimeSpan.FromMilliseconds(100), ct);
                }
            }, _tokenSource.Token);
        }

        public bool HallBottomReached() => _doorPosition == UpperRange;

        public bool HallTopReached() => _doorPosition == LowerRange;

        public bool TasterDownPressed => false;
        public bool TasterUpPressed => false;
        public Result<Unit> Drive(Direction direction, double speed)
        {
            if (speed < 0) speed = 0;
            if (speed > 1) speed = 1;

            Stop();

            Log.Info($"Driving in direction: '{direction}' with speed {speed}");

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
            Log.Info("Door stopped");
            return No.Thing;
        }

        public Result<Unit> TurnLightOn()
        {
            Log.Info("Lights on");
            return Unit.Instance;
        }

        public Result<Unit> TurnLightOff()
        {
            Log.Info("Lights off");
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
            Log.Info("Shut down");
        }
    }

    public class MockMailer : IExternalNotification
    {
        public Task Notify(DoorState doorState, string cameraImage)
        {
            Log.Info($"Mocked External notification request: DoorState {doorState}, Image length: {cameraImage.Length}");
            return Task.CompletedTask;
        }
    }
}
