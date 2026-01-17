using UnityEngine;

/// <summary>
/// Soul currency system - souls are collected and can be converted to health
/// Souls are NOT health, they are a resource for healing
/// </summary>
public class PlayerSoul : MonoBehaviour
{
    [Header("Soul Settings")]
    [SerializeField] private int maxSouls = 99;

    public int CurrentSouls { get; private set; }
    public int MaxSouls => maxSouls;

    /// <summary>
    /// Add souls to the player's collection
    /// </summary>
    public void AddSouls(int amount)
    {
        CurrentSouls = Mathf.Min(CurrentSouls + amount, maxSouls);
    }

    /// <summary>
    /// Check if player has enough souls to spend
    /// </summary>
    public bool CanSpend(int amount)
    {
        return CurrentSouls >= amount;
    }

    /// <summary>
    /// Spend souls (used for healing)
    /// </summary>
    public void Spend(int amount)
    {
        CurrentSouls = Mathf.Max(0, CurrentSouls - amount);
    }

    /// <summary>
    /// Clear all souls (called on death)
    /// </summary>
    public void ClearSouls()
    {
        CurrentSouls = 0;
    }

    /// <summary>
    /// Get current souls (for dropping on death)
    /// </summary>
    public int GetSouls()
    {
        return CurrentSouls;
    }
}
