using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class NotebookChecklist : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text listText;
    [SerializeField] private CanvasGroup notebookCanvasGroup; // 1. Added CanvasGroup reference

    [Header("Settings")]
    [SerializeField] private GameObject[] items;

    [Header("Events")]
    [SerializeField] private UnityEvent OnCompleted;

    private bool[] collected;
    private XRGrabInteractable[] itemGrabs;
    private bool isAlreadyCompleted = false;

    private void Awake()
    {
        collected = new bool[items.Length];
        itemGrabs = new XRGrabInteractable[items.Length];

        for (int i = 0; i < items.Length; i++)
            if (items[i] != null)
                itemGrabs[i] = items[i].GetComponentInChildren<XRGrabInteractable>(true);
    }

    private void Start()
    {
        RefreshList();
    }

    private void OnEnable()
    {
        if (itemGrabs == null) return;

        for (int i = 0; i < itemGrabs.Length; i++)
            if (itemGrabs[i] != null)
                itemGrabs[i].selectEntered.AddListener(OnItemSelected);
    }

    private void OnDisable()
    {
        if (itemGrabs == null) return;

        for (int i = 0; i < itemGrabs.Length; i++)
            if (itemGrabs[i] != null)
                itemGrabs[i].selectEntered.RemoveListener(OnItemSelected);
    }

    // --- NEW CANVAS GROUP METHODS ---

    public void ToggleNotebook()
    {
        if (notebookCanvasGroup == null) return;

        // If alpha is greater than 0, consider it visible
        bool isVisible = notebookCanvasGroup.alpha > 0f;

        if (isVisible)
            HideNotebook();
        else
            ShowNotebook();
    }

    public void ShowNotebook()
    {
        if (notebookCanvasGroup == null) return;

        notebookCanvasGroup.alpha = 1f;
        notebookCanvasGroup.interactable = true;
        notebookCanvasGroup.blocksRaycasts = true;
    }

    public void HideNotebook()
    {
        if (notebookCanvasGroup == null) return;

        notebookCanvasGroup.alpha = 0f;
        notebookCanvasGroup.interactable = false;
        notebookCanvasGroup.blocksRaycasts = false;
    }

    // --------------------------------

    private void OnItemSelected(SelectEnterEventArgs args)
    {
        if (args == null) return;

        for (int i = 0; i < items.Length; i++)
            if (args.interactableObject == itemGrabs[i])
            {
                Collect(i);
                return;
            }
    }

    private void Collect(int index)
    {
        if (index < 0 || index >= items.Length || collected[index] || items[index] == null)
            return;

        collected[index] = true;
        items[index].SetActive(false);
        RefreshList();

        CheckForCompletion();
    }

    private void CheckForCompletion()
    {
        if (isAlreadyCompleted) return;

        for (int i = 0; i < collected.Length; i++)
        {
            if (!collected[i]) return;
        }

        isAlreadyCompleted = true;
        Debug.Log("Inventory Checklist Completed!");
        OnCompleted?.Invoke();
    }

    private void RefreshList()
    {
        if (listText == null) return;

        string text = "";
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null) continue;

            string itemName = CleanName(items[i].name);

            if (collected[i])
                itemName = "<s>" + itemName + "</s>";

            if (text.Length > 0)
                text += "\n";

            text += itemName;
        }

        listText.text = text;
    }

    private string CleanName(string itemName)
    {
        return itemName.Replace("(Clone)", "").Trim();
    }
}