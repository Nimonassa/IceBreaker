public struct AudioHandle
{
    public readonly int ID;
    public static readonly AudioHandle Invalid = new AudioHandle(0);

    public AudioHandle(int id)
    {
        ID = id;
    }

    public bool IsValid => ID != 0;
}