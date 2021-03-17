using Application.Driver;
using FunicularSwitch;

namespace Application.Command
{
    public class TurnLightOnCommand
    {
        private readonly IChickenDoorDriver _driver;

        public TurnLightOnCommand(IChickenDoorDriver driver)
        {
            _driver = driver;
        }

        public Result<Unit> Turn() => _driver.TurnLightOn();
    }
}
