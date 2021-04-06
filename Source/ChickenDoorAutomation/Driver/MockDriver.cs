using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Driver;
using ChickenDoorDriver;
using FunicularSwitch;

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

        public Result<Unit> Init()
        {
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
                        _currentDirection = DoorDirection.None;
                        _isRunning = false;
                    }
                    else if (_currentDirection != DoorDirection.None)
                    {
                        if (_driveCounter <= 0)
                        {
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

        public Result<bool> IsOpeningDoor() => _currentDirection == DoorDirection.Up;

        public Result<bool> IsClosingDoor() => _currentDirection == DoorDirection.Down;

        public Result<DoorDirection> GetDirection() => _currentDirection;
        public Result<SensorData> ReadSensorData()
        {
            var r = new Random();
            return new SensorData()
            {
                HeatMap = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAGAAAABgCAYAAADimHc4AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsIAAA7CARUoSoAAAAEVSURBVHhe7duxDQIxEABBQxdUgUho4juigO+ImigDEhfwyWstNJPYoaXVZb7L/fH8DjLXeRIRICZATICYADEBYgLEBIgJEBMgJkBMgJgAMQFiAsQEiAkQEyAmQEyAmAAxAWICxE7/F7S/P/N2zGu7zds5VnuPCYgJEBMgJkBMgJgAMQFiAsQEiAkQEyBmRyxmAmICxASICRATICZATICYADEBYgLEBIgJEBMgJkBMgJgAMQFiAsQEiAkQEyAmQEyA2HI7YquxI/bnBIgJEBMgJkBMgJgAMQFiAsQEiAkQsyMWMwExAWICxASICRATICZATICYADEBYgLEBIgJEBMgJkBMgJgAMQFiAsQEiAkQEyAmQGqMHwwcEaU0wlqLAAAAAElFTkSuQmCC",
                Distance = r.Next(50, 145),
                Magnetometer = new double[3] { r.Next(50, 145) , r.Next(50, 145) , r.Next(50, 145) }
            };
        }
    }
}
