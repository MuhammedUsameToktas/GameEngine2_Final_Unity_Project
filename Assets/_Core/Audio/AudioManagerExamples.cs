using UnityEngine;

/// <summary>
/// AudioManagerExamples - Example usage of AudioManager
/// Copy these examples into your own scripts
/// </summary>
public class AudioManagerExamples : MonoBehaviour
{
    // ============================================
    // MUSIC EXAMPLES
    // ============================================

    void Example_PlayMusic()
    {
        // Play music by name (from Music Library)
        AudioManager.Instance.PlayMusic("MainMenuTheme");
        
        // Play with fade in (default)
        AudioManager.Instance.PlayMusic("BattleMusic", fadeIn: true);
        
        // Play without fade (immediate)
        AudioManager.Instance.PlayMusic("VictoryFanfare", fadeIn: false);
    }

    void Example_StopMusic()
    {
        // Stop music with fade out
        AudioManager.Instance.StopMusic(fadeOut: true);
        
        // Stop music immediately
        AudioManager.Instance.StopMusic(fadeOut: false);
    }

    void Example_PauseResumeMusic()
    {
        // Pause music
        AudioManager.Instance.PauseMusic();
        
        // Resume music
        AudioManager.Instance.ResumeMusic();
    }

    void Example_CheckMusicPlaying()
    {
        if (AudioManager.Instance.IsMusicPlaying())
        {
            Debug.Log($"Currently playing: {AudioManager.Instance.GetCurrentMusicName()}");
        }
    }

    // ============================================
    // SFX EXAMPLES
    // ============================================

    void Example_PlaySFX()
    {
        // Play SFX by name (from SFX Library)
        AudioManager.Instance.PlaySFX("Jump");
        
        // Play with custom volume and pitch
        AudioManager.Instance.PlaySFX("Coin", volume: 0.8f, pitch: 1.2f);
        
        // Play with random pitch variation (avoids repetition)
        float randomPitch = Random.Range(0.9f, 1.1f);
        AudioManager.Instance.PlaySFX("Footstep", pitch: randomPitch);
    }

    void Example_PlaySFX3D()
    {
        // Play SFX at 3D position (spatial audio)
        Vector3 explosionPos = transform.position;
        AudioManager.Instance.PlaySFXAtPosition("Explosion", explosionPos);
        
        // Play with custom settings
        AudioManager.Instance.PlaySFXAtPosition(
            "Footstep",
            transform.position,
            volume: 0.6f,
            pitch: 1f,
            minDistance: 5f,  // Start fading at 5 units
            maxDistance: 20f  // Silent at 20 units
        );
    }

    void Example_StopAllSFX()
    {
        // Stop all playing SFX
        AudioManager.Instance.StopAllSFX();
    }

    // ============================================
    // REAL-WORLD EXAMPLES
    // ============================================

    // ============================================
    // EXAMPLE: Player Controller
    // Copy these methods into your PlayerController script
    // ============================================
    
    // In Update() or input handler:
    // if (Input.GetButtonDown("Jump"))
    // {
    //     AudioManager.Instance.PlaySFX("Jump");
    //     // ... jump logic
    // }

    // On damage:
    // void OnTakeDamage()
    // {
    //     AudioManager.Instance.PlaySFX("Hurt");
    // }

    // On collect item:
    // void OnCollectItem()
    // {
    //     float pitch = Random.Range(0.95f, 1.05f);
    //     AudioManager.Instance.PlaySFX("Coin", pitch: pitch);
    // }

    // ============================================
    // EXAMPLE: Level Manager
    // Copy these methods into your LevelManager script
    // ============================================
    
    // void Start()
    // {
    //     // Play level music when level starts
    //     AudioManager.Instance.PlayMusic("Level1Music");
    // }

    // void OnBossFightStart()
    // {
    //     // Switch to boss music
    //     AudioManager.Instance.PlayMusic("BossMusic");
    // }

    // void OnLevelComplete()
    // {
    //     // Play victory music
    //     AudioManager.Instance.PlayMusic("VictoryFanfare");
    // }

    // ============================================
    // EXAMPLE: Enemy Script
    // Copy these methods into your Enemy script
    // ============================================
    
    // void OnDeath()
    // {
    //     // Play death sound at enemy position (3D audio)
    //     AudioManager.Instance.PlaySFXAtPosition("EnemyDeath", transform.position);
    // }

    // void OnAttack()
    // {
    //     // Play attack sound
    //     AudioManager.Instance.PlaySFX("EnemyAttack");
    // }

    // ============================================
    // EXAMPLE: UI Button
    // Copy this method into your UI Button script
    // ============================================
    
    // public void OnButtonClick()
    // {
    //     AudioManager.Instance.PlaySFX("ButtonClick");
    //     // ... button logic
    // }

    // ============================================
    // EXAMPLE: Weapon Script
    // Copy these methods into your Weapon script
    // ============================================
    
    // void OnShoot()
    // {
    //     // Play shoot sound
    //     AudioManager.Instance.PlaySFX("Shoot");
    // }

    // void OnHit(Vector3 hitPoint)
    // {
    //     // Play hit sound at impact point (3D audio)
    //     AudioManager.Instance.PlaySFXAtPosition("Hit", hitPoint, volume: 0.7f);
    // }
}
