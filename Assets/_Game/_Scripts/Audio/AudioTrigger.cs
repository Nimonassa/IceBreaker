using UnityEngine;

public class AudioTrigger : MonoBehaviour
{
    public enum TriggerType { Manual, OnAwake, OnStart, OnEnable, OnTriggerEnter, OnTriggerExit, OnCollisionEnter }
    public enum ExitAction  { DoNothing, StopSound, PauseSound }

    [Header("Dependencies")]
    [Tooltip("If left blank, it will look for an AudioPlayer on this object.")]
    public AudioPlayer audioPlayer;
    public AudioPreset audioPreset;

    [Header("Trigger Conditions")]
    public TriggerType playOn = TriggerType.Manual;
    public ExitAction onExit = ExitAction.DoNothing;
    public bool playOnce = false;

    

    [Header("Filters")]
    public string filterTag = "Player";
    public LayerMask filterLayer = ~0;

    private AudioHandle currentHandle;
    private bool hasPlayed = false;
    private bool isOccupied = false;
    private bool isPausedByTrigger = false; 


    private bool fadeAudio = true;
    private float fadeDuration = 0.5f;

    

    public void ExecuteTrigger()
    {
        if (audioPlayer == null || audioPreset == null) return;

        if (currentHandle.IsValid && audioPlayer.IsSoundActive(currentHandle))
        {
            if (isPausedByTrigger)
            {
                UnpauseTrigger();
                if (fadeAudio) audioPlayer.FadeTo(currentHandle, 1f, fadeDuration);
            }
            return;
        }

        if (playOnce && hasPlayed) 
            return;

        currentHandle = audioPlayer.Play(audioPreset);
        hasPlayed = true;
    }
    
    public void StopTrigger()
    {
        if (audioPlayer != null && currentHandle.IsValid)
        {
            audioPlayer.StopSound(currentHandle);
            isPausedByTrigger = false; 
        }
    }

    public void PauseTrigger()
    {
        if (audioPlayer != null && currentHandle.IsValid)
        {
            audioPlayer.PauseSound(currentHandle);
            isPausedByTrigger = true; 
        }
    }

    public void UnpauseTrigger()
    {
        if (audioPlayer != null && currentHandle.IsValid)
        {
            audioPlayer.UnpauseSound(currentHandle);
            isPausedByTrigger = false; 
        }
    }



    private void Awake()
    {
        if (audioPlayer == null)
            audioPlayer = GetComponent<AudioPlayer>();
            
        if (!string.IsNullOrEmpty(filterTag) && filterTag != "Untagged")
        {
            try
            {
                GameObject.FindWithTag(filterTag);
            }
            catch (UnityException)
            {
                filterTag = "Untagged";
            }
        }

        if (playOn == TriggerType.OnAwake) ExecuteTrigger();
    }

    private void OnEnable()
    {
        if (playOn == TriggerType.OnEnable) ExecuteTrigger();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (CheckTag(other.gameObject) && CheckLayer(other.gameObject))
        {
            if (!isOccupied)
            {
                isOccupied = true;
                if (playOn == TriggerType.OnTriggerEnter) ExecuteTrigger();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (CheckTag(other.gameObject) && CheckLayer(other.gameObject))
        {
            isOccupied = false;
            
            if (playOn == TriggerType.OnTriggerExit) 
            {
                ExecuteTrigger();
            }
            else if (playOn == TriggerType.OnTriggerEnter) 
            {
                // Only handle exit actions if this is an "Enter" based trigger zone
                HandleExitAction();
            }
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

            if (playOn == TriggerType.OnCollisionEnter)
            {
                HandleExitAction();
            }
        }
    }

    private void HandleExitAction()
    {
        if (!currentHandle.IsValid) return;

        if (fadeAudio)
        {
            if (onExit == ExitAction.StopSound)
            {
                isPausedByTrigger = false; 
                audioPlayer.FadeTo(currentHandle, 0f, fadeDuration, StopTrigger);
            }
            else if (onExit == ExitAction.PauseSound)
            {
                isPausedByTrigger = true; 
                audioPlayer.FadeTo(currentHandle, 0f, fadeDuration, PauseTrigger);
            }
        }
        else
        {
            if (onExit == ExitAction.StopSound) StopTrigger();
            else if (onExit == ExitAction.PauseSound) PauseTrigger();
        }
    }

    private bool CheckTag(GameObject go) { return string.IsNullOrEmpty(filterTag) || go.CompareTag(filterTag); }
    private bool CheckLayer(GameObject go) { return (filterLayer.value & (1 << go.layer)) != 0; }
}