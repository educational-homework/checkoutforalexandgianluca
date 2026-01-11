using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CryingSnow.CheckoutFrenzy
{
    public class LicenseListing : MonoBehaviour
    {
        [SerializeField, Tooltip("Text displaying the license name.")]
        private TMP_Text nameText;

        [SerializeField, Tooltip("Text displaying the license price.")]
        private TMP_Text priceText;

        [SerializeField, Tooltip("Text displaying the license description (products it unlocks).")]
        private TMP_Text descriptionText;

        [SerializeField, Tooltip("Text displaying the license purchase requirements.")]
        private TMP_Text requirementText;

        [SerializeField, Tooltip("Button to purchase the license.")]
        private Button purchaseButton;

        private License license;

        /// <summary>
        /// Initializes the license listing with the license's details.
        /// </summary>
        /// <param name="license">The License object to display.</param>
        public void Initialize(License license)
        {
            this.license = license;

            nameText.text = license.Name;
            priceText.text = $"Price: ${license.Price:N2}";
            descriptionText.text = "Permission to sell the following products:";

            // Add each product unlocked by the license to the description text.
            foreach (var product in license.Products)
            {
                if (product == null)
                {
                    Debug.LogWarning($"[LicenseListing] Null product found in license '{license.Name}'. Please check the license asset.");
                    continue;
                }

                descriptionText.text += $"\n\u2022 {product.Name} ({product.Section})";
            }

            // Disable purchasing if the license is already owned.
            if (license.IsOwnedByDefault || license.IsPurchased)
            {
                DisablePurchasing();
            }
            else
            {
                // Subscribe to the OnLevelUp event to update purchase availability.
                DataManager.Instance.OnLevelUp += UpdatePurchaseAvailability;
                UpdatePurchaseAvailability(DataManager.Instance.Data.CurrentLevel); // Initial check.

                StoreManager.Instance.OnLicensePurchased += HandleLicensePurchased;
            }
        }

        /// <summary>
        /// Disables the purchase button and updates the requirement text when a license is already owned.
        /// </summary>
        private void DisablePurchasing()
        {
            purchaseButton.gameObject.SetActive(false);
            requirementText.text = "You already own this license.";
        }

        private void HandleLicensePurchased(License _)
        {
            UpdatePurchaseAvailability(DataManager.Instance.Data.CurrentLevel);
        }

        /// <summary>
        /// Updates the purchase availability (interactability of the purchase button) based on the player / store level.
        /// </summary>
        /// <param name="level">The player / store current level.</param>
        private void UpdatePurchaseAvailability(int level)
        {
            bool meetsLevelRequirement = level >= license.Level;
            bool hasRequiredLicense = license.RequiredLicense == null ||
                                      license.RequiredLicense.IsOwnedByDefault ||
                                      license.RequiredLicense.IsPurchased;

            bool isAvailable = meetsLevelRequirement && hasRequiredLicense;

            string levelColor = meetsLevelRequirement ? "<color=green>" : "<color=red>";
            string licenseColor = hasRequiredLicense ? "<color=green>" : "<color=red>";

            string levelRequirementText = $"{levelColor}Requires Level {license.Level}</color>";
            string licenseRequirementText = license.RequiredLicense != null
                ? $"\n{licenseColor}Requires {license.RequiredLicense.Name}</color>"
                : "";

            requirementText.text = $"{(isAvailable ? "<color=green>AVAILABLE" : "<color=red>UNAVAILABLE")}</color>\n"
                + levelRequirementText + licenseRequirementText;

            purchaseButton.interactable = meetsLevelRequirement && hasRequiredLicense;

            if (purchaseButton.interactable)
            {
                purchaseButton.onClick.RemoveAllListeners();
                purchaseButton.onClick.AddListener(() => HandlePurchase(license));
                DataManager.Instance.OnLevelUp -= UpdatePurchaseAvailability;
                StoreManager.Instance.OnLicensePurchased -= HandleLicensePurchased;
            }
        }

        /// <summary>
        /// Handles the purchase of the license.
        /// </summary>
        /// <param name="license">The License being purchased.</param>
        private void HandlePurchase(License license)
        {
            bool isPurchased = StoreManager.Instance.PurchaseLicense(license); // Attempt purchase.
            if (isPurchased) DisablePurchasing(); // Update UI if purchase successful.
        }
    }
}
