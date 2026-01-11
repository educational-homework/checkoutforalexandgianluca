using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CryingSnow.CheckoutFrenzy
{
    public class FurnitureListing : MonoBehaviour
    {
        [SerializeField, Tooltip("Image displaying the furniture icon.")]
        private Image iconImage;

        [SerializeField, Tooltip("Text displaying the furniture name.")]
        private TMP_Text nameText;

        [SerializeField, Tooltip("Text displaying the furniture section.")]
        private TMP_Text sectionText;

        [SerializeField, Tooltip("Text displaying the furniture price.")]
        private TMP_Text priceText;

        [SerializeField, Tooltip("Text displaying the selected amount of furniture.")]
        private TMP_Text amountText;

        [SerializeField, Tooltip("Text displaying the total price of the selected furniture amount.")]
        private TMP_Text totalText;

        [SerializeField, Tooltip("Button to decrease the selected amount.")]
        private Button decreaseButton;

        [SerializeField, Tooltip("Button to increase the selected amount.")]
        private Button increaseButton;

        [SerializeField, Tooltip("Button to add the selected furniture to the cart.")]
        private Button addToCartButton;

        public Section Section { get; private set; }

        private int amount;
        private decimal price;

        /// <summary>
        /// Initializes the furniture listing with the furniture's details.
        /// </summary>
        /// <param name="furniture">The Furniture object to display.</param>
        public void Initialize(Furniture furniture)
        {
            this.Section = furniture.Section;

            iconImage.sprite = furniture.Icon;
            nameText.text = furniture.Name;

            // Hide the section text if the furniture is in the General section (e.g., Trash Can, Decorations).
            if (furniture.Section == Section.General) sectionText.gameObject.SetActive(false);
            else sectionText.text = $"Section: {furniture.Section}";

            price = furniture.Price;
            priceText.text = $"Price: ${price:N2}";

            UpdateAmount(1, false); // Initialize the amount to 1.

            decreaseButton.onClick.AddListener(() => UpdateAmount(-1)); // Add listener to decrease button.
            increaseButton.onClick.AddListener(() => UpdateAmount(1)); // Add listener to increase button.

            addToCartButton.onClick.AddListener(() => PC.Instance.AddToCart(furniture, amount)); // Add listener to add to cart button.
        }

        /// <summary>
        /// Updates the selected amount of furniture and the total price.
        /// </summary>
        /// <param name="value">The amount to change the selected quantity by (positive or negative).</param>
        /// <param name="playSFX">Whether to play a sound effect (defaults to true).</param>
        private void UpdateAmount(int value, bool playSFX = true)
        {
            amount += value;
            amount = Mathf.Clamp(amount, 1, 10); // Clamp the amount between 1 and 10.
            amountText.text = $"Amount: {amount}";

            decimal totalPrice = price * amount;
            totalText.text = $"Total: ${totalPrice:N2}";

            if (playSFX) AudioManager.Instance.PlaySFX(AudioID.Click);
        }
    }
}
