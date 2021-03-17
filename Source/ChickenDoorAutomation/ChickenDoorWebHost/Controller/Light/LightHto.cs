using ChickenDoorWebHost.Controller.Light.Actions;
using Domain.Light;
using WebApi.HypermediaExtensions.Hypermedia;
using WebApi.HypermediaExtensions.Hypermedia.Attributes;

namespace ChickenDoorWebHost.Controller.Light
{
    public class LightHto : HypermediaObject
    {
        public LightMode Mode { get; }
        public LightState State { get; }

        [HypermediaAction(Name = "Turn Light On", Title = "Turn Light On")]
        public TurnLightOnAction TurnOn { get; }

        [HypermediaAction(Name = "Turn Light Off", Title = "Turn Light Off")]
        public TurnLightOffAction TurnOff { get; }

        [HypermediaAction(Name = "Switch To Manuel Mode", Title = "Switch To Manuel Mode")]
        public SwitchToManuelModeAction SwitchToManuelMode { get; }

        [HypermediaAction(Name = "Switch To Auto Mode", Title = "Switch To Auto Mode")]
        public SwitchToAutoModeAction SwitchToAutoMode { get; }

        public LightHto(Domain.Light.Light light)
        {
            Mode = light.LightMode;
            State = light.LightState;

            TurnOn = new TurnLightOnAction(light.LightState == Domain.Light.LightState.Off && light.LightMode == Domain.Light.LightMode.Manuel);
            TurnOff = new TurnLightOffAction(light.LightState == Domain.Light.LightState.On && light.LightMode == Domain.Light.LightMode.Manuel);
            SwitchToManuelMode = new SwitchToManuelModeAction(light.LightMode == Domain.Light.LightMode.Auto);
            SwitchToAutoMode = new SwitchToAutoModeAction(light.LightMode == Domain.Light.LightMode.Manuel);
        }

    }
}
