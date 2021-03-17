using Application.Driver;
using Domain.Light;
using FunicularSwitch;

namespace Application.Query
{
    public class LightQuery
    {
        private readonly IChickenDoorDriver _driver;

        public LightQuery(IChickenDoorDriver driver)
        {
            _driver = driver;
        }

        public Result<Light> Get() => _driver.GetLight();
    }
}
