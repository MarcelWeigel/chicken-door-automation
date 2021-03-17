using WebApi.HypermediaExtensions.Hypermedia.Actions;

namespace ChickenDoorWebHost.Controller.Light.Actions
{
    public class SwitchToManuelModeAction : HypermediaAction
    {
        public SwitchToManuelModeAction(bool canExecute) : base(() => canExecute, null)
        {
        }

        public override object GetPrefilledParameter()
        {
            return null;
        }
    }
}
