using UnityEngine;

public static class AudioPool
{
    // Strict hard limit. No memory allocations after the game starts.
    private const int POOL_SIZE = 32;
    private static AudioInstance[] pool;
    private static int currentIndex = 0;
    private static GameObject poolRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoWarmup()
    {
        if (poolRoot == null)
        {
            poolRoot = new GameObject("AudioPool_Global");
            Object.DontDestroyOnLoad(poolRoot);
        }

        pool = new AudioInstance[POOL_SIZE];

        for (int i = 0; i < POOL_SIZE; i++)
        {
            pool[i] = CreateNewInstance();
            pool[i].gameObject.SetActive(false);
        }
    }

    public static AudioInstance Get()
    {
        if (pool == null || poolRoot == null) AutoWarmup();

        for (int i = 0; i < POOL_SIZE; i++)
        {
            currentIndex = (currentIndex + 1) % POOL_SIZE;
            AudioInstance instance = pool[currentIndex];

            if (!instance.gameObject.activeInHierarchy)
            {
                instance.gameObject.SetActive(true);
                return instance;
            }
        }

        // 2. VOICE STEALING (Fallback):
        // If we reach here, it means ALL 32 VOICES are actively playing right now.
        // Instead of breaking the game by instantiating a 33rd object, we advance 
        // the cursor one more time to steal the "oldest" voice we checked, 
        currentIndex = (currentIndex + 1) % POOL_SIZE;
        AudioInstance stolenInstance = pool[currentIndex];

        return stolenInstance;
    }

    public static void Return(AudioInstance instance)
    {
        instance.gameObject.SetActive(false);
    }

    private static AudioInstance CreateNewInstance()
    {
        GameObject go = new GameObject("AudioInstance");
        go.transform.SetParent(poolRoot.transform);
        return go.AddComponent<AudioInstance>();
    }
}
