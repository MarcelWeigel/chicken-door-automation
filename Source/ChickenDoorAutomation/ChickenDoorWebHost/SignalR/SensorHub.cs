using System.Threading;
using System.Threading.Tasks;
using Application.Driver;
using Driver;
using Microsoft.AspNetCore.SignalR;

namespace ChickenDoorWebHost.SignalR
{
    public class SensorDataClient
    {
        public string HeatMapBase64Image { get; set; }
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
        public double[] Gyroscope { get; set; }
        public double[] Accelerometer { get; set; }
        public double[] Magnetometer { get; set; }
    }

    public class DoorInfoClient
    {
        public string DoorState { get; set; }
        public string DoorDirection { get; set; }
        public double Position { get; set; }
    }

    public class SensorHub : Hub
    {
        private readonly IDriver _driver;
        private bool _isRunning;
        private CancellationTokenSource _tokenSource;

        public SensorHub(IDriver driver)
        {
            _driver = driver;
        }

        public async Task NewMessage(long username, string message)
        {
            await Clients.All.SendAsync("messageReceived", username, message);
        }

        public async Task ReadSensorData()
        {
            await _driver.ReadSensorData().Match(sensorData => Clients.All.SendAsync("sensorDataUpdated", Convert(sensorData)));
        }

        public void OpenDoor()
        {
            _driver.OpenDoor();
        }

        public void CloseDoor()
        {
            _driver.CloseDoor();
        }

        public async Task ReadDoorInfo()
        {
            await _driver.GetDoorInfo().Match(doorInfo => Clients.All.SendAsync("doorInfoUpdated", Convert(doorInfo)));
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
    }
}
