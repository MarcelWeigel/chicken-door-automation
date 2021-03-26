using ChickenDoorDriver;
using ChickenDoorWebHost.Controller.Door.Actions;
using WebApi.HypermediaExtensions.Hypermedia;
using WebApi.HypermediaExtensions.Hypermedia.Attributes;

namespace ChickenDoorWebHost.Controller.Door
{
    public class DoorHto : HypermediaObject
    {
        public DoorDirection Direction { get; }

        [HypermediaAction(Name = "CloseDoor", Title = "CloseDoor")]
        public CloseDoorAction CloseDoor { get; }

        [HypermediaAction(Name = "OpenDoor", Title = "OpenDoor")]
        public OpenDoorAction OpenDoor { get; }

        public DoorHto(DoorDirection direction)
        {
            Direction = direction;

            CloseDoor = new CloseDoorAction(direction == DoorDirection.None);
            OpenDoor = new OpenDoorAction(direction == DoorDirection.None);
        }
    }
}
