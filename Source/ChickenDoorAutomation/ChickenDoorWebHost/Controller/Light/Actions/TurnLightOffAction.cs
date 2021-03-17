using System;
using WebApi.HypermediaExtensions.Hypermedia.Actions;

namespace ChickenDoorWebHost.Controller.Light.Actions
{
    public class TurnLightOffAction :  HypermediaAction
    {
        public TurnLightOffAction(bool canExecute) : base(() => canExecute, null)
        {
        }

        public override object GetPrefilledParameter()
        {
            return null;
        }
    }
}
