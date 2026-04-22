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

    // Helper method to clear all subscriptions when a scene unloads to prevent memory leaks
    public static void ClearAllSubscriptions()
    {
        OnRequestSpendMoney = null;
        OnMoneyChanged = null;
        OnBuildingPlaced = null;
        OnBuildingDeleted = null;
        OnTooltipStateChanged = null;
        Debug.Log("EventBus: All subscriptions cleared.");
    }
}
