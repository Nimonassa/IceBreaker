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