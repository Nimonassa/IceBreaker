using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;

    [Header("Volume")]
    public Button volumeButton;
    public Sprite volumeOnSprite;
    public Sprite volumeOffSprite;
    private bool isVolumeOn = true;

    void Start()
    {
        // Start with main menu visible
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);

        UpdateVolumeIcon();
    }

    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void BackToMainMenu()
    {
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void ToggleVolume()
    {
        isVolumeOn = !isVolumeOn;

        // Set global volume
        AudioListener.volume = isVolumeOn ? 1f : 0f;

        UpdateVolumeIcon();
    }

    private void UpdateVolumeIcon()
    {
        if (isVolumeOn)
            volumeButton.image.sprite = volumeOnSprite;
        else
            volumeButton.image.sprite = volumeOffSprite;
    }
}
