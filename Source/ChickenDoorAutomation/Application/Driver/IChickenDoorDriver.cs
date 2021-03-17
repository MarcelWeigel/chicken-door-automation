using Domain.Light;
using FunicularSwitch;

namespace Application.Driver
{
    public interface IChickenDoorDriver
    {
        Result<Light> GetLight();
        Result<Unit> TurnLightOn();
        Result<Unit> TurnLightOff();
        Result<Unit> SwitchToManuel();
        Result<Unit> SwitchToAuto();
    }
}
