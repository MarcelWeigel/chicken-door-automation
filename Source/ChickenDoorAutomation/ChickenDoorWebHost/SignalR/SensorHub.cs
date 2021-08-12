using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Application.Driver;
using Driver;
using Microsoft.AspNetCore.SignalR;
using Console = System.Console;

namespace ChickenDoorWebHost.SignalR
{
    public class SensorDataClient
    {
        public string? HeatMapBase64Image { get; set; }
        public bool HallTop { get; set; }
        public bool HallBottom { get; set; }
        public bool PhotoelectricBarrier { get; set; }
        public bool Taster { get; set; }
        public double Distance { get; set; }
        public double Temperature { get; set; }
        public double Pressure { get; set; }
        public double Humidity { get; set; }
        public double Altitude { get; set; }
        public double Illuminance { get; set; }
        public double[]? Gyroscope { get; set; }
        public double[]? Accelerometer { get; set; }
        public double[]? Magnetometer { get; set; }
    }

    public class DoorInfoClient
    {
        public string? DoorState { get; set; }
        public string? DoorDirection { get; set; }
        public double Position { get; set; }
    }

    public class SensorHub : Hub
    {
        private readonly IDriver _driver;
        private readonly IHubContext<SensorHub> _hubContext;
        private readonly DataPublisher _dataPublisher;
        private readonly ClientTracking _clientTracking;

        public SensorHub(IDriver driver, IHubContext<SensorHub> hubContext, DataPublisher dataPublisher, ClientTracking clientTracking)
        {
            _driver = driver;
            _hubContext = hubContext;
            _dataPublisher = dataPublisher;
            _clientTracking = clientTracking;
        }

        public async Task NewMessage(long username, string message)
        {
            await Clients.All.SendAsync("messageReceived", username, message);
        }

        public async Task ReadSensorData()
        {
            await _driver.ReadSensorData().Match(sensorData => _hubContext.Clients.All
                .SendAsync("sensorDataUpdated", Convert(sensorData)));
        }

        public async Task ReadVideoCapture()
        {
            await _driver.ReadVideoCapture().Match(videoCapture => _hubContext.Clients.All
                .SendAsync("videoCaptureUpdated", videoCapture));
        }
        public async Task ReadDoorInfo()
        {
            await _driver.GetDoorInfo().Match(doorInfo => _hubContext.Clients.All
                .SendAsync("doorInfoUpdated", Convert(doorInfo)));
        }

        public async Task PublishData()
        {
            await ReadVideoCapture();
            await ReadDoorInfo();
        }

        public void OpenDoor()
        {
            _driver.OpenDoor();
        }

        public void CloseDoor()
        {
            _driver.CloseDoor();
        }

        public void StopMotor()
        {
            _driver.EmergencyStop();
        }

        public void TurnLightOn()
        {
            _driver.TurnLightOn();
        }

        public void TurnLightOff()
        {
            _driver.TurnLightOff();
        }


        private SensorDataClient Convert(SensorData data)
        {
            return new SensorDataClient
            {
                HeatMapBase64Image = HeatMap.Base64HeatMapFromTemperature(data.HeatMap),
                HallTop = data.HallTop,
                HallBottom = data.HallBottom,
                PhotoelectricBarrier = data.PhotoelectricBarrier,
                Taster = data.Taster,
                Gyroscope = data.Gyroscope,
                Accelerometer = data.Accelerometer,
                Magnetometer = data.Magnetometer,
                Distance = data.Distance,
                Illuminance = data.Illuminance,
                Temperature = data.Temperature,
                Pressure = data.Pressure,
                Humidity = data.Humidity,
                Altitude = data.Altitude
            };
        }

        private DoorInfoClient Convert(DoorInfo doorInfo)
        {
            return new DoorInfoClient
            {
                DoorState = doorInfo.DoorState.ToString(),
                DoorDirection = doorInfo.DoorDirection.ToString(),
                Position = doorInfo.Position
            };
        }

        public override Task OnConnectedAsync()
        {
            if (_clientTracking.NumberOfConnectedClients == 0)
            {
                _dataPublisher.Start(PublishData);
            }
            _clientTracking.OnClientConnected(Context.ConnectionId);

            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception e)
        {
            _clientTracking.OnClientDisconnected(Context.ConnectionId);

            if (_clientTracking.NumberOfConnectedClients == 0)
            {
                _dataPublisher.Stop();
            }
            return Task.CompletedTask;
        }
    }

    public class ClientTracking
    {
        readonly object _lock = new object();
        HashSet<string> ConnectionIds { get; } = new HashSet<string>();

        public void OnClientConnected(string connectionId)
        {
            lock (_lock)
            {
                ConnectionIds.Add(connectionId);
                Console.WriteLine($"Hallo sagt Client {connectionId}. Connected clients: {ConnectionIds.Count}");
            }
        }

        public void OnClientDisconnected(string connectionId)
        {
            lock (_lock)
            {
                ConnectionIds.Remove(connectionId);
                Console.WriteLine($"Tschüss sagt Client {connectionId}. Connected clients: {ConnectionIds.Count}");
            }
        }

        public int NumberOfConnectedClients => ConnectionIds.Count;
    }

    public class DataPublisher
    {
        IDisposable? _action;
        readonly object _lock = new object();

        public void Start(Func<Task> action)
        {
            Console.WriteLine("Start publishing data");
            lock (_lock)
            {
                _action = Scheduler.Default.ScheduleAsync(async (scheduler, cancellationToken) =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await action().ConfigureAwait(false);
                        await scheduler.Sleep(TimeSpan.FromSeconds(0.5), cancellationToken).ConfigureAwait(false);
                    }
                });
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (_action != null)
                {
                    _action?.Dispose();
                    _action = null;
                    Console.WriteLine("Stopped publishing data");
                }
            }
        }
    }
}
