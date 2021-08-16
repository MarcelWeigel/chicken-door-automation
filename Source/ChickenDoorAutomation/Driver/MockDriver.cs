using System;
using System.Drawing;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Driver;
using ChickenDoorDriver;
//using Emgu.CV;
using FunicularSwitch;
using OpenCvSharp;

namespace Driver
{
    public class MockDriver : IDriver
    {
        private int _currentSpeed = 100;
        private DoorDirection _currentDirection = DoorDirection.None;
        private const int UpSpeed = 70;
        private const int DownSpeed = 35;

        private CancellationTokenSource _tokenSource;

        private int _driveCounter = 0;
        private bool _isRunning = false;

        private SensorData[] _sensorData;
        private int _currentIndex = 0;

        private VideoCapture _capture;

        public Result<Unit> Start()
        {
            _capture = new VideoCapture(0);

            Run();

            var data = System.IO.File.ReadAllText("Data_202104102216.json");
            _sensorData = JsonSerializer.Deserialize<SensorData[]>(data);

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
                        _currentDirection = DoorDirection.None;
                        _isRunning = false;
                    }
                    else if (_currentDirection != DoorDirection.None)
                    {
                        if (_driveCounter <= 0)
                        {
                            Console.WriteLine($"Finished driving.");
                            _driveCounter = 0;
                            _currentDirection = DoorDirection.None;
                        }
                        else
                        {
                            _driveCounter -= _currentSpeed;
                        }
                    }
                    Thread.Sleep(100);
                }
            }, _tokenSource.Token);
        }

        private Result<Unit> Drive(DoorDirection direction, int speed)
        {
            Console.WriteLine($"Drive in direction: '{direction}'.");

            _currentDirection = direction;
            _currentSpeed = speed;
            switch (direction)
            {
                case DoorDirection.None:
                    break;
                case DoorDirection.Up:
                    _driveCounter = 1000;
                    break;
                case DoorDirection.Down:
                    _driveCounter = 1000;
                    break;
                default:
                    return Result.Error<Unit>($"{nameof(direction)} type has no member '${direction}'.");
            }
            return Unit.Instance;
        }

        public Result<Unit> EmergencyStop()
        {
            _currentDirection = DoorDirection.Down;
            _currentSpeed = 0;

            return Unit.Instance;
        }

        public Result<Unit> CloseDoor() => Drive(DoorDirection.Down, DownSpeed);

        public Result<Unit> OpenDoor() => Drive(DoorDirection.Up, UpSpeed);

        public Result<Unit> TurnLightOn()
        {
            return Unit.Instance;
        }

        public Result<Unit> TurnLightOff()
        {
            return Unit.Instance;
        }

        public Result<bool> IsOpeningDoor() => _currentDirection == DoorDirection.Up;

        public Result<bool> IsClosingDoor() => _currentDirection == DoorDirection.Down;

        public Result<DoorDirection> GetDirection() => _currentDirection;

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

        public Result<SensorData> ReadSensorData()
        {
            _currentIndex = (_currentIndex + 1) % _sensorData.Length;
            return _sensorData[_currentIndex];
        }

        public Result<DoorInfo> GetDoorInfo()
        {
            return new DoorInfo
            {
                DoorState = DoorState.Unknown,
                DoorDirection = _currentDirection,
                Position = 20
            };
        }

        public void Dispose()
        {
            Console.WriteLine("Clean up sensors");

            _capture.Dispose();
        }
    }
}
