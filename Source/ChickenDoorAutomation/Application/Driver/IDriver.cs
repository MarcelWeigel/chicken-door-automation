using System;
using System.Threading.Tasks;
using ChickenDoorDriver;
using FunicularSwitch;

namespace Application.Driver
{
    public class SensorData
    {
        public double[] HeatMap { get; set; }
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

    public interface IDriver : IDisposable
    {
        Result<Unit> Start();
        Task<Result<Unit>> EmergencyStop();
        Task<Result<Unit>> CloseDoor();
        Task<Result<Unit>> OpenDoor();
        Result<Unit> TurnLightOn();
        Result<Unit> TurnLightOff();
        Result<bool> IsOpeningDoor();
        Result<bool> IsClosingDoor();
        Result<DoorDirection> GetDirection();
        Result<SensorData> ReadSensorData();
        Result<DoorInfo> GetDoorInfo();
        Result<string> ReadVideoCapture();
    }
}
