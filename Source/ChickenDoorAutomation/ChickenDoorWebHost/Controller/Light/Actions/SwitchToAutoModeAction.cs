using WebApi.HypermediaExtensions.Hypermedia.Actions;

namespace ChickenDoorWebHost.Controller.Light.Actions
{
    public class SwitchToAutoModeAction : HypermediaAction
    {
        public SwitchToAutoModeAction(bool canExecute) : base(() => canExecute, null)
        {
        }

        public override object GetPrefilledParameter()
        {
            return null;
        }
    }
}
