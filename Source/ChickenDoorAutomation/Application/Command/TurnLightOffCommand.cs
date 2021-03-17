using Application.Driver;
using FunicularSwitch;

namespace Application.Command
{
    public class TurnLightOffCommand
    {
        private readonly IChickenDoorDriver _driver;

        public TurnLightOffCommand(IChickenDoorDriver driver)
        {
            _driver = driver;
        }

        public Result<Unit> Turn() => _driver.TurnLightOff();
    }
}
