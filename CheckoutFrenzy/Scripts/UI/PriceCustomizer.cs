using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace CryingSnow.CheckoutFrenzy
{
    public class PriceCustomizer : MonoBehaviour
    {
        [SerializeField, Tooltip("RectTransform of the main price customizer panel.")]
        private RectTransform mainPanel;

        [SerializeField, Tooltip("Image to display the product icon.")]
        private Image productIconImage;

        [SerializeField, Tooltip("Text to display the product name.")]
        private TMP_Text productNameText;

        [SerializeField, Tooltip("Text to display the current custom price of the product.")]
        private TMP_Text currentPriceText;

        [SerializeField, Tooltip("Text to display the default market price of the product.")]
        private TMP_Text marketPriceText;

        [SerializeField, Tooltip("Text to display the calculated profit based on the custom price.")]
        private TMP_Text profitText;

        [SerializeField, Tooltip("Text to display the currently set custom price.")]
        private TMP_Text customPriceText;

        [SerializeField, Tooltip("Slider to adjust the custom price.")]
        private Slider priceSlider;

        [SerializeField, Tooltip("Button to decrease the custom price (by one cent).")]
        private Button decreaseButton;

        [SerializeField, Tooltip("Button to increase the custom price (by one cent).")]
        private Button increaseButton;

        [SerializeField, Tooltip("Button to confirm and save the custom price.")]
        private Button confirmButton;

        /// <summary>
        /// Event invoked when the price customizer is closed.
        /// </summary>
        public UnityEvent OnClose { get; } = new UnityEvent();

        private Product product;
        private CustomPrice customPrice;

        private void Awake()
        {
            // Add a listener to the price slider's value changed event.
            priceSlider.onValueChanged.AddListener((value) =>
            {
                customPrice.PriceInCents = (long)value; // Update the custom price in cents.

                decimal price = customPrice.PriceInCents / 100m; // Convert to dollars.
                customPriceText.text = $"${price:N2}"; // Update the displayed custom price.

                decimal profit = price - product.Price; // Calculate profit.
                string color = profit > 0 ? "green" : profit < 0 ? "red" : "white"; // Set color based on profit.
                profitText.text = $"<color={color}>Profit: ${profit:N2}"; // Update the profit text.
            });

            mainPanel.anchoredPosition = Vector2.zero; // Position the panel correctly.
            gameObject.SetActive(false); // Initially hide the panel.
        }

        /// <summary>
        /// Opens the price customizer for the specified product.
        /// </summary>
        /// <param name="product">The Product object to customize the price for.</param>
        public void Open(Product product)
        {
            gameObject.SetActive(true);

            this.product = product;
            customPrice = new CustomPrice(); // Create a new CustomPrice object.
            customPrice.ProductId = product.ProductID;

            productIconImage.sprite = product.Icon; // Set the product icon.
            productNameText.text = product.Name; // Set the product name.

            // Display the product's current custom price (if it exists).
            decimal productPrice = DataManager.Instance.GetCustomProductPrice(product);
            currentPriceText.text = $"Current Price: ${productPrice:N2}";

            // Display the product's default market price.
            decimal defaultPrice = product.Price;
            marketPriceText.text = $"Market Price: ${product.MarketPrice:N2}";

            // Set up the price slider's range and initial value.
            float minValue = Mathf.FloorToInt((float)defaultPrice * 50f); // Minimum price (50% of default).
            float maxValue = Mathf.FloorToInt((float)defaultPrice * 200f); // Maximum price (200% of default).

            priceSlider.minValue = minValue;
            priceSlider.maxValue = maxValue;
            priceSlider.value = (float)productPrice * 100; // Set slider to current price.
            customPrice.PriceInCents = (long)priceSlider.value; // Initialize custom price.

            // Add listeners to the decrease and increase buttons.
            decreaseButton.onClick.RemoveAllListeners();
            decreaseButton.onClick.AddListener(() =>
            {
                if (priceSlider.value > minValue)
                    priceSlider.value--;

                AudioManager.Instance.PlaySFX(AudioID.Click);
            });

            increaseButton.onClick.RemoveAllListeners();
            increaseButton.onClick.AddListener(() =>
            {
                if (priceSlider.value < maxValue)
                    priceSlider.value++;

                AudioManager.Instance.PlaySFX(AudioID.Click);
            });

            // Add a listener to the confirm button.
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() =>
            {
                DataManager.Instance.AddCustomProductPrice(customPrice);
                UIManager.Instance.ToggleActionUI(ActionType.Return, false, null);
                AudioManager.Instance.PlaySFX(AudioID.Click);
                Close();
            });

            // Toggle the return button and set its action to close the price customizer.
            UIManager.Instance.ToggleActionUI(ActionType.Return, true, () =>
            {
                UIManager.Instance.ToggleActionUI(ActionType.Return, false, null);
                Close();
            });
        }

        /// <summary>
        /// Closes the price customizer.
        /// </summary>
        private void Close()
        {
            customPrice = null; // Clear the custom price data.
            OnClose?.Invoke(); // Invoke the OnClose event.
            gameObject.SetActive(false); // Deactivate the price customizer.
        }
    }
}
