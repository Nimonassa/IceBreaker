using UnityEngine;
using UnityEngine.InputSystem; // <-- Add this for Keyboard support

public class GamePauseManager : MonoBehaviour
{
    private bool isPaused = false;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if(keyboard.escapeKey.wasPressedThisFrame)
                TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;
        AudioManager.Pause();
    }

    private void ResumeGame()
    {
        Time.timeScale = 1f;
        AudioManager.Unpause();
    }
}