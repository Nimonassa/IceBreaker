using UnityEngine;
using UnityEngine.UI; // Required for LayoutRebuilder
using TMPro;

public class SinglePromptUI : MonoBehaviour
{
    public TextMeshProUGUI promptText;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetText(string text)
    {
        if (promptText != null)
        {
            promptText.text = text;
            Canvas.ForceUpdateCanvases();
            if (rectTransform != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }
    }
}
