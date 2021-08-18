using System.Threading.Tasks;
using Application.Driver;
using FunicularSwitch;

namespace Application.Command
{
    public class CloseDoorCommand
    {
        private readonly IDriver _driver;

        public CloseDoorCommand(IDriver driver)
        {
            _driver = driver;
        }

        public Task<Result<Unit>> Close() => _driver.CloseDoor();
    }
}
