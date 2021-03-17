namespace Domain.Light
{
    public class Light
    {
        public LightMode LightMode { get; set; }
        public LightState LightState { get; set; }

        public Light(LightMode lightMode, LightState lightState)
        {
            LightMode = lightMode;
            LightState = lightState;
        }
    }
}
