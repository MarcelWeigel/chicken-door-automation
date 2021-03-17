using WebApi.HypermediaExtensions.Hypermedia.Actions;

namespace ChickenDoorWebHost.Controller.Light.Actions
{
    public class TurnLightOnAction : HypermediaAction
    {
        public TurnLightOnAction(bool canExecute) : base(() => canExecute, null)
        {
        }

        public override object GetPrefilledParameter()
        {
            return null;
        }
    }
}
