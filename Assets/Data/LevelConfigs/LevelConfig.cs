using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelConfig", menuName = "Game/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("Level Information")]
    public string levelName;
    public string displayName;
    [TextArea(3, 5)]
    public string description;

    [Header("Player Spawn")]
    public Transform playerSpawnPoint;

    [Header("Level Settings")]
    public bool allowRespawn = true;
    public float timeLimit = 0f; // 0 = no time limit

    [Header("Enemy Spawns")]
    public EnemySpawnData[] enemySpawns;

    [Header("Interactables")]
    public InteractableSpawnData[] interactableSpawns;
}

[System.Serializable]
public class EnemySpawnData
{
    public GameObject enemyPrefab;
    public Vector3 spawnPosition;
    public bool spawnOnStart = true;
}

[System.Serializable]
public class InteractableSpawnData
{
    public GameObject interactablePrefab;
    public Vector3 spawnPosition;
    public Quaternion spawnRotation = Quaternion.identity;
}

