using System;
using UnityEngine;

public class XRButtonTarget : MonoBehaviour, IHandAction
{
    public event Action<ControllerSide> OnTriggerPressed;
    public event Action<ControllerSide> OnGripPressed;
    public event Action<ControllerSide> OnPrimaryPressed;
    public event Action<ControllerSide> OnSecondaryPressed;
    public event Action<ControllerSide> OnThumbstickPressed;

    public void OnTrigger(ControllerSide side) => OnTriggerPressed?.Invoke(side);
    public void OnGrip(ControllerSide side) => OnGripPressed?.Invoke(side);
    public void OnPrimary(ControllerSide side) => OnPrimaryPressed?.Invoke(side);
    public void OnSecondary(ControllerSide side) => OnSecondaryPressed?.Invoke(side);
    public void OnThumbstick(ControllerSide side) => OnThumbstickPressed?.Invoke(side);
}