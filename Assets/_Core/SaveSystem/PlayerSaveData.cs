using UnityEngine;

[System.Serializable]
public class PlayerSaveData
{
    public Vector3 position;
    public int health;
    public int coins; // Currency (kept here for backward compatibility, also saved in SaveData.coins)
}
