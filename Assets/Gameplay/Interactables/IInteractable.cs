using UnityEngine;

/// <summary>
/// Universal interface for all interactable objects (doors, levers, items, NPCs, etc.)
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Called when the player interacts with this object
    /// </summary>
    void Interact(GameObject interactor);

    /// <summary>
    /// Get the interaction prompt text (e.g., "Press E to Open Door")
    /// </summary>
    string GetInteractionPrompt();

    /// <summary>
    /// Check if this object can be interacted with
    /// </summary>
    bool CanInteract(GameObject interactor);

    /// <summary>
    /// Get the position where the interaction prompt should be displayed
    /// </summary>
    Vector3 GetInteractionPosition();
}

