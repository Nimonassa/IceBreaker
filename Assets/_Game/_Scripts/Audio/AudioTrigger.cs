using UnityEngine;

public enum AudioTriggerType
{
    Manual,
    OnAwake,
    OnStart,
    OnEnable,
    OnTriggerEnter,
    OnTriggerExit,
    OnCollisionEnter
}

public class AudioTrigger : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private AudioPlayer audioPlayer;
    [SerializeField] private AudioPreset audioPreset;

    [Header("Trigger Settings")]
    public AudioTriggerType triggerType = AudioTriggerType.Manual;

    [Tooltip("If true, the sound will only ever play once per scene load.")]
    public bool triggerOnce = false;
    public string filterTag = "Player";
    private bool hasPlayed = false;

    private void Awake()
    {
        if (audioPlayer == null)
            audioPlayer = GetComponent<AudioPlayer>();
        if (triggerType == AudioTriggerType.OnAwake)
            ExecuteTrigger();
    }

    private void Start()
    {
        if (triggerType == AudioTriggerType.OnStart)
            ExecuteTrigger();
    }

    private void OnEnable()
    {
        if (triggerType == AudioTriggerType.OnEnable)
            ExecuteTrigger();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerType == AudioTriggerType.OnTriggerEnter && CheckTag(other.gameObject))
            ExecuteTrigger();
    }

    private void OnTriggerExit(Collider other)
    {
        if (triggerType == AudioTriggerType.OnTriggerExit && CheckTag(other.gameObject))
            ExecuteTrigger();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (triggerType == AudioTriggerType.OnCollisionEnter && CheckTag(collision.gameObject))
            ExecuteTrigger();
    }

    public void ExecuteTrigger()
    {
        if (triggerOnce && hasPlayed) return;
        if (audioPlayer == null || audioPreset == null) return;

        audioPlayer.Play(audioPreset); // Triggers the pooled AudioInstance
        hasPlayed = true;
    }

    private bool CheckTag(GameObject go)
    {
        if (string.IsNullOrEmpty(filterTag)) return true;
        return go.CompareTag(filterTag);
    }
}
