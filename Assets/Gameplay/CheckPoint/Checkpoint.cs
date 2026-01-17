using UnityEngine;

/// <summary>
/// Checkpoint - Detects player entry and triggers save
/// Per spec: Checkpoint updates respawn position and saves game automatically
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;

    private bool activated;

    private void OnTriggerEnter(Collider other)
    {
        if (activated) return;
        if (!other.CompareTag("Player")) return;

        activated = true;

        // Use spawnPoint position if assigned, otherwise use checkpoint's own position
        Vector3 checkpointPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        
        // Set checkpoint in LevelManager
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.SetCheckpoint(checkpointPosition);
        }
        else
        {
            Debug.LogError("Checkpoint: LevelManager.Instance is null! Cannot set checkpoint.");
            return;
        }
        
        // Save the game (this will save the checkpoint position)
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
            Debug.Log($"Checkpoint activated and saved at: {checkpointPosition}");
        }
        else
        {
            Debug.LogError("Checkpoint: SaveManager.Instance is null! Cannot save game.");
        }
    }
}
