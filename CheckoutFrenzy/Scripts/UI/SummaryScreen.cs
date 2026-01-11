using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CryingSnow.CheckoutFrenzy
{
    public class SummaryScreen : MonoBehaviour
    {
        [SerializeField, Tooltip("RectTransform of the main summary panel.")]
        private RectTransform mainPanel;

        [SerializeField, Tooltip("Text component to display the summary values.")]
        private TMP_Text valuesText;

        [SerializeField, Tooltip("Toggle to allow skipping the summary.")]
        private Toggle skipToggle;

        [SerializeField, Tooltip("Button to continue after viewing the summary.")]
        private Button continueButton;

        private PlayerStateManager stateManager;

        private void Start()
        {
            mainPanel.anchoredPosition = Vector2.zero; // Center the panel.

            // Set a semi-transparent background color if an Image component exists.
            if (TryGetComponent<Image>(out Image image))
            {
                image.color = new Color(0f, 0f, 0f, 0.4f);
            }

            // Add a listener to the skip toggle's value changed event.
            skipToggle.onValueChanged.AddListener(isOn =>
                AudioManager.Instance.PlaySFX(AudioID.Click)
            );

            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                stateManager = player.StateManager;

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows the summary screen and populates it with the provided data.
        /// </summary>
        /// <param name="data">The SummaryData object containing the summary information.</param>
        /// <param name="onContinue">The action to be performed when the continue button is clicked. Passes a boolean indicating if the summary was skipped.</param>
        public void Show(SummaryData data, System.Action<bool> onContinue)
        {
            gameObject.SetActive(true);

            string values = $"{data.TotalCustomers}"; // 1. Total Customers

            values += $"\n${data.PreviousBalance:N2}"; // 2. Previous Balance

            values += $"\n<color=green>+${data.TotalRevenues:N2}"; // 3. Total Revenues (green text)

            values += $"\n<color=red>-${data.TotalSpending:N2}"; // 4. Total Spending (red text)

            values += $"\n<color=white>${DataManager.Instance.PlayerMoney:N2}"; // 5. Current Balance (white text)

            valuesText.text = values; // Set the summary text.

            stateManager?.PushState(PlayerState.Paused);

            // Add a listener to the continue button.
            continueButton.onClick.RemoveAllListeners(); // Remove any previous listeners.
            continueButton.onClick.AddListener(() =>
            {
                onContinue?.Invoke(skipToggle.isOn); // Invoke the continue action and pass the skip toggle state.
                AudioManager.Instance.PlaySFX(AudioID.Click);

                stateManager?.PopState();

                gameObject.SetActive(false);
            });
        }
    }
}
