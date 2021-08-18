using System.Threading.Tasks;
using ChickenDoorDriver;

namespace Driver
{
    public interface IExternalNotification
    {
        Task Notify(DoorState doorState, string cameraImage);
    }
}