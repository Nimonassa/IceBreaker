using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerTeleportBlink : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Image blinkImage;

    [Header("Blink Phases")]
    [SerializeField] private float durationClosing = 0.1f;
    [SerializeField] private float durationHold = 0.05f;
    [SerializeField] private float durationOpening = 0.25f;
    [SerializeField] private float speedScale = 1.0f;
    [SerializeField] private bool isEnabled = true;

    private Coroutine currentFade;

    private void Reset()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    private void Awake()
    {
        SetAlpha(0f);
    }

    private void Start()
    {
        SyncTeleportDelay();
    }

    private void OnEnable()
    {
        PlayerEvents.OnTeleportStarted.AddListener(FadeOut);
        PlayerEvents.OnTeleportEnded.AddListener(FadeIn);
        SyncTeleportDelay();
    }

    private void OnDisable()
    {
        PlayerEvents.OnTeleportStarted.RemoveListener(FadeOut);
        PlayerEvents.OnTeleportEnded.RemoveListener(FadeIn);
    }

    public void SetEnabled(bool enable)
    {
        isEnabled = enable;
        SyncTeleportDelay();
    }

    public void SetSpeedScale(float scale)
    {
        speedScale = Mathf.Max(0.1f, scale);
        SyncTeleportDelay();
    }

    public void SetBlinkTimes(float closing, float hold, float opening)
    {
        durationClosing = closing;
        durationHold = hold;
        durationOpening = opening;
        SyncTeleportDelay();
    }

    private void SyncTeleportDelay()
    {
        if (playerMovement != null)
        {
            float totalWait = (durationClosing + durationHold) / speedScale;
            playerMovement.SetTeleportDelay(isEnabled ? totalWait : 0f);
        }
    }

    private void FadeOut()
    {
        if (isEnabled)
        {
            RunFade(1f, durationClosing / speedScale);
        }
    }

    private void FadeIn()
    {
        if (blinkImage != null)
        {
            RunFade(0f, durationOpening / speedScale);
        }
    }

    private void RunFade(float targetAlpha, float duration)
    {
        if (currentFade != null)
        {
            StopCoroutine(currentFade);
        }

        currentFade = StartCoroutine(FadeRoutine(targetAlpha, duration));
    }

    private void SetAlpha(float alpha)
    {
        if (blinkImage != null)
        {
            Color color = blinkImage.color;
            color.a = alpha;
            blinkImage.color = color;
        }
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        if (blinkImage == null || duration <= 0f)
        {
            yield break;
        }

        float startAlpha = blinkImage.color.a;

        for (float t = 0; t < 1f; t += Time.deltaTime / duration)
        {
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetAlpha(targetAlpha);
    }
}