using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CryingSnow.CheckoutFrenzy
{
    public class ExpansionListing : MonoBehaviour
    {
        [SerializeField, Tooltip("Text displaying the expansion name.")]
        private TMP_Text nameText;

        [SerializeField, Tooltip("Text displaying the expansion price.")]
        private TMP_Text priceText;

        [SerializeField, Tooltip("Text displaying the expansion description.")]
        private TMP_Text descriptionText;

        [SerializeField, Tooltip("Text displaying the expansion requirements.")]
        private TMP_Text requirementText;

        [SerializeField, Tooltip("Button to purchase the expansion.")]
        private Button purchaseButton;

        private Expansion expansion;

        /// <summary>
        /// Initializes the expansion listing with the expansion's details.
        /// </summary>
        /// <param name="expansion">The Expansion object to display.</param>
        public void Initialize(Expansion expansion)
        {
            this.expansion = expansion;

            nameText.text = expansion.Name;
            priceText.text = $"Price: ${expansion.UnlockPrice:N2}";

            descriptionText.text = expansion.UnlockWarehouse
                ? "\u2022 Unlock Warehouse"
                : "\u2022 Add more room to your store";

            if (expansion.AdditionalCustomers > 0)
            {
                descriptionText.text += $"\n\u2022 Customer capacity +{expansion.AdditionalCustomers}";
            }

            if (StoreManager.Instance.IsExpansionPurchased(expansion))
            {
                DisablePurchasing();
            }
            else
            {
                DataManager.Instance.OnLevelUp += UpdateRequirements;
                StoreManager.Instance.OnExpansionPurchased += UpdateRequirements;

                UpdateRequirements(0);
            }
        }

        /// <summary>
        /// Disables the purchase button and updates the requirement text when an expansion is already owned.
        /// </summary>
        private void DisablePurchasing()
        {
            purchaseButton.gameObject.SetActive(false);
            requirementText.text = "You already own this expansion.";
        }

        /// <summary>
        /// Updates the requirement text and interactability of the purchase button based on current game state.
        /// </summary>
        /// <param name="_">Parameter is not used but required by the event signature.</param>
        private void UpdateRequirements(int _)
        {
            bool isLevelMet = DataManager.Instance.Data.CurrentLevel >= expansion.RequiredLevel;
            bool isCurrentExpansion = StoreManager.Instance.IsCurrentExpansion(expansion);

            bool isAvailable = isLevelMet && isCurrentExpansion;
            requirementText.text = isAvailable ? "<color=green>AVAILABLE" : "<color=red>UNAVAILABLE";

            requirementText.text += isLevelMet ? "\n<color=green>" : "\n<color=red>";
            requirementText.text += $"Requires Level {expansion.RequiredLevel}";

            if (isCurrentExpansion) requirementText.text += "\n<color=green>Previous expansion purchased";
            else requirementText.text += "\n<color=red>Purchase previous expansion";

            purchaseButton.interactable = isAvailable;

            if (isAvailable)
            {
                purchaseButton.onClick.AddListener(HandlePurchase);
                DataManager.Instance.OnLevelUp -= UpdateRequirements;
                StoreManager.Instance.OnExpansionPurchased -= UpdateRequirements;
            }
        }

        /// <summary>
        /// Handles the purchase of the expansion.
        /// </summary>
        private void HandlePurchase()
        {
            bool isPurchased = StoreManager.Instance.PurchaseExpansion();
            if (isPurchased) DisablePurchasing();
        }
    }
}
