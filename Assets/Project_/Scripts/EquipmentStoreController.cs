
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentStoreController : MonoBehaviour
{
    [Header("Budget")]
    [SerializeField] private int startingBudget = 10000;
    [SerializeField] private int rocketACost = 5000;
    [SerializeField] private int rocketBCost = 8000;

    [Header("UI References")]
    [SerializeField] private TMP_Text budgetText;
    [SerializeField] private TMP_Text purchaseFeedbackText;

    [SerializeField] private Button buyRocketButton;
    [SerializeField] private Button buyRocketBButton;

    private int currentBudget;

    private bool hasPurchasedRocketA;
    private bool hasPurchasedRocketB;

    private void Awake()
    {
        currentBudget = startingBudget;

        hasPurchasedRocketA = false;
        hasPurchasedRocketB = false;

        if (purchaseFeedbackText != null)
            purchaseFeedbackText.text = "";

        UpdateBudgetUI();

        ResetPurchaseButton(buyRocketButton);
        ResetPurchaseButton(buyRocketBButton);
    }

    // Purchase Rocket A
    public void BuyRocketA()
    {
        PurchaseRocket(
            "ROCKET A",
            rocketACost,
            ref hasPurchasedRocketA,
            buyRocketButton
        );
    }

    // Purchase Rocket B
    public void BuyRocketB()
    {
        PurchaseRocket(
            "ROCKET B",
            rocketBCost,
            ref hasPurchasedRocketB,
            buyRocketBButton
        );
    }

    // Shared purchase system
    private void PurchaseRocket(
        string rocketName,
        int cost,
        ref bool hasPurchased,
        Button purchaseButton)
    {
        if (hasPurchased)
        {
            ShowFeedback(
                rocketName + " ALREADY PURCHASED!",
                new Color32(255, 190, 70, 255)
            );
            return;
        }

        if (currentBudget < cost)
        {
            ShowFeedback(
                "INSUFFICIENT BUDGET!",
                new Color32(255, 90, 90, 255)
            );
            return;
        }

        currentBudget -= cost;
        hasPurchased = true;

        UpdateBudgetUI();

        ShowFeedback(
            rocketName + " PURCHASED!",
            new Color32(80, 220, 150, 255)
        );

        if (purchaseButton != null)
        {
            purchaseButton.interactable = false;

            TMP_Text buttonText =
                purchaseButton.GetComponentInChildren<TMP_Text>(true);

            if (buttonText != null)
                buttonText.text = "PURCHASED";
        }

        Debug.Log(rocketName + " purchased successfully!");
    }

    private void UpdateBudgetUI()
    {
        if (budgetText != null)
            budgetText.text = $"BUDGET: {currentBudget:N0} CR";
    }

    private void ShowFeedback(string message, Color color)
    {
        if (purchaseFeedbackText == null)
            return;

        purchaseFeedbackText.text = message;
        purchaseFeedbackText.color = color;
    }

    private void ResetPurchaseButton(Button button)
    {
        if (button != null)
            button.interactable = true;
    }
}
