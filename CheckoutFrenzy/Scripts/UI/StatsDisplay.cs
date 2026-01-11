using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CryingSnow.CheckoutFrenzy
{
    public class StatsDisplay : MonoBehaviour
    {
        [SerializeField, Tooltip("Text component to display the player's money.")]
        private TMP_Text moneyDisplay;

        [SerializeField, Tooltip("Text component to display the player / store current level.")]
        private TMP_Text levelDisplay;

        [SerializeField, Tooltip("Image used as a fill bar to visualize level progress.")]
        private Image levelFill;

        private void Start()
        {
            DataManager.Instance.OnMoneyChanged += UpdateMoneyDisplay; // Subscribe to money changed events.
            DataManager.Instance.OnLevelUp += UpdateLevelDisplay; // Subscribe to level up events.
            DataManager.Instance.OnExperienceGain += UpdateLevelFill; // Subscribe to experience gain events.

            UpdateMoneyDisplay(DataManager.Instance.PlayerMoney); // Initial update of money display.
            UpdateLevelDisplay(DataManager.Instance.Data.CurrentLevel); // Initial update of level display.

            // Calculate and update the level fill bar.
            float currentExp = (float)DataManager.Instance.Data.CurrentExperience;
            int expForNextLevel = DataManager.Instance.CalculateExperienceForNextLevel();
            float progress = currentExp / expForNextLevel;
            UpdateLevelFill(progress);
        }

        /// <summary>
        /// Updates the displayed money amount.
        /// </summary>
        /// <param name="amount">The player's current money.</param>
        private void UpdateMoneyDisplay(decimal amount)
        {
            moneyDisplay.text = $"$ {amount:N2}"; // Format and set the money text.
            moneyDisplay.color = amount < 0m ? Color.red : Color.white;
        }

        /// <summary>
        /// Updates the displayed player / store level.
        /// </summary>
        /// <param name="level">The player / store current level.</param>
        private void UpdateLevelDisplay(int level)
        {
            levelDisplay.text = $"Level {level}";
        }

        /// <summary>
        /// Updates the level fill bar based on experience progress.
        /// </summary>
        /// <param name="progress">The normalized experience progress (0-1).</param>
        private void UpdateLevelFill(float progress)
        {
            levelFill.fillAmount = progress;
        }
    }
}
