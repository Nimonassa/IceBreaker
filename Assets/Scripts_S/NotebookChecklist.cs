using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class NotebookChecklist : MonoBehaviour
{
    [SerializeField] private TMP_Text listText;
    [SerializeField] private GameObject[] items;

    private bool[] collected;
    private XRGrabInteractable[] itemGrabs;

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
        if (itemGrabs == null)
            return;

        for (int i = 0; i < itemGrabs.Length; i++)
            if (itemGrabs[i] != null)
                itemGrabs[i].selectEntered.AddListener(OnItemSelected);
    }

    private void OnDisable()
    {
        if (itemGrabs == null)
            return;

        for (int i = 0; i < itemGrabs.Length; i++)
            if (itemGrabs[i] != null)
                itemGrabs[i].selectEntered.RemoveListener(OnItemSelected);
    }

    private void OnItemSelected(SelectEnterEventArgs args)
    {
        if (args == null)
            return;

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
    }

    private void RefreshList()
    {
        if (listText == null)
            return;

        string text = "";

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;

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
