using UnityEngine;


public class PlayerAudio : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerGrabbing playerGrabbing;
    [SerializeField] private PlayerFeet playerFeet;

    [Header("Audio Players")]
    [SerializeField] private AudioPlayer head;
    [SerializeField] private AudioPlayer feet;

    [Header("Movement Audio")]
    [SerializeField] private AudioPreset teleport;
    [SerializeField] private AudioPreset snapTurn;
    [SerializeField] private AudioPreset footstep;

    [Header("Interaction Audio")]
    [SerializeField] private AudioPreset grab;
    [SerializeField] private AudioPreset drop;
    [SerializeField] private AudioPreset hover;

    private void Reset()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
        playerGrabbing = GetComponentInParent<PlayerGrabbing>();
        playerFeet = GetComponentInChildren<PlayerFeet>();
    }

    private void OnEnable()
    {
        playerMovement?.events?.onTeleport?.AddListener(PlayTeleport);
        playerMovement?.events?.onSnapTurn?.AddListener(PlaySnapTurn);
        playerGrabbing?.events?.onObjectGrabbed?.AddListener(PlayGrab);
        playerGrabbing?.events?.onObjectReleased?.AddListener(PlayDrop);
        playerGrabbing?.events?.onHoverEnter?.AddListener(PlayHover);
        playerFeet?.events?.onStepTaken?.AddListener(PlayFootSteps);
    }

    private void OnDisable()
    {
        playerMovement?.events?.onTeleport?.RemoveListener(PlayTeleport);
        playerMovement?.events?.onSnapTurn?.RemoveListener(PlaySnapTurn);
        playerGrabbing?.events?.onObjectGrabbed?.RemoveListener(PlayGrab);
        playerGrabbing?.events?.onObjectReleased?.RemoveListener(PlayDrop);
        playerGrabbing?.events?.onHoverEnter?.RemoveListener(PlayHover);
        playerFeet?.events?.onStepTaken?.RemoveListener(PlayFootSteps);
    }


    private void PlayFootSteps()
    {
        Debug.Log("Play footstep!");
        feet?.Play(footstep);
    }

    private void PlayTeleport()
    {
        Debug.Log("Play teleport!");
        head?.Play(teleport);
    }

    private void PlaySnapTurn()
    {
        Debug.Log("Play snapturn!");
        head?.Play(snapTurn);
    }

    private void PlayGrab(GameObject target)
    {
        Debug.Log("Play grab!");
        head?.Play(grab);
    }

    private void PlayDrop(GameObject target)
    {
        Debug.Log("Play drop!");
        head?.Play(drop);
    }

    private void PlayHover()
    {
        Debug.Log("Play hover!");
        head?.Play(hover);
    }
}