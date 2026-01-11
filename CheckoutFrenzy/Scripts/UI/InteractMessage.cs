using UnityEngine;
using TMPro;

namespace CryingSnow.CheckoutFrenzy
{
    public class InteractMessage : MonoBehaviour
    {
        [SerializeField] private ControlMode controlMode;
        private TMP_Text messageText;

        public ControlMode ControlMode => controlMode;

        private void Awake()
        {
            messageText = GetComponentInChildren<TMP_Text>();
            Hide(); // Initially hide the message.
        }

        /// <summary>
        /// Displays the interaction message.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public void Display(string message)
        {
            // Check PlayerPrefs to see if the message display is enabled.
            if (PlayerPrefs.GetInt("Display Interact Message", 1) < 1) return; // If disabled, don't display the message.

            gameObject.SetActive(true); // Activate the message UI.
            messageText.text = message; // Set the message text.
        }

        /// <summary>
        /// Hides the interaction message.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false); // Deactivate the message UI.
        }
    }
}
