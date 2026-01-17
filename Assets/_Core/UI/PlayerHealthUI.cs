using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player Health UI - Displays health masks (Hollow Knight style)
/// Shows ON/OFF masks for each health point
/// UI only reads data, never modifies health
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [Header("Health Masks")]
    [SerializeField] private Image[] masks;

    private PlayerHealth playerHealth;

    private void Start()
    {
        FindPlayerHealth();
        
        // Validate mask array
        if (masks == null || masks.Length == 0)
        {
            Debug.LogWarning("PlayerHealthUI: No masks assigned. Assign mask images in the Inspector.");
        }
    }

    private void Update()
    {
        // Try to find player if not found yet (in case player spawns dynamically)
        if (playerHealth == null)
        {
            FindPlayerHealth();
        }

        UpdateHealthUI();
    }

    /// <summary>
    /// Find player health component
    /// </summary>
    private void FindPlayerHealth()
    {
        // Try to get from LevelManager first (if player is spawned)
        if (LevelManager.Instance != null)
        {
            GameObject player = LevelManager.Instance.GetPlayerInstance();
            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
            }
        }

        // Fallback to FindObjectOfType if LevelManager doesn't have player yet
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }
    }

    /// <summary>
    /// Update health UI based on current health
    /// Shows masks for each health point (Hollow Knight style)
    /// </summary>
    private void UpdateHealthUI()
    {
        if (playerHealth == null || masks == null) return;

        int currentHealth = playerHealth.CurrentHealth;
        int maxHealth = playerHealth.MaxHealth;

        // Enable masks up to current health, disable the rest
        for (int i = 0; i < masks.Length; i++)
        {
            if (masks[i] != null)
            {
                // Show mask if index is less than current health
                masks[i].enabled = i < currentHealth;
            }
        }
    }
}
