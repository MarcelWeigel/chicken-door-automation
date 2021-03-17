using ChickenDoorWebHost.Controller.Light;
using WebApi.HypermediaExtensions.Hypermedia;
using WebApi.HypermediaExtensions.Hypermedia.Attributes;
using WebApi.HypermediaExtensions.Hypermedia.Links;

namespace ChickenDoorWebHost.Controller.EntryPoint
{
    [HypermediaObject(Title = "Einstiegspunkt", Classes = new[] { "EntryPoint" })]
    public class EntryPointHto : HypermediaObject
    {
        public EntryPointHto()
        {
            Links.Add("Light", new HypermediaObjectKeyReference(typeof(LightHto)));
        }
    }
}
