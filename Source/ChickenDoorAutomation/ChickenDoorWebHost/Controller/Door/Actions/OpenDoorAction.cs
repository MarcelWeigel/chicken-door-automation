using WebApi.HypermediaExtensions.Hypermedia.Actions;

namespace ChickenDoorWebHost.Controller.Door.Actions
{
    public class OpenDoorAction : HypermediaAction
    {
        public OpenDoorAction(bool canExecute) : base(() => canExecute, null)
        {
        }

        public override object GetPrefilledParameter()
        {
            return null;
        }
    }
}
