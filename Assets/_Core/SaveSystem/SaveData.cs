using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public PlayerSaveData playerData = new PlayerSaveData();
    public WorldSaveData worldData = new WorldSaveData();

    public float playTime;
    public string lastLevel;
    public string saveDate;
    
    // Per spec: Track if this is a new game and current level
    public bool isNewGame;
    public string currentLevel;
    
    // Per spec: Checkpoint position for respawning
    public Vector3 checkpointPosition;
    
    // Lost soul persistence (Elden Ring style)
    public LostSoulSaveData lostSoul;
    
    // Currency and Inventory
    public int coins;
    public List<InventoryItem> inventory = new List<InventoryItem>();
}

