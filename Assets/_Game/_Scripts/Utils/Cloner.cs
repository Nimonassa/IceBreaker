using System.Collections.Generic;
using UnityEngine;

public enum CloneMode
{
    Sequential,
    Random
}

// Cloner.cs
[ExecuteAlways]
public class Cloner : MonoBehaviour
{
    public GameObject[] prefabs;
    public CloneMode cloneMode = CloneMode.Sequential;
    public int randomSeed = 0;

    public Vector3Int gridCount = new Vector3Int(3, 3, 3);
    public Vector3 spacing = new Vector3(1.5f, 1.5f, 1.5f);

    // FIX 1: Removed [SerializeField] to prevent memory leaks with DontSave objects during domain reload
    private List<GameObject> _clones = new List<GameObject>();

    private void OnValidate()
    {
#if UNITY_EDITOR
        // FIX 2: Prevent generation while Unity is actively freezing the editor to switch to Play Mode
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && !Application.isPlaying)
            return;

        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            GenerateClones();
        };
#endif
    }

    private void Start()
    {
        // FIX 3: DontSave objects are not copied into the Play Mode scene. 
        // We must tell the Cloner to explicitly spawn them when Play Mode actually starts.
        if (Application.isPlaying)
        {
            GenerateClones();
        }
    }

    public void GenerateClones()
    {
        ClearClones();

        if (prefabs == null || prefabs.Length == 0 || gridCount.x <= 0 || gridCount.y <= 0 || gridCount.z <= 0)
            return;

        Transform[] cloneTransforms = new Transform[gridCount.x * gridCount.y * gridCount.z];
        int index = 0;

        Vector3 centerOffset = new Vector3(
            (gridCount.x - 1) * spacing.x * 0.5f,
            (gridCount.y - 1) * spacing.y * 0.5f,
            (gridCount.z - 1) * spacing.z * 0.5f
        );

        if (cloneMode == CloneMode.Random)
        {
            Random.InitState(randomSeed);
        }

        for (int x = 0; x < gridCount.x; x++)
        {
            for (int y = 0; y < gridCount.y; y++)
            {
                for (int z = 0; z < gridCount.z; z++)
                {
                    GameObject prefabToInstantiate = null;
                    if (cloneMode == CloneMode.Sequential)
                    {
                        prefabToInstantiate = prefabs[index % prefabs.Length];
                    }
                    else if (cloneMode == CloneMode.Random)
                    {
                        prefabToInstantiate = prefabs[Random.Range(0, prefabs.Length)];
                    }

                    if (prefabToInstantiate == null)
                    {
                        index++;
                        continue;
                    }

                    GameObject clone = Instantiate(prefabToInstantiate, transform);

                    clone.hideFlags = HideFlags.DontSave;
                    clone.transform.localPosition = new Vector3(x * spacing.x, y * spacing.y, z * spacing.z) - centerOffset;
                    clone.name = $"{prefabToInstantiate.name}_Clone_{index}";

                    _clones.Add(clone);
                    cloneTransforms[index] = clone.transform;
                    index++;
                }
            }
        }

        ApplyModifiers(cloneTransforms);
    }

    private void ApplyModifiers(Transform[] cloneTransforms)
    {
        Modifier[] modifiers = GetComponents<Modifier>();
        foreach (var modifier in modifiers)
        {
            if (modifier.isActiveAndEnabled)
            {
                modifier.ApplyModifier(cloneTransforms);
            }
        }
    }

    private void ClearClones()
    {
        _clones.Clear();

        // FIX 4: Safely iterate backwards based on the current childCount.
        // This permanently stops the `while` loop from freezing Unity when Destroy() is called in Play Mode.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
}
