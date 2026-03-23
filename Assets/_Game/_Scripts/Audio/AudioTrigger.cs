using UnityEngine;

public class AudioTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        Manual, OnAwake, OnStart, OnEnable, OnTriggerEnter, OnTriggerExit, OnCollisionEnter
    }

    [Header("Dependencies")]
    [Tooltip("If left blank, it will look for an AudioPlayer on this object.")]
    public AudioPlayer audioPlayer;
    public AudioPreset audioPreset;

    [Header("Trigger Conditions")]
    public TriggerType playOn = TriggerType.Manual;
    public bool playOnce = false;

    [Header("Filters")]
    public string filterTag = "Player";
    public LayerMask filterLayer = ~0;

    private AudioHandle currentHandle;
    private bool hasPlayed = false;
    private bool isOccupied = false;

    public void ExecuteTrigger()
    {
        if (playOnce && hasPlayed) return;
        if (audioPlayer == null || audioPreset == null) return;

        currentHandle = audioPlayer.Play(audioPreset);
        hasPlayed = true;
    }

    private void Awake()
    {
        if (audioPlayer == null)
        {
            audioPlayer = GetComponent<AudioPlayer>();
        }

        if (playOn == TriggerType.OnAwake) 
            ExecuteTrigger();
    }

    private void Start()
    {
        if (playOn == TriggerType.OnStart) 
            ExecuteTrigger();
    }

    private void OnEnable()
    {
        if (playOn == TriggerType.OnEnable) 
            ExecuteTrigger();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (CheckTag(other.gameObject) && CheckLayer(other.gameObject))
        {
            if (!isOccupied)
            {
                isOccupied = true;
                if (playOn == TriggerType.OnTriggerEnter) 
                    ExecuteTrigger();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (CheckTag(other.gameObject) && CheckLayer(other.gameObject))
        {
            isOccupied = false;
            if (playOn == TriggerType.OnTriggerExit) 
                ExecuteTrigger();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (CheckTag(collision.gameObject) && CheckLayer(collision.gameObject))
        {
            if (!isOccupied)
            {
                isOccupied = true;
                if (playOn == TriggerType.OnCollisionEnter) 
                    ExecuteTrigger();
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (CheckTag(collision.gameObject) && CheckLayer(collision.gameObject))
        {
            isOccupied = false;
        }
    }

    private bool CheckTag(GameObject go)
    {
        if (string.IsNullOrEmpty(filterTag)) return true;
        return go.CompareTag(filterTag);
    }

    private bool CheckLayer(GameObject go)
    {
        return (filterLayer.value & (1 << go.layer)) != 0;
    }
}