namespace ChickenDoorDriver.Motor
{
    public interface IMotorDriver
    {
        void Up();
        void Down();
        bool IsUp();
        bool IsDown();
        bool IsMoving();
    }
}
