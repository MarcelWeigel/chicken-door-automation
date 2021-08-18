using System.Threading.Tasks;
using Application.Driver;
using FunicularSwitch;

namespace Application.Command
{
    public class OpenDoorCommand
    {
        private readonly IDriver _driver;

        public OpenDoorCommand(IDriver driver)
        {
            _driver = driver;
        }

        public Task<Result<Unit>> Open() => _driver.OpenDoor();
    }
}
