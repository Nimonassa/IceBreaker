using UnityEngine;

[System.Serializable]
public class AudioData
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1.0f;
}


public class PlayerAudio : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerGrabbing grabbing;

    [Header("Audio Source")]
    [SerializeField] private AudioSource source;

    [Space(20)]
    [Header("Movement Audio")]
    [SerializeField] private AudioPreset teleport;
    [SerializeField] private AudioPreset snapTurn;

    [Header("Interaction Audio")]
    [SerializeField] private AudioPreset grab;
    [SerializeField] private AudioPreset drop;
    [SerializeField] private AudioPreset hover;

    private void Reset()
    {
        movement = GetComponentInParent<PlayerMovement>();
        grabbing = GetComponentInParent<PlayerGrabbing>();
    }

    private void OnEnable()
    {
        if (movement != null)
        {
            movement.events.onTeleport.AddListener(PlayTeleport);
            movement.events.onSnapTurn.AddListener(PlaySnapTurn);
        }

        if (grabbing != null)
        {
            grabbing.events.onObjectGrabbed.AddListener(PlayGrab);
            grabbing.events.onObjectReleased.AddListener(PlayDrop);
            grabbing.events.onHoverEnter.AddListener(PlayHover);
        }
    }

    private void OnDisable()
    {
        if (movement != null)
        {
            movement.events.onTeleport.RemoveListener(PlayTeleport);
            movement.events.onSnapTurn.RemoveListener(PlaySnapTurn);
        }

        if (grabbing != null)
        {
            grabbing.events.onObjectGrabbed.RemoveListener(PlayGrab);
            grabbing.events.onObjectReleased.RemoveListener(PlayDrop);
            grabbing.events.onHoverEnter.RemoveListener(PlayHover);
        }
    }

    private void PlayTeleport()
    {
        teleport?.Play(source);
    }
    private void PlaySnapTurn()
    {
        snapTurn?.Play(source);
    }

    private void PlayGrab(GameObject target)
    {
        grab?.Play(source);
    }
    private void PlayDrop(GameObject target)
    {
        drop?.Play(source);
    }

    private void PlayHover()
    {
        hover?.Play(source);
    }
}