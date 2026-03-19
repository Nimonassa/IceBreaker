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
    [SerializeField] private AudioSource earSource;
    [SerializeField] private AudioSource footSource;


    [Header("Movement Audio")]
    [SerializeField] private AudioPreset teleport;
    [SerializeField] private AudioPreset snapTurn;
    [SerializeField] private AudioPreset footstep;
    [SerializeField] private float stepDistance = 1.6f; // Average distance between steps
    [SerializeField, Range(0f, 1f)] private float stepDistanceVariance = 0.2f; // Random variance added/subtracted per step


    [Header("Interaction Audio")]
    [SerializeField] private AudioPreset grab;
    [SerializeField] private AudioPreset drop;
    [SerializeField] private AudioPreset hover;

    private Vector3 _lastPosition;
    private float _distanceAccumulator;
    private float _currentTargetDistance; // The actual distance required for the *next* step

    private void Start()
    {
        if (movement != null)
        {
            _lastPosition = movement.transform.position;
        }

        // Initialize the very first step target
        CalculateNextStepTarget();
    }

    private void Update()
    {
        HandleContinuousFootsteps();
    }

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

    private void HandleContinuousFootsteps()
    {
        if (movement.CurrentLocomotion != MoveType.Continuous)
            return;

        float distanceMoved = Vector3.Distance(movement.transform.position, _lastPosition);
        _lastPosition = movement.transform.position;
        _distanceAccumulator += distanceMoved;

        if (_distanceAccumulator >= _currentTargetDistance)
        {
            _distanceAccumulator = 0f;
            CalculateNextStepTarget(); 
            footstep?.Play(earSource);
        }
    }

    private void CalculateNextStepTarget()
    {
        _currentTargetDistance = stepDistance + Random.Range(-stepDistanceVariance, stepDistanceVariance);
        _currentTargetDistance = Mathf.Max(0.1f, _currentTargetDistance);
    }

    private void PlayTeleport()
    {
        Debug.Log("Play Teleport!");
        teleport?.Play(earSource);
    }

    private void PlaySnapTurn()
    {
        Debug.Log("Play Snap Turn!");
        snapTurn?.Play(earSource);
    }

    private void PlayGrab(GameObject target)
    {
        Debug.Log("Play Grab!");
        grab?.Play(earSource);
    }

    private void PlayDrop(GameObject target)
    {
        Debug.Log("Play Drop!");
        drop?.Play(earSource);
    }

    private void PlayHover()
    {
        Debug.Log("Play Hover!");
        hover?.Play(earSource);
    }
}