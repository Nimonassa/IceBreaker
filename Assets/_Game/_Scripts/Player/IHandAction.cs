public interface IHandAction
{
    void OnTrigger(ControllerSide side) {}
    void OnGrip(ControllerSide side) {}
    void OnPrimary(ControllerSide side) {}
    void OnSecondary(ControllerSide side) {}
    void OnThumbstick(ControllerSide side) {}
}

