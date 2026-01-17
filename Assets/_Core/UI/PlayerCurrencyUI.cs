using UnityEngine;
using TMPro;

/// <summary>
/// Player Currency UI - Displays current coin count
/// UI only reads data, never modifies coins
/// </summary>
public class PlayerCurrencyUI : MonoBehaviour
{
    [Header("Coin Display")]
    [SerializeField] private TMP_Text coinText;

    private PlayerCurrency playerCurrency;

    private void Start()
    {
        FindPlayerCurrency();
        
        if (coinText == null)
        {
            Debug.LogWarning("PlayerCurrencyUI: Coin text not assigned. Assign a TextMeshPro text component in the Inspector.");
        }
    }

    private void Update()
    {
        // Try to find player if not found yet (in case player spawns dynamically)
        if (playerCurrency == null)
        {
            FindPlayerCurrency();
        }

        UpdateCoinUI();
    }

    /// <summary>
    /// Find player currency component
    /// </summary>
    private void FindPlayerCurrency()
    {
        // Try to get from LevelManager first (if player is spawned)
        if (LevelManager.Instance != null)
        {
            GameObject player = LevelManager.Instance.GetPlayerInstance();
            if (player != null)
            {
                playerCurrency = player.GetComponent<PlayerCurrency>();
            }
        }

        // Fallback to FindObjectOfType if LevelManager doesn't have player yet
        if (playerCurrency == null)
        {
            playerCurrency = FindObjectOfType<PlayerCurrency>();
        }
    }

    /// <summary>
    /// Update coin UI with current coin count
    /// </summary>
    private void UpdateCoinUI()
    {
        if (playerCurrency == null || coinText == null) return;

        coinText.text = playerCurrency.Coins.ToString();
    }
}
