using Application.Driver;
using FunicularSwitch;

namespace Application.Command
{
    public class SwitchToManuelModeCommand
    {
        private readonly IChickenDoorDriver _driver;

        public SwitchToManuelModeCommand(IChickenDoorDriver driver)
        {
            _driver = driver;
        }

        public Result<Unit> Switch() => _driver.SwitchToManuel();
    }
}
