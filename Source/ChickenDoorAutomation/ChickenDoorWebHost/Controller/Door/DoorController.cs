using System.Threading.Tasks;
using Application.Command;
using Application.Query;
using ChickenDoorWebHost.Controller.Door.Actions;
using FunicularSwitch;
using Microsoft.AspNetCore.Mvc;
using WebApi.HypermediaExtensions.WebApi.AttributedRoutes;

namespace ChickenDoorWebHost.Controller.Door
{
    [Route("api/[controller]")]
    public class DoorController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly OpenDoorCommand _openDoorCommand;
        private readonly CloseDoorCommand _closeDoorCommand;
        private readonly GetDoorDirectionQuery _getDoorDirectionQuery;
        public DoorController(OpenDoorCommand openDoorCommand, CloseDoorCommand closeDoorCommand, GetDoorDirectionQuery getDoorDirectionQuery)
        {
            _openDoorCommand = openDoorCommand;
            _closeDoorCommand = closeDoorCommand;
            _getDoorDirectionQuery = getDoorDirectionQuery;
        }

        [HttpGetHypermediaObject(typeof(DoorHto))]
        public ActionResult GetDoor() =>
            _getDoorDirectionQuery
                .Get()
                .Match(direction =>
                        Ok(new DoorHto(direction)),
                    error => Problem(error));

        [HttpPostHypermediaAction("OpenDoor", typeof(OpenDoorAction))]
        public Task<ObjectResult> OpenDoor() =>
            _openDoorCommand.Open()
                .Match(
                    c => Ok(StatusCode(200)),
                    error => Problem(error));

        [HttpPostHypermediaAction("CloseDoor", typeof(CloseDoorAction))]
        public Task<ObjectResult> CloseDoor() =>
            _closeDoorCommand.Close()
                .Match(
                    c => Ok(StatusCode(200)),
                    error => Problem(error));
    }
}
