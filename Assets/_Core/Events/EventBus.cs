using System;
using UnityEngine;

/// <summary>
/// Global event bus for decoupled system communication.
/// All systems can subscribe/publish without hard references.
/// </summary>
public static class EventBus
{
    // ========== GAME FLOW ==========
    public static Action OnGameStarted;
    public static Action OnGamePaused;
    public static Action OnGameResumed;
    public static Action OnGameEnded;

    // ========== SCENE MANAGEMENT ==========
    public static Action<string> OnLevelLoaded;
    public static Action<string> OnLevelUnloaded;
    public static Action OnLevelComplete;

    // ========== PLAYER ==========
    public static Action OnPlayerDied;
    public static Action OnPlayerRespawned;
    public static Action<Vector3> OnPlayerMoved;
    public static Action<int> OnPlayerHealthChanged;
    public static Action<int> OnPlayerHealthMaxChanged;

    // ========== SAVE SYSTEM ==========
    public static Action OnSaveRequested;
    public static Action<int> OnSaveCompleted; // slot number
    public static Action OnLoadCompleted;
    public static Action<int> OnLoadStarted; // slot number

    // ========== INTERACTABLES ==========
    public static Action<IInteractable> OnInteractableEntered;
    public static Action<IInteractable> OnInteractableExited;
    public static Action<IInteractable, GameObject> OnInteractionPerformed;

    // ========== COMBAT ==========
    public static Action<GameObject, int> OnDamageDealt; // target, damage
    public static Action<GameObject, int> OnDamageReceived; // target, damage
    public static Action<GameObject> OnEnemyDefeated;
    public static Action<GameObject> OnEnemySpawned;

    // ========== UI ==========
    public static Action OnMenuOpened;
    public static Action OnMenuClosed;
    public static Action<string> OnDialogueStarted; // dialogue ID
    public static Action OnDialogueEnded;

    // ========== CUTSCENES ==========
    public static Action<string> OnCutsceneStarted; // cutscene ID
    public static Action OnCutsceneEnded;
}
