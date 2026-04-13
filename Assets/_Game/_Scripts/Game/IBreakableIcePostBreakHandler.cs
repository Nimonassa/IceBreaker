using System.Collections;

public interface IBreakableIcePostBreakHandler
{
    bool CanHandlePostBreak(BreakableIceZone zone, PlayerManager player);
    IEnumerator HandlePostBreak(BreakableIceZone zone, PlayerManager player);
}
