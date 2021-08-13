using ChickenDoorDriver;

namespace Application.Driver
{
    public class DoorInfo
    {
        public DoorState DoorState { get; set; }
        public DoorDirection DoorDirection { get; set; }
        public double Position { get; set; }
        public double CpuTemperature { get; set; }
    }
}
