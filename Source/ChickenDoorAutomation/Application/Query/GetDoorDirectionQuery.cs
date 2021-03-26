using Application.Driver;
using ChickenDoorDriver;
using FunicularSwitch;

namespace Application.Query
{
    public class GetDoorDirectionQuery
    {
        private readonly IDriver _driver;

        public GetDoorDirectionQuery(IDriver driver)
        {
            _driver = driver;
        }

        public Result<DoorDirection> Get() => _driver.GetDirection();
    }
}
