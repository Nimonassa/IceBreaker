using UnityEditor;
using UnityEngine;
using System.IO;

public static class AudioPresetCreator
{
    [MenuItem("Assets/Create/Audio/Preset From Clip", false, 0)]
    public static void CreatePresetFromClip()
    {
        Object lastCreated = null;

        foreach (Object obj in Selection.objects)
        {
            if (obj is AudioClip clip)
            {
                lastCreated = CreateAsset(clip);
            }
        }

        if (lastCreated != null)
        {
            // Automatically select the last created preset so you can edit volume/pitch immediately
            Selection.activeObject = lastCreated;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/Create/Audio/Preset From Clip", true)]
    public static bool CreatePresetFromClipValidation()
    {
        foreach (Object obj in Selection.objects)
        {
            if (obj is AudioClip) return true;
        }
        return false;
    }

    private static Object CreateAsset(AudioClip clip)
    {
        AudioPreset newPreset = ScriptableObject.CreateInstance<AudioPreset>();
        newPreset.clip = clip;
        newPreset.volume = 1.0f;
        newPreset.pitch = 1.0f;
        newPreset.pitchRandomness = 0.05f;

        string clipPath = AssetDatabase.GetAssetPath(clip);
        string directory = Path.GetDirectoryName(clipPath);
        string clipName = Path.GetFileNameWithoutExtension(clipPath);

        // Removed "_Preset" so the filename matches the clip exactly
        string newPath = Path.Combine(directory, $"{clipName}.asset");
        newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);

        AssetDatabase.CreateAsset(newPreset, newPath);
        return newPreset;
    }
}