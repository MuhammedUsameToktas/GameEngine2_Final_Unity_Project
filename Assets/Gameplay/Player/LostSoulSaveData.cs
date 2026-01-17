using UnityEngine;

/// <summary>
/// Lost Soul Save Data - Persists lost soul position and amounts (souls + coins) across saves
/// </summary>
[System.Serializable]
public struct LostSoulSaveData
{
    public bool exists;
    public Vector3 position;
    public int amount; // Souls (for backward compatibility)
    public int souls; // Souls amount
    public int coins; // Coins amount
}
