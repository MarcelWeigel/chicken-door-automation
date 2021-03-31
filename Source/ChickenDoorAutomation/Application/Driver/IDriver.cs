using System.Drawing;
using ChickenDoorDriver;
using FunicularSwitch;

namespace Application.Driver
{
    public interface IDriver
    {
        Result<Unit> Init();
        Result<Unit> EmergencyStop();
        Result<Unit> CloseDoor();
        Result<Unit> OpenDoor();
        Result<bool> IsOpeningDoor();
        Result<bool> IsClosingDoor();
        Result<DoorDirection> GetDirection();
        Result<string> ReadHeatMap();
        Result<string> ReadDistance();
    }
}
