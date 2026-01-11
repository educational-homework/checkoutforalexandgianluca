using UnityEngine;
using UnityEngine.UI;

namespace CryingSnow.CheckoutFrenzy
{
    public class SkipDialog : MonoBehaviour
    {
        [SerializeField, Tooltip("The button used to skip the day.")]
        private Button skipButton;

        [SerializeField, Tooltip("The key used to skip the day.")]
        private KeyCode skipKey = KeyCode.Z;

        [SerializeField, Tooltip("Image showing the icon of the skip key.")]
        private Image keyIcon;

        [SerializeField, Tooltip("Toggle used to show and hide the dialog on mobile contol mode.")]
        private PanelToggle panelToggle;

        private bool isMobileControl;
        private System.Action onSkip;

        private void Awake()
        {
            isMobileControl = GameConfig.Instance.ControlMode == ControlMode.Mobile;

            keyIcon.gameObject.SetActive(!isMobileControl);
            panelToggle.gameObject.SetActive(isMobileControl);

            Hide(); // Initially hide the dialog.
        }

        private void Update()
        {
            if (Input.GetKeyDown(skipKey) && !isMobileControl)
            {
                SkipTheDay();
            }
        }

        /// <summary>
        /// Shows the skip dialog and sets up the skip button's action.
        /// </summary>
        /// <param name="onSkip">The action to be performed when the skip button is clicked.</param>
        public void Show(System.Action onSkip)
        {
            gameObject.SetActive(true); // Activate the dialog.

            this.onSkip = onSkip;

            if (isMobileControl)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(SkipTheDay);
            }
        }

        /// <summary>
        /// Hides the skip dialog.
        /// </summary>
        public void Hide()
        {
            onSkip = null;
            gameObject.SetActive(false);
        }

        private void SkipTheDay()
        {
            onSkip?.Invoke(); // Invoke the provided skip action.
            AudioManager.Instance.PlaySFX(AudioID.Click); // Play a click sound.
            Hide(); // Hide the dialog.
        }
    }
}
