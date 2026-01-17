using UnityEngine;
using TMPro;

/// <summary>
/// Player Soul UI - Displays current soul count
/// UI only reads data, never modifies souls
/// </summary>
public class PlayerSoulUI : MonoBehaviour
{
    [Header("Soul Display")]
    [SerializeField] private TMP_Text soulText;

    private PlayerSoul playerSoul;

    private void Start()
    {
        FindPlayerSoul();
        
        if (soulText == null)
        {
            Debug.LogWarning("PlayerSoulUI: Soul text not assigned. Assign a TextMeshPro text component in the Inspector.");
        }
    }

    private void Update()
    {
        // Try to find player if not found yet (in case player spawns dynamically)
        if (playerSoul == null)
        {
            FindPlayerSoul();
        }

        UpdateSoulUI();
    }

    /// <summary>
    /// Find player soul component
    /// </summary>
    private void FindPlayerSoul()
    {
        // Try to get from LevelManager first (if player is spawned)
        if (LevelManager.Instance != null)
        {
            GameObject player = LevelManager.Instance.GetPlayerInstance();
            if (player != null)
            {
                playerSoul = player.GetComponent<PlayerSoul>();
            }
        }

        // Fallback to FindObjectOfType if LevelManager doesn't have player yet
        if (playerSoul == null)
        {
            playerSoul = FindObjectOfType<PlayerSoul>();
        }
    }

    /// <summary>
    /// Update soul UI with current soul count
    /// </summary>
    private void UpdateSoulUI()
    {
        if (playerSoul == null || soulText == null) return;

        soulText.text = playerSoul.CurrentSouls.ToString();
    }
}
