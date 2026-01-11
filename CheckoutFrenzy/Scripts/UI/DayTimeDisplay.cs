using UnityEngine;
using TMPro;

namespace CryingSnow.CheckoutFrenzy
{
    [RequireComponent(typeof(TMP_Text))]
    public class DayTimeDisplay : MonoBehaviour
    {
        private TMP_Text displayText;

        private void Start()
        {
            displayText = GetComponent<TMP_Text>();

            TimeManager.Instance.OnMinutePassed += UpdateDisplay; // Subscribe to the OnMinutePassed event.
            UpdateDisplay(); // Initial update of the display.
        }

        private void OnDisable()
        {
            TimeManager.Instance.OnMinutePassed -= UpdateDisplay; // Unsubscribe from the event to prevent memory leaks.
        }

        /// <summary>
        /// Updates the displayed day and time.
        /// </summary>
        private void UpdateDisplay()
        {
            int day = DataManager.Instance.Data.TotalDays; // Get the current day.
            string time = TimeManager.Instance.GetFormattedTime(); // Get the formatted time.

            displayText.text = $"<mspace=0.7em>Day {day}, {time}"; // Set the display text.
        }
    }
}
