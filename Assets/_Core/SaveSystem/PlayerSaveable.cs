using UnityEngine;

/// <summary>
/// PlayerSaveable - Makes player position saveable
/// Per spec: First ISaveable implementation, proves save system works
/// Player never saves itself - SaveManager handles saving
/// </summary>
public class PlayerSaveable : MonoBehaviour, ISaveable
{
    private string uniqueID;
    
    private void Awake()
    {
        // Generate unique ID for this player instance
        uniqueID = $"Player_{gameObject.GetInstanceID()}";
        
        // Register with SaveManager
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterSaveable(this);
        }
    }
    
    private void OnDestroy()
    {
        // Unregister when destroyed
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UnregisterSaveable(this);
        }
    }
    
    /// <summary>
    /// Get unique identifier for this saveable object
    /// </summary>
    public string GetUniqueID()
    {
        return uniqueID;
    }
    
    /// <summary>
    /// Capture current state (position)
    /// Per spec: Player position survives saves
    /// </summary>
    public object CaptureState()
    {
        // Per spec: Save player position
        return transform.position;
    }
    
    /// <summary>
    /// Restore state (position)
    /// Per spec: Restore player position when loading
    /// </summary>
    public void RestoreState(object state)
    {
        if (state is Vector3 position)
        {
            transform.position = position;
            Debug.Log($"Player position restored to: {position}");
        }
        else
        {
            Debug.LogError($"Failed to restore player state: Invalid state type");
        }
    }
}

