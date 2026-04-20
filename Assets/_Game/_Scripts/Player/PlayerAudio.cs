using UnityEngine;


public class PlayerAudio : MonoBehaviour
{
    [Header("Audio Players")]
    [SerializeField] private AudioPlayer head;
    [SerializeField] private AudioPlayer feet;

    [Header("Movement Audio")]
    [SerializeField] private AudioPreset teleport;
    [SerializeField] private AudioPreset shiftMove;
    [SerializeField] private AudioPreset snapTurn;
    [SerializeField] private AudioPreset shiftTurn;
    [SerializeField] private AudioPreset footstep;

    [Header("Interaction Audio")]
    [SerializeField] private AudioPreset grab;
    [SerializeField] private AudioPreset drop;
    [SerializeField] private AudioPreset hover;

    [Header("Freezing Audio")]
    [SerializeField] private AudioPreset freezing;

    private AudioHandle freezingHandle;

    private void OnEnable()
    {
        PlayerEvents.OnTeleportEnded.AddListener(PlayTeleport);
        PlayerEvents.OnShiftStarted.AddListener(PlayShiftMove);
        PlayerEvents.OnSnapTurn.AddListener(PlaySnapTurn);
        PlayerEvents.OnShiftTurnStarted.AddListener(PlayShiftTurn);
        PlayerEvents.OnObjectGrabbed.AddListener(PlayGrab);
        PlayerEvents.OnObjectReleased.AddListener(PlayDrop);
        PlayerEvents.OnHoverEnter.AddListener(PlayHover);
        PlayerEvents.OnStepTaken.AddListener(PlayFootSteps);
        PlayerEvents.OnFreezingStart.AddListener(PlayFreezing);
        PlayerEvents.OnFreezingEnd.AddListener(StopFreezing);
    }

    private void OnDisable()
    {
        PlayerEvents.OnTeleportEnded.RemoveListener(PlayTeleport);
        PlayerEvents.OnShiftStarted.RemoveListener(PlayShiftMove);
        PlayerEvents.OnSnapTurn.RemoveListener(PlaySnapTurn);
        PlayerEvents.OnShiftTurnStarted.RemoveListener(PlayShiftTurn);
        PlayerEvents.OnObjectGrabbed.RemoveListener(PlayGrab);
        PlayerEvents.OnObjectReleased.RemoveListener(PlayDrop);
        PlayerEvents.OnHoverEnter.RemoveListener(PlayHover);
        PlayerEvents.OnStepTaken.RemoveListener(PlayFootSteps);
        PlayerEvents.OnFreezingStart.RemoveListener(PlayFreezing);
        PlayerEvents.OnFreezingEnd.RemoveListener(StopFreezing);
    }


    private void PlayFreezing(float duration)
    {
        if(head != null)
        {
            freezingHandle = head.Play(freezing);
            head.FadeTo(freezingHandle, 0f, 0f);
            head.FadeTo(freezingHandle, 1f, duration);
        }
    }

    private void StopFreezing()
    {
        head.StopSound(freezingHandle);
    }
    
    private void PlayFootSteps()
    {
        feet?.Play(footstep);
    }

    private void PlayTeleport()
    {
        head?.Play(teleport);
    }

    private void PlayShiftMove()
    {
        head?.Play(shiftMove);
    }

    private void PlaySnapTurn()
    {
        head?.Play(snapTurn);
    }

    private void PlayShiftTurn()
    {
        head?.Play(shiftTurn);
    }

    private void PlayGrab(GameObject target)
    {
        head?.Play(grab);
    }

    private void PlayDrop(GameObject target)
    {
        head?.Play(drop);
    }

    private void PlayHover()
    {
        head?.Play(hover);
    }
}