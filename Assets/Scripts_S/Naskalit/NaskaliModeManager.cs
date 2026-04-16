using UnityEngine;

public class NaskaliModeManager : MonoBehaviour
{
    public NaskalitAttach combinedScript;
    public SingleNaskali[] singleNaskalit;

    public GameObject combinedObject;
    public GameObject singleObjectsParent;

    public void SetCombinedMode()
    {
        combinedScript.enabled = true;
        combinedObject.SetActive(true);

        foreach (var n in singleNaskalit)
            n.enabled = false;

        singleObjectsParent.SetActive(false);
    }

    public void SetSeparateMode()
    {
        combinedScript.enabled = false;
        combinedObject.SetActive(false);

        foreach (var n in singleNaskalit)
            n.enabled = true;

        singleObjectsParent.SetActive(true);
    }
}