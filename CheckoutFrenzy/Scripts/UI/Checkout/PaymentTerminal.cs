using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace CryingSnow.CheckoutFrenzy
{
    public class PaymentTerminal : MonoBehaviour
    {
        [SerializeField, Tooltip("The text display showing the entered amount.")]
        private TMP_Text displayText;

        [SerializeField, Tooltip("The button used to confirm the payment.")]
        private Button confirmButton;

        public event System.Action<decimal> OnConfirm;

        private RectTransform rect;
        private float originalPosY;
        private bool allowInput;
        private string enteredAmount;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            originalPosY = rect.anchoredPosition.y;
            confirmButton.onClick.AddListener(ConfirmAmount); // Add listener to the confirm button.
        }

        /// <summary>
        /// Appends the input to the entered amount string.
        /// </summary>
        /// <param name="input">The input string (number, "back", or ".").</param>
        public void Append(string input)
        {
            if (!allowInput) return;

            if (input == "back")
            {
                if (enteredAmount.Length > 0)
                {
                    enteredAmount = enteredAmount.Substring(0, enteredAmount.Length - 1); // Remove the last character.
                }
            }
            else if (input == "." && !enteredAmount.Contains(".")) // Allow only one decimal point.
            {
                enteredAmount += ".";
            }
            else if (int.TryParse(input, out int _)) // Only allow numeric input.
            {
                enteredAmount += input;
            }

            displayText.text = $"$ {enteredAmount}";

            AudioManager.Instance.PlaySFX(AudioID.Beep);
        }

        /// <summary>
        /// Confirms the entered amount and triggers the OnConfirm event.
        /// </summary>
        private void ConfirmAmount()
        {
            if (decimal.TryParse(enteredAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount) && amount > 0)
            {
                OnConfirm?.Invoke(amount);
            }
            else
            {
                UIManager.Instance.Message.Log("Invalid amount. Please enter a valid amount.", Color.red);
            }

            AudioManager.Instance.PlaySFX(AudioID.Beep);
        }

        /// <summary>
        /// Opens the payment terminal UI, allowing input.
        /// </summary>
        public void Open()
        {
            enteredAmount = ""; // Clear the entered amount.
            displayText.text = "$";  // Reset the display text.

            rect.DOAnchorPosY(0f, 0.5f) // Animate the terminal opening.
                .OnComplete(() => allowInput = true); // Enable input after the animation.
        }

        /// <summary>
        /// Closes the payment terminal UI, disabling input.
        /// </summary>
        public void Close()
        {
            allowInput = false; // Disable input.
            rect.DOAnchorPosY(originalPosY, 0.5f); // Animate the terminal closing.
        }
    }
}
