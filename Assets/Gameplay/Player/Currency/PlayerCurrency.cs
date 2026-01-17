using UnityEngine;

/// <summary>
/// Player Currency System - Manages coins (stackable currency)
/// Coins are separate from souls - used for shops, upgrades, etc.
/// </summary>
public class PlayerCurrency : MonoBehaviour
{
    public int Coins { get; private set; }

    /// <summary>
    /// Add coins to player's currency
    /// </summary>
    public void AddCoins(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"PlayerCurrency: Attempted to add negative coins ({amount}). Use SpendCoins instead.");
            return;
        }
        
        Coins += amount;
        Debug.Log($"PlayerCurrency: Added {amount} coins. Total: {Coins}");
    }

    /// <summary>
    /// Check if player can spend the specified amount
    /// </summary>
    public bool CanSpend(int amount)
    {
        return Coins >= amount;
    }

    /// <summary>
    /// Spend coins (validates amount first)
    /// </summary>
    public bool SpendCoins(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"PlayerCurrency: Attempted to spend negative coins ({amount})");
            return false;
        }

        if (!CanSpend(amount))
        {
            Debug.LogWarning($"PlayerCurrency: Insufficient coins. Required: {amount}, Have: {Coins}");
            return false;
        }

        Coins -= amount;
        Debug.Log($"PlayerCurrency: Spent {amount} coins. Remaining: {Coins}");
        return true;
    }

    /// <summary>
    /// Restore coins from save data
    /// </summary>
    public void RestoreCoins(int amount)
    {
        Coins = Mathf.Max(0, amount);
        Debug.Log($"PlayerCurrency: Restored {Coins} coins from save");
    }

    /// <summary>
    /// Clear all coins (called on death)
    /// </summary>
    public void ClearCoins()
    {
        Coins = 0;
    }

    /// <summary>
    /// Get current coins (for dropping on death)
    /// </summary>
    public int GetCoins()
    {
        return Coins;
    }
}
