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

        newPreset.clips = new AudioClip[] { clip };
        newPreset.playMode = PlayMode.Sequential; 

        newPreset.volume = 1.0f;
        newPreset.pitch = 1.0f;
        newPreset.pitchRandomness = 0.05f;

        string clipPath = AssetDatabase.GetAssetPath(clip);
        string directory = Path.GetDirectoryName(clipPath);
        string clipName = Path.GetFileNameWithoutExtension(clipPath);

        string newPath = Path.Combine(directory, $"{clipName}.asset");
        newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);

        AssetDatabase.CreateAsset(newPreset, newPath);
        return newPreset;
    }
}