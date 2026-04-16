using UnityEngine;

// Modifier.cs
public abstract class Modifier : MonoBehaviour
{
    public abstract void ApplyModifier(Transform[] clones);

    // Forces the cloner to update when you tweak modifier values in the Inspector
    protected virtual void OnValidate()
    {
#if UNITY_EDITOR
        Cloner cloner = GetComponent<Cloner>();
        if (cloner != null)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (cloner != null) cloner.GenerateClones();
            };
        }
#endif
    }
}