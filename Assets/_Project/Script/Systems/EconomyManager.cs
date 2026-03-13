using UnityEngine;
using TMPro;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Economy Settings")]
    public int currentMoney = 10000;

    [Header("UI References")]
    public TextMeshProUGUI moneyText;

    private void Awake()
    {
        // Simple Singleton pattern for global access
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple instances of EconomyManager found. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        UpdateMoneyUI();
    }

    public bool CanAfford(int amount)
    {
        return currentMoney >= amount;
    }

    public void SpendMoney(int amount)
    {
        if (CanAfford(amount))
        {
            currentMoney -= amount;
            UpdateMoneyUI();
        }
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateMoneyUI();
    }

    private void UpdateMoneyUI()
    {
        if (moneyText)
        {
            moneyText.text = $"Money: ${currentMoney}";
        }
    }
}
