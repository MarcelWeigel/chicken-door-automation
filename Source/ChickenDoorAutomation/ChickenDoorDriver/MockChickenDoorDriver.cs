using Application.Driver;
using Domain.Light;
using FunicularSwitch;

namespace ChickenDoorDriver
{
    public class MockChickenDoorDriver : IChickenDoorDriver
    {
        private Light _light = new Light(LightMode.Manuel, LightState.Off);

        public Result<Light> GetLight()
        {
            return _light;
        }

        public Result<Unit> TurnLightOn()
        {
            _light.LightState = LightState.On;

            return Unit.Instance;
        }

        public Result<Unit> TurnLightOff()
        {
            _light.LightState = LightState.Off;

            return Unit.Instance;
        }

        public Result<Unit> SwitchToManuel()
        {
            _light.LightMode = LightMode.Manuel;

            return Unit.Instance;
        }

        public Result<Unit> SwitchToAuto()
        {
            _light.LightMode = LightMode.Auto;

            return Unit.Instance;
        }
    }
}
