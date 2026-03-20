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


    [Header("Audio Players")]
    [SerializeField] private AudioPlayer ears;
    [SerializeField] private AudioPlayer feet;


    [Header("Movement Audio")]
    [SerializeField] private AudioPreset teleport;
    [SerializeField] private AudioPreset snapTurn;
    [SerializeField] private AudioPreset footstep;
    [SerializeField] private float stepDistance = 0.75f; 
    [SerializeField, Range(0f, 1f)] private float stepDistanceVariance = 0.05f;


    [Header("Interaction Audio")]
    [SerializeField] private AudioPreset grab;
    [SerializeField] private AudioPreset drop;
    [SerializeField] private AudioPreset hover;

    private Vector3 _lastPosition;
    private float _distanceAccumulator;
    private float _currentTargetDistance;


    private void Start()
    {
        if (movement != null)
        {
            _lastPosition = movement.transform.position;
        }

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

        if (!movement.IsGrounded())
        {
            Debug.Log("player is not grounded!");
            _lastPosition = movement.transform.position;
            return;
        }
        

        Vector3 currentPos = movement.transform.position;
        Vector3 flatCurrent = new Vector3(currentPos.x, 0, currentPos.z);
        Vector3 flatLast = new Vector3(_lastPosition.x, 0, _lastPosition.z);
        
        float distanceMoved = Vector3.Distance(flatCurrent, flatLast);
        
        _lastPosition = currentPos;
        _distanceAccumulator += distanceMoved;

        if (_distanceAccumulator >= _currentTargetDistance)
        {
            _distanceAccumulator = 0f;
            CalculateNextStepTarget();
            PlayFootSteps();
        }
    }

    private void CalculateNextStepTarget()
    {
        _currentTargetDistance = stepDistance + Random.Range(-stepDistanceVariance, stepDistanceVariance);
        _currentTargetDistance = Mathf.Max(0.1f, _currentTargetDistance);
    }

    private void PlayFootSteps()
    {
        feet?.Play(footstep);
    }

    private void PlayTeleport()
    {
        ears?.Play(teleport);
    }

    private void PlaySnapTurn()
    {
        ears?.Play(snapTurn);
    }

    private void PlayGrab(GameObject target)
    {
        ears?.Play(grab);
    }

    private void PlayDrop(GameObject target)
    {
        ears?.Play(drop);
    }

    private void PlayHover()
    {
        ears?.Play(hover);
    }
}