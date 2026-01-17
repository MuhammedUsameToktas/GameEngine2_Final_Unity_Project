using UnityEngine;

public interface ISaveable
{
    string GetUniqueID();
    object CaptureState();
    void RestoreState(object state);
}

