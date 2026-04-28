using System;
using UnityEngine;

/// <summary>
/// A global message broker that allows decoupled communication between different systems.
/// </summary>
public static class EventBus
{
    // --- Economy Events ---
    // Invoked when an amount of money should be spent. Observers (like EconomyManager) will handle the deduction.
    // Parameters: int amount
    public static Action<int> OnRequestSpendMoney;
    
    // Invoked when money has successfully been spent or added, usually to notify UI to update.
    // Parameters: int newTotalMoney
    public static Action<int> OnMoneyChanged;

    // --- Building Events ---
    // Invoked when a building has been successfully placed. Observers (like AudioManager) can play sounds.
    // Parameters: Vector3 position, BuildingData data
    public static Action<Vector3, BuildingData> OnBuildingPlaced;
    
    // Invoked when a building has been deleted.
    // Parameters: Vector3 position
    public static Action<Vector3> OnBuildingDeleted;

    // --- UI Events ---
    // Invoked when a tooltip should be shown or hidden.
    // Parameters: bool bShow, string text, Vector2 position
    public static Action<bool, string, Vector2> OnTooltipStateChanged;

    // --- Audio Events ---
    // Invoked when a sound effect should be played at a world position.
    // Parameters: AudioClip clip, Vector3 worldPosition
    public static Action<AudioClip, Vector3> OnPlaySFX;

    // --- Save / Load Events ---
    // Invoked when the player requests a save or load operation.
    public static Action OnSaveRequested;
    public static Action OnLoadRequested;

    // Helper method to clear all subscriptions when a scene unloads to prevent memory leaks
    public static void ClearAllSubscriptions()
    {
        OnRequestSpendMoney = null;
        OnMoneyChanged = null;
        OnBuildingPlaced = null;
        OnBuildingDeleted = null;
        OnTooltipStateChanged = null;
        OnPlaySFX = null;
        OnSaveRequested = null;
        OnLoadRequested = null;
        Debug.Log("EventBus: All subscriptions cleared.");
    }
}
