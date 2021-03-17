using System.Threading.Tasks;
using Application.Command;
using Application.Query;
using ChickenDoorWebHost.Controller.Light.Actions;
using Microsoft.AspNetCore.Mvc;
using WebApi.HypermediaExtensions.WebApi.AttributedRoutes;

namespace ChickenDoorWebHost.Controller.Light
{
    [Route("api/[controller]")]
    public class LightController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly LightQuery _lightQuery;
        private readonly TurnLightOnCommand _turnLightOnCommand;
        private readonly TurnLightOffCommand _turnLightOffCommand;
        private readonly SwitchToManuelModeCommand _switchToManuelModeCommand;
        private readonly SwitchToAutoModeCommand _switchToAutoModeCommand;

        public LightController(LightQuery lightQuery, TurnLightOnCommand turnLightOnCommand, TurnLightOffCommand turnLightOffCommand, SwitchToManuelModeCommand switchToManuelModeCommand, SwitchToAutoModeCommand switchToAutoModeCommand)
        {
            _lightQuery = lightQuery;
            _turnLightOnCommand = turnLightOnCommand;
            _turnLightOffCommand = turnLightOffCommand;
            _switchToManuelModeCommand = switchToManuelModeCommand;
            _switchToAutoModeCommand = switchToAutoModeCommand;
        }

        [HttpGetHypermediaObject(typeof(LightHto))]
        public ActionResult GetLight() => 
            _lightQuery
                .Get()
                .Match(status => 
                    Ok(new LightHto(status)), 
                    error => Problem(error));

        [HttpPostHypermediaAction("TurnOn", typeof(TurnLightOnAction))]
        public ActionResult TurnOn() =>
            _turnLightOnCommand.Turn()
                .Match(
                c => Ok(StatusCode(200)),
                error => Problem(error));

        [HttpPostHypermediaAction("TurnOff", typeof(TurnLightOffAction))]
        public ActionResult TurnOff() =>
            _turnLightOffCommand.Turn()
                .Match(
                    c => Ok(StatusCode(200)),
                    error => Problem(error));

        [HttpPostHypermediaAction("SwitchToManuel", typeof(SwitchToManuelModeAction))]
        public ActionResult SwitchToManuel() =>
            _switchToManuelModeCommand.Switch()
                .Match(
                    c => Ok(StatusCode(200)),
                    error => Problem(error));

        [HttpPostHypermediaAction("SwitchToAuto", typeof(SwitchToAutoModeAction))]
        public ActionResult SwitchToAuto() =>
            _switchToAutoModeCommand.Switch()
                .Match(
                    c => Ok(StatusCode(200)),
                    error => Problem(error));
    }
}
