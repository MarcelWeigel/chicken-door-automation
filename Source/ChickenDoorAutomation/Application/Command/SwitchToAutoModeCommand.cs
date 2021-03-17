using Application.Driver;
using FunicularSwitch;

namespace Application.Command
{
    public class SwitchToAutoModeCommand
    {
        private readonly IChickenDoorDriver _driver;

        public SwitchToAutoModeCommand(IChickenDoorDriver driver)
        {
            _driver = driver;
        }

        public Result<Unit> Switch() => _driver.SwitchToAuto();
    }
}
