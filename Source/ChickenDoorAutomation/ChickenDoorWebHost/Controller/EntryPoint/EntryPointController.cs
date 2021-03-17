using Microsoft.AspNetCore.Mvc;
using WebApi.HypermediaExtensions.WebApi.AttributedRoutes;

namespace ChickenDoorWebHost.Controller.EntryPoint
{
    [Route("api/[controller]")]
    [ApiController]
    public class EntryPointController : Microsoft.AspNetCore.Mvc.Controller
    {
        [HttpGetHypermediaObject(typeof(EntryPointHto))]
        public ActionResult GetEntryPoint() => Ok(new EntryPointHto());
    }
}
