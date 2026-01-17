using UnityEngine;
using System.Collections;

/// <summary>
/// Hold-to-heal system (Hollow Knight style)
/// Drains souls gradually to heal HP one point at a time
/// </summary>
public class PlayerHeal : MonoBehaviour
{
    [Header("Heal Settings")]
    [SerializeField] private int soulCostPerHeal = 10;
    [SerializeField] private float healInterval = 0.5f;

    private PlayerHealth health;
    private PlayerSoul soul;
    private Coroutine healRoutine;
    private bool isHealing = false;

    public bool IsHealing => isHealing;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        soul = GetComponent<PlayerSoul>();
    }

    /// <summary>
    /// Start healing process (called when heal button is held)
    /// </summary>
    public void StartHealing()
    {
        if (healRoutine != null || !CanHeal()) return;
        
        isHealing = true;
        healRoutine = StartCoroutine(HealRoutine());
    }

    /// <summary>
    /// Stop healing process (called when heal button is released)
    /// </summary>
    public void StopHealing()
    {
        if (healRoutine != null)
        {
            StopCoroutine(healRoutine);
            healRoutine = null;
        }
        isHealing = false;
    }

    /// <summary>
    /// Check if player can heal (has health to restore and souls to spend)
    /// </summary>
    private bool CanHeal()
    {
        return health != null && soul != null && 
               health.CanHeal() && 
               soul.CanSpend(soulCostPerHeal);
    }

    /// <summary>
    /// Healing coroutine - drains souls and heals HP gradually
    /// </summary>
    private IEnumerator HealRoutine()
    {
        while (CanHeal())
        {
            // Check if player took damage (interrupts healing)
            if (health.IsInvincible)
            {
                StopHealing();
                yield break;
            }

            soul.Spend(soulCostPerHeal);
            health.Heal(1);

            // TODO: Add heal animation + VFX + sound

            yield return new WaitForSeconds(healInterval);
        }

        // Auto-stop when conditions are no longer met
        StopHealing();
    }
}
