using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

// SplineModifier.cs
[ExecuteAlways] // Critical: Allows OnEnable/OnDisable to run in Edit Mode
public class SplineModifier : Modifier
{
    [Tooltip("Drag your Spline Container here")]
    public SplineContainer path;

    private void OnEnable()
    {
        // Start listening for ANY spline edits in the project
        Spline.Changed += OnSplineChanged;
    }

    private void OnDisable()
    {
        // CRITICAL FIX: Stop listening when deleted/disabled to prevent memory leaks
        Spline.Changed -= OnSplineChanged;
    }

    private void OnSplineChanged(Spline spline, int knotIndex, SplineModification modificationType)
    {
        if (path == null) return;

        // Verify that the spline we just edited belongs to our assigned Spline Container
        bool isOurSpline = false;
        var splines = path.Splines;
        for (int i = 0; i < splines.Count; i++)
        {
            if (splines[i] == spline)
            {
                isOurSpline = true;
                break;
            }
        }

        if (isOurSpline)
        {
#if UNITY_EDITOR
            Cloner cloner = GetComponent<Cloner>();
            if (cloner != null)
            {
                // Delaying the call ensures the spline has finished recalculating its length under the hood
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (cloner != null) cloner.GenerateClones();
                };
            }
#endif
        }
    }

    public override void ApplyModifier(Transform[] clones)
    {
        if (path == null || clones == null || clones.Length == 0) return;

        Cloner cloner = GetComponent<Cloner>();
        if (cloner == null) return;

        Vector3Int grid = cloner.gridCount;
        Vector3 spacing = cloner.spacing;

        for (int i = 0; i < clones.Length; i++)
        {
            if (clones[i] == null) continue;

            // 1. Reverse-engineer the clone's grid coordinate
            int zIndex = i % grid.z;
            int yIndex = (i / grid.z) % grid.y;
            int xIndex = i / (grid.y * grid.z);

            // 2. Calculate vertical (Y) and lateral (Z) distances using the Cloner's spacing
            float distY = (yIndex * spacing.y) - ((grid.y - 1) * spacing.y * 0.5f);
            float distZ = (zIndex * spacing.z) - ((grid.z - 1) * spacing.z * 0.5f);

            // 3. X is distributed evenly along the spline's entire length
            float t = grid.x > 1 ? (float)xIndex / (grid.x - 1) : 0f;

            // 4. Evaluate the spline at the 't' percentage
            path.Evaluate(0, t, out float3 localPos, out float3 localTangent, out float3 localUp);

            // 5. Use raw local Spline data
            Vector3 splinePos = localPos;
            Vector3 splineTangent = math.normalize(localTangent);
            Vector3 splineUp = math.normalize(localUp);

            // Calculate the lateral normal vector
            Vector3 splineNormal = Vector3.Cross(splineUp, splineTangent).normalized;

            // 6. Apply the wrapped grid position to the clone's LOCAL position
            clones[i].localPosition = splinePos + (splineUp * distY) + (splineNormal * distZ);
        }
    }
}
