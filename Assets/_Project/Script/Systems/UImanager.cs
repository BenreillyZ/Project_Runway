using UnityEngine;
using TMPro;

/// <summary>
/// A global UIManager that listens to the EventBus to display tooltips and other UI elements,
/// completely decoupling UI logic from systemic logic.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Tooltip References")]
    public GameObject tooltipUI;
    public TextMeshProUGUI tooltipText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        EventBus.OnTooltipStateChanged += HandleTooltipState;
    }

    private void OnDisable()
    {
        EventBus.OnTooltipStateChanged -= HandleTooltipState;
    }

    private void Start()
    {
        // Ensure tooltip is hidden on start
        if (tooltipUI != null)
        {
            tooltipUI.SetActive(false);
        }
    }

    private void HandleTooltipState(bool show, string text, Vector2 screenPos)
    {
        if (tooltipUI == null) return;

        if (show)
        {
            tooltipUI.SetActive(true);
            // Offset tooltip slightly from mouse
            tooltipUI.transform.position = screenPos + new Vector2(15, 15);
            
            if (tooltipText != null)
            {
                tooltipText.text = text;
            }
        }
        else
        {
            tooltipUI.SetActive(false);
        }
    }
}
