using UnityEngine;

// RandomModifier.cs
public class RandomModifier : Modifier
{
    public int seed = 42;
    public Vector3 positionRandomness = Vector3.zero;
    public Vector3 rotationRandomness = Vector3.zero;
    public Vector3 scaleRandomness = Vector3.zero;

    public override void ApplyModifier(Transform[] clones)
    {
        // Initialize the random state so the randomness is deterministic and doesn't flicker
        Random.InitState(seed);

        foreach (var clone in clones)
        {
            if (clone == null) continue;

            clone.localPosition += new Vector3(
                Random.Range(-positionRandomness.x, positionRandomness.x),
                Random.Range(-positionRandomness.y, positionRandomness.y),
                Random.Range(-positionRandomness.z, positionRandomness.z)
            );

            clone.localEulerAngles += new Vector3(
                Random.Range(-rotationRandomness.x, rotationRandomness.x),
                Random.Range(-rotationRandomness.y, rotationRandomness.y),
                Random.Range(-rotationRandomness.z, rotationRandomness.z)
            );

            clone.localScale += new Vector3(
                Random.Range(-scaleRandomness.x, scaleRandomness.x),
                Random.Range(-scaleRandomness.y, scaleRandomness.y),
                Random.Range(-scaleRandomness.z, scaleRandomness.z)
            );
        }
    }
}
